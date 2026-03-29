"""
Pivot Point Setter
──────────────────
• Bulk-select hundreds of images
• Click to set a pivot (saved as % from bottom-left)
• Auto-advances to the next image after each click
• Thumbnails generated in background threads; virtual list for performance
• Saves <image_name>.txt next to each image
"""

import tkinter as tk
from tkinter import filedialog, messagebox
from PIL import Image, ImageTk
import os
import threading
import queue
from collections import OrderedDict

# ── Palette ───────────────────────────────────────────────────────────────────
BG         = "#0f0f13"
PANEL_BG   = "#16161d"
ACCENT     = "#7c5cfc"
ACCENT2    = "#c084fc"
TEXT       = "#e8e6f0"
TEXT_DIM   = "#6b6880"
THUMB_BG   = "#1e1e28"
THUMB_SEL  = "#2a2040"
BORDER     = "#2a2840"
CROSS_COL  = "#ff4d6d"
CROSS_RING = "#ffffff"
SUCCESS    = "#34d399"
WARNING    = "#fbbf24"

THUMB_W, THUMB_H = 100, 72   # thumbnail pixel size
CELL_H           = 96        # fixed row height in virtual list
CANVAS_PAD       = 20
THUMB_CACHE_MAX  = 300       # max cached PhotoImage thumbnails
PREFETCH_AHEAD   = 3         # how many images to pre-load full-res ahead


# ── LRU thumbnail cache (PhotoImage must stay alive) ──────────────────────────
class LRUPhotoCache:
    def __init__(self, maxsize: int):
        self._cache: OrderedDict = OrderedDict()
        self._maxsize = maxsize

    def get(self, key: str):
        if key in self._cache:
            self._cache.move_to_end(key)
            return self._cache[key]
        return None

    def put(self, key: str, photo):
        self._cache[key] = photo
        self._cache.move_to_end(key)
        while len(self._cache) > self._maxsize:
            self._cache.popitem(last=False)


# ── Background thumbnail worker ───────────────────────────────────────────────
class ThumbLoader(threading.Thread):
    """Daemon thread that generates thumbnails from a work queue."""

    def __init__(self, result_queue):
        super().__init__(daemon=True)
        self._work = queue.Queue()
        self._result = result_queue
        self._seen = set()

    def request(self, path: str):
        if path not in self._seen:
            self._seen.add(path)
            self._work.put(path)

    def run(self):
        while True:
            path = self._work.get()
            try:
                img = Image.open(path)
                img.thumbnail((THUMB_W, THUMB_H), Image.BILINEAR)
                self._result.put((path, img))
            except Exception:
                self._result.put((path, None))


# ── Main application ──────────────────────────────────────────────────────────
class PivotApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("Pivot Point Setter")
        self.configure(bg=BG)
        self.minsize(1000, 640)

        # ── Core state ──
        self.image_paths = []
        self.pivots = {}
        self.current_index = None

        # ── Image display state ──
        self._photo = None
        self._orig_image = None
        self._canvas_offset = (0, 0)
        self._canvas_scale  = 1.0

        # ── Thumbnail pipeline ──
        self._thumb_cache = LRUPhotoCache(THUMB_CACHE_MAX)
        self._thumb_queue = queue.Queue()
        self._thumb_loader = ThumbLoader(self._thumb_queue)
        self._thumb_loader.start()

        # ── Full-image prefetch cache (keeps last N PIL Images) ──
        self._img_cache = OrderedDict()
        self._img_cache_max = PREFETCH_AHEAD + 2
        self._prefetch_lock = threading.Lock()

        self._build_ui()
        self._bind_keys()
        self._poll_thumbs()

    # ── UI construction ───────────────────────────────────────────────────────

    def _build_ui(self):
        # Top bar
        topbar = tk.Frame(self, bg=PANEL_BG, height=52)
        topbar.pack(fill="x", side="top")
        topbar.pack_propagate(False)

        tk.Label(topbar, text="✦ PIVOT SETTER", font=("Courier New", 13, "bold"),
                 bg=PANEL_BG, fg=ACCENT2).pack(side="left", padx=18, pady=14)

        btn_kw = dict(font=("Courier New", 10, "bold"), relief="flat",
                      cursor="hand2", pady=6, padx=14)

        tk.Button(topbar, text="SAVE  TXT", bg=ACCENT, fg="#fff",
                  command=self._save, **btn_kw).pack(side="right", padx=12, pady=8)
        tk.Button(topbar, text="ADD  IMAGES", bg=THUMB_SEL, fg=TEXT,
                  command=self._add_images, **btn_kw).pack(side="right", padx=4, pady=8)
        tk.Button(topbar, text="CLEAR  ALL", bg=THUMB_BG, fg=TEXT_DIM,
                  command=self._clear_all, **btn_kw).pack(side="right", padx=4, pady=8)

        body = tk.Frame(self, bg=BG)
        body.pack(fill="both", expand=True)

        # ── Left panel: virtual thumbnail list ──
        left = tk.Frame(body, bg=PANEL_BG, width=152)
        left.pack(side="left", fill="y")
        left.pack_propagate(False)

        hdr = tk.Frame(left, bg=PANEL_BG)
        hdr.pack(fill="x", padx=8, pady=(10, 2))
        tk.Label(hdr, text="IMAGES", font=("Courier New", 8, "bold"),
                 bg=PANEL_BG, fg=TEXT_DIM).pack(side="left")
        self._count_lbl = tk.Label(hdr, text="", font=("Courier New", 8),
                                   bg=PANEL_BG, fg=TEXT_DIM)
        self._count_lbl.pack(side="right")

        vlist_frame = tk.Frame(left, bg=PANEL_BG)
        vlist_frame.pack(fill="both", expand=True)

        self._vcanvas = tk.Canvas(vlist_frame, bg=PANEL_BG, bd=0,
                                  highlightthickness=0, width=140)
        vsb = tk.Scrollbar(vlist_frame, orient="vertical",
                           command=self._vcanvas.yview)
        self._vcanvas.configure(yscrollcommand=vsb.set)
        vsb.pack(side="right", fill="y")
        self._vcanvas.pack(side="left", fill="both", expand=True)
        self._vcanvas.bind("<Configure>",     self._on_vcanvas_configure)
        self._vcanvas.bind("<ButtonPress-1>", self._on_vlist_click)
        self._vcanvas.bind("<MouseWheel>",    self._on_vlist_scroll)
        self._vcanvas.bind("<Button-4>",      self._on_vlist_scroll)
        self._vcanvas.bind("<Button-5>",      self._on_vlist_scroll)

        self.status_lbl = tk.Label(left, text="no image selected",
                                   font=("Courier New", 8), bg=PANEL_BG,
                                   fg=TEXT_DIM, wraplength=136, justify="left")
        self.status_lbl.pack(pady=6, padx=8, anchor="w")

        # ── Centre: main image canvas ──
        centre = tk.Frame(body, bg=BG)
        centre.pack(side="left", fill="both", expand=True)

        self.hint_lbl = tk.Label(centre,
            text="Add images and click to place a pivot point.\n"
                 "Auto-advances to the next image after each click.\n"
                 "The pivot is saved as % from the bottom-left corner.",
            font=("Courier New", 11), bg=BG, fg=TEXT_DIM, justify="center")
        self.hint_lbl.place(relx=0.5, rely=0.5, anchor="center")

        self.canvas = tk.Canvas(centre, bg=BG, bd=0, highlightthickness=0,
                                cursor="crosshair")
        self.canvas.pack(fill="both", expand=True)
        self.canvas.bind("<Button-1>",  self._on_canvas_click)
        self.canvas.bind("<Configure>", self._on_canvas_resize)

        # ── Right panel: info ──
        right = tk.Frame(body, bg=PANEL_BG, width=180)
        right.pack(side="right", fill="y")
        right.pack_propagate(False)

        tk.Label(right, text="PIVOT  INFO", font=("Courier New", 8, "bold"),
                 bg=PANEL_BG, fg=TEXT_DIM).pack(pady=(16, 10), padx=14, anchor="w")

        self.pivot_lbl = tk.Label(right, text="—", font=("Courier New", 20, "bold"),
                                  bg=PANEL_BG, fg=ACCENT2, justify="center")
        self.pivot_lbl.pack(pady=4)
        self.pivot_sub = tk.Label(right, text="click image\nto set pivot",
                                  font=("Courier New", 9), bg=PANEL_BG,
                                  fg=TEXT_DIM, justify="center")
        self.pivot_sub.pack()

        tk.Frame(right, bg=BORDER, height=1).pack(fill="x", padx=14, pady=16)

        tk.Label(right, text="LEGEND", font=("Courier New", 8, "bold"),
                 bg=PANEL_BG, fg=TEXT_DIM).pack(anchor="w", padx=14)
        for icon, desc in [("✦", "has pivot"), ("○", "no pivot"),
                            ("◀▶", "arrow keys"), ("↵", "auto-advance")]:
            row = tk.Frame(right, bg=PANEL_BG)
            row.pack(fill="x", padx=14, pady=2)
            tk.Label(row, text=icon, font=("Courier New", 9, "bold"),
                     bg=PANEL_BG, fg=ACCENT, width=5, anchor="w").pack(side="left")
            tk.Label(row, text=desc, font=("Courier New", 8),
                     bg=PANEL_BG, fg=TEXT_DIM, anchor="w").pack(side="left")

        tk.Frame(right, bg=BORDER, height=1).pack(fill="x", padx=14, pady=16)

        self.progress_lbl = tk.Label(right, text="0 / 0  set",
                                     font=("Courier New", 9, "bold"),
                                     bg=PANEL_BG, fg=SUCCESS)
        self.progress_lbl.pack(padx=14, anchor="w")

        self.load_lbl = tk.Label(right, text="", font=("Courier New", 8),
                                 bg=PANEL_BG, fg=WARNING)
        self.load_lbl.pack(padx=14, anchor="w", pady=(4, 0))

    # ── Virtual thumbnail list ────────────────────────────────────────────────

    def _on_vcanvas_configure(self, _event=None):
        total_h = len(self.image_paths) * CELL_H
        self._vcanvas.configure(scrollregion=(0, 0, 140, max(total_h, 1)))
        self._render_vlist()

    def _on_vlist_scroll(self, event):
        if event.num == 4 or (hasattr(event, 'delta') and event.delta > 0):
            self._vcanvas.yview_scroll(-1, "units")
        else:
            self._vcanvas.yview_scroll(1, "units")

    def _on_vlist_click(self, event):
        canvas_y = self._vcanvas.canvasy(event.y)
        idx = int(canvas_y // CELL_H)
        if 0 <= idx < len(self.image_paths):
            self._select(idx)

    def _render_vlist(self):
        """Redraw only the visible portion of the virtual list."""
        self._vcanvas.delete("all")
        if not self.image_paths:
            return

        cw      = self._vcanvas.winfo_width() or 140
        top     = self._vcanvas.canvasy(0)
        bottom  = self._vcanvas.canvasy(self._vcanvas.winfo_height() or 500)
        first   = max(0, int(top // CELL_H))
        last    = min(len(self.image_paths) - 1, int(bottom // CELL_H) + 1)

        for i in range(first, last + 1):
            path      = self.image_paths[i]
            is_sel    = (i == self.current_index)
            has_pivot = path in self.pivots
            y0        = i * CELL_H

            bg = THUMB_SEL if is_sel else THUMB_BG
            self._vcanvas.create_rectangle(4, y0+2, cw-4, y0+CELL_H-2,
                                           fill=bg,
                                           outline=BORDER if is_sel else "",
                                           width=1)

            photo = self._thumb_cache.get(path)
            if photo:
                self._vcanvas.create_image(cw // 2, y0 + 6, anchor="n", image=photo)
            else:
                self._thumb_loader.request(path)
                self._vcanvas.create_text(cw // 2, y0 + 6 + THUMB_H // 2,
                                          text="…", fill=TEXT_DIM,
                                          font=("Courier New", 16))

            name  = os.path.basename(path)
            short = name if len(name) <= 13 else name[:10] + "…"
            ind   = "✦" if has_pivot else "○"
            col   = ACCENT2 if has_pivot else (TEXT if is_sel else TEXT_DIM)
            self._vcanvas.create_text(6, y0 + CELL_H - 12, anchor="w",
                                      text=f"{ind} {short}",
                                      fill=col,
                                      font=("Courier New", 7))

        total_h = len(self.image_paths) * CELL_H
        self._vcanvas.configure(scrollregion=(0, 0, cw, total_h))

        # Queue thumbnails for a slightly wider window than visible
        for i in range(max(0, first - 5), min(len(self.image_paths), last + 6)):
            self._thumb_loader.request(self.image_paths[i])

    def _scroll_vlist_to(self, idx: int):
        if not self.image_paths:
            return
        total_h   = len(self.image_paths) * CELL_H
        visible_h = self._vcanvas.winfo_height() or 400
        frac_top  = (idx * CELL_H) / total_h
        frac_bot  = ((idx + 1) * CELL_H) / total_h
        cur_top, cur_bot = self._vcanvas.yview()
        if frac_top < cur_top:
            self._vcanvas.yview_moveto(frac_top)
        elif frac_bot > cur_bot:
            self._vcanvas.yview_moveto(max(0.0, frac_bot - visible_h / total_h))

    # ── Background thumbnail polling ──────────────────────────────────────────

    def _poll_thumbs(self):
        dirty = False
        try:
            while True:
                path, pil_img = self._thumb_queue.get_nowait()
                if pil_img is not None:
                    photo = ImageTk.PhotoImage(pil_img)
                    self._thumb_cache.put(path, photo)
                    dirty = True
        except queue.Empty:
            pass
        if dirty:
            self._render_vlist()
        self.after(80, self._poll_thumbs)

    # ── Image loading / prefetch ──────────────────────────────────────────────

    def _get_full_image(self, path: str):
        if path in self._img_cache:
            self._img_cache.move_to_end(path)
            return self._img_cache[path]
        try:
            img = Image.open(path)
            img.load()
            self._img_cache[path] = img
            self._img_cache.move_to_end(path)
            while len(self._img_cache) > self._img_cache_max:
                self._img_cache.popitem(last=False)
            return img
        except Exception as e:
            messagebox.showerror("Error", f"Cannot open image:\n{e}")
            return None

    def _prefetch(self, idx: int):
        def _load(path):
            if path not in self._img_cache:
                try:
                    img = Image.open(path)
                    img.load()
                    with self._prefetch_lock:
                        self._img_cache[path] = img
                        self._img_cache.move_to_end(path)
                        while len(self._img_cache) > self._img_cache_max:
                            self._img_cache.popitem(last=False)
                except Exception:
                    pass

        for offset in range(1, PREFETCH_AHEAD + 1):
            nxt = idx + offset
            if nxt < len(self.image_paths):
                threading.Thread(target=_load,
                                 args=(self.image_paths[nxt],),
                                 daemon=True).start()

    # ── Add / select / clear ──────────────────────────────────────────────────

    def _add_images(self):
        paths = filedialog.askopenfilenames(
            title="Select images",
            filetypes=[("Images", "*.png *.jpg *.jpeg *.bmp *.gif *.tiff *.webp"),
                       ("All files", "*.*")])
        if not paths:
            return

        existing = set(self.image_paths)
        added = [p for p in paths if p not in existing]
        self.image_paths.extend(added)

        self.hint_lbl.place_forget()
        self._count_lbl.config(text=str(len(self.image_paths)))
        self._update_progress()
        self._on_vcanvas_configure()

        if self.current_index is None and self.image_paths:
            self._select(0)

        for p in added:
            self._thumb_loader.request(p)

    def _select(self, idx: int):
        self.current_index = idx
        path = self.image_paths[idx]

        self.load_lbl.config(text="loading…")
        self.update_idletasks()

        img = self._get_full_image(path)
        if img is None:
            self.load_lbl.config(text="")
            return

        self._orig_image = img
        self.load_lbl.config(text="")
        self.status_lbl.config(
            text=f"{idx+1}/{len(self.image_paths)}\n{os.path.basename(path)}")
        self._scroll_vlist_to(idx)
        self._render_vlist()
        self._draw_image()
        self._prefetch(idx)

    def _clear_all(self):
        if not self.image_paths:
            return
        if messagebox.askyesno("Clear all", "Remove all images and pivots?"):
            self.image_paths.clear()
            self.pivots.clear()
            self._img_cache.clear()
            self.current_index = None
            self._orig_image   = None
            self._photo        = None
            self.canvas.delete("all")
            self.hint_lbl.place(relx=0.5, rely=0.5, anchor="center")
            self._count_lbl.config(text="")
            self._render_vlist()
            self._update_progress()
            self.pivot_lbl.config(text="—")
            self.pivot_sub.config(text="click image\nto set pivot")
            self.status_lbl.config(text="no image selected")

    # ── Canvas rendering ──────────────────────────────────────────────────────

    def _on_canvas_resize(self, _event=None):
        self._draw_image()

    def _draw_image(self):
        if self._orig_image is None:
            return
        self.canvas.delete("all")

        cw = self.canvas.winfo_width()
        ch = self.canvas.winfo_height()
        if cw < 2 or ch < 2:
            self.after(50, self._draw_image)
            return

        iw, ih  = self._orig_image.size
        scale   = min((cw - 2*CANVAS_PAD) / iw, (ch - 2*CANVAS_PAD) / ih, 1.0)
        nw, nh  = int(iw * scale), int(ih * scale)
        ox      = (cw - nw) // 2
        oy      = (ch - nh) // 2

        self._canvas_offset = (ox, oy)
        self._canvas_scale  = scale

        disp = self._orig_image.resize((nw, nh), Image.BILINEAR)
        self._photo = ImageTk.PhotoImage(disp)
        self.canvas.create_image(ox, oy, anchor="nw", image=self._photo)
        self.canvas.create_rectangle(ox-1, oy-1, ox+nw, oy+nh,
                                     outline=BORDER, width=1)

        path = self.image_paths[self.current_index]
        if path in self.pivots:
            px_pct, py_pct = self.pivots[path]
            self._draw_crosshair(px_pct, py_pct)
            self._update_info_panel(px_pct, py_pct)

    def _draw_crosshair(self, px_pct: float, py_pct: float):
        ox, oy = self._canvas_offset
        scale  = self._canvas_scale
        iw, ih = self._orig_image.size
        nw, nh = int(iw * scale), int(ih * scale)
        cx = ox + int(px_pct / 100 * nw)
        cy = oy + nh - int(py_pct / 100 * nh)
        R  = 10
        self.canvas.delete("crosshair")
        self.canvas.create_oval(cx-R, cy-R, cx+R, cy+R,
                                outline=CROSS_RING, width=2, tags="crosshair")
        self.canvas.create_oval(cx-3, cy-3, cx+3, cy+3,
                                fill=CROSS_COL, outline="", tags="crosshair")
        for x0, y0, x1, y1 in [(cx-R-6, cy, cx-R+2, cy),
                                (cx+R-2, cy, cx+R+6, cy),
                                (cx, cy-R-6, cx, cy-R+2),
                                (cx, cy+R-2, cx, cy+R+6)]:
            self.canvas.create_line(x0, y0, x1, y1,
                                    fill=CROSS_COL, width=2, tags="crosshair")

    def _update_info_panel(self, px_pct: float, py_pct: float):
        self.pivot_lbl.config(text=f"{px_pct:.1f}\n{py_pct:.1f}", fg=ACCENT2)
        self.pivot_sub.config(
            text=f"x: {px_pct:.2f}%\ny: {py_pct:.2f}%\n(from bottom-left)",
            fg=TEXT_DIM)

    def _update_progress(self):
        n_set = sum(1 for p in self.image_paths if p in self.pivots)
        total = len(self.image_paths)
        self.progress_lbl.config(text=f"{n_set} / {total}  set", fg=SUCCESS)

    # ── Click: set pivot + auto-advance ──────────────────────────────────────

    def _on_canvas_click(self, event):
        if self.current_index is None or self._orig_image is None:
            return

        ox, oy = self._canvas_offset
        scale  = self._canvas_scale
        iw, ih = self._orig_image.size
        nw, nh = int(iw * scale), int(ih * scale)

        if not (ox <= event.x <= ox + nw and oy <= event.y <= oy + nh):
            return

        rel_x  = event.x - ox
        rel_y  = event.y - oy
        px_pct = rel_x / nw * 100
        py_pct = (nh - rel_y) / nh * 100

        path = self.image_paths[self.current_index]
        self.pivots[path] = (round(px_pct, 4), round(py_pct, 4))

        self._draw_crosshair(px_pct, py_pct)
        self._update_info_panel(px_pct, py_pct)
        self._update_progress()
        self._render_vlist()

        # Auto-advance
        next_idx = self.current_index + 1
        if next_idx < len(self.image_paths):
            self.after(60, lambda: self._select(next_idx))
        else:
            self.progress_lbl.config(fg=ACCENT2, text="✦ all done!")
            self.after(1500, self._update_progress)

    # ── Keyboard navigation ───────────────────────────────────────────────────

    def _bind_keys(self):
        self.bind("<Left>",  lambda e: self._step(-1))
        self.bind("<Right>", lambda e: self._step(1))
        self.bind("<Up>",    lambda e: self._step(-1))
        self.bind("<Down>",  lambda e: self._step(1))

    def _step(self, delta: int):
        if not self.image_paths:
            return
        idx = (self.current_index or 0) + delta
        idx = max(0, min(len(self.image_paths) - 1, idx))
        self._select(idx)

    # ── Save ─────────────────────────────────────────────────────────────────

    def _save(self):
        if not self.image_paths:
            messagebox.showinfo("Nothing to save", "Add some images first.")
            return

        saved, missing = [], []
        for path in self.image_paths:
            if path in self.pivots:
                px, py = self.pivots[path]
                out    = os.path.splitext(path)[0] + ".txt"
                with open(out, "w", encoding="utf-8") as f:
                    f.write(f"{px:.4f} {py:.4f}")
                saved.append(os.path.basename(out))
            else:
                missing.append(os.path.basename(path))

        msg = f"Saved {len(saved)} file(s) next to their images."
        if missing:
            msg += f"\n\nSkipped {len(missing)} image(s) with no pivot set."
        messagebox.showinfo("Saved ✓", msg)


# ── Entry point ───────────────────────────────────────────────────────────────
if __name__ == "__main__":
    try:
        from PIL import Image, ImageTk
    except ImportError:
        import subprocess, sys
        subprocess.check_call([sys.executable, "-m", "pip", "install", "Pillow"])
        from PIL import Image, ImageTk

    app = PivotApp()
    app.mainloop()