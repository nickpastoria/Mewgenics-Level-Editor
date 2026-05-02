param([switch]$Execute)

$spawnsDir = "D:\Unity\Mewgenics Level Editor\Assets\StreamingAssets\spawns"
$dryRun = -not $Execute
if ($dryRun) { Write-Host "=== DRY RUN - no changes will be made ===" -ForegroundColor Yellow }
else { Write-Host "=== EXECUTING CHANGES ===" -ForegroundColor Green }

$deletedCount = 0
$renamedCount = 0

function Remove-Asset($path) {
    Write-Host "  DELETE: $path" -ForegroundColor Red
    if (-not $dryRun) {
        Remove-Item $path -Force -ErrorAction SilentlyContinue
        if (Test-Path "$path.meta") { Remove-Item "$path.meta" -Force }
    }
    $script:deletedCount++
}

function Remove-AssetFolder($path) {
    Write-Host "  DELETE FOLDER: $path" -ForegroundColor Red
    if (-not $dryRun) { Remove-Item $path -Recurse -Force }
    $script:deletedCount++
}

function Rename-Asset($oldPath, $newName) {
    Write-Host "  RENAME: $(Split-Path $oldPath -Leaf) -> $newName" -ForegroundColor Cyan
    if (-not $dryRun) {
        $metaOld = "$oldPath.meta"
        Rename-Item $oldPath $newName -Force
        $newPath = Join-Path (Split-Path $oldPath) $newName
        if (Test-Path $metaOld) { Rename-Item $metaOld "$newName.meta" -Force }
    }
    $script:renamedCount++
}

function Rename-AssetFolder($oldPath, $newName) {
    Write-Host "  RENAME FOLDER: $(Split-Path $oldPath -Leaf) -> $newName" -ForegroundColor Cyan
    if (-not $dryRun) {
        $metaOld = "$oldPath.meta"
        Rename-Item $oldPath $newName -Force
        if (Test-Path $metaOld) { Rename-Item $metaOld "$newName.meta" -Force }
    }
    $script:renamedCount++
}

# ─────────────────────────────────────────────
# STEP 1: Delete exact-duplicate " 1" folders
# ─────────────────────────────────────────────
Write-Host "`n[STEP 1] Deleting duplicate trailing-number folders"
$duplicateFolders = @(
    "CharmedKittenPortrait 1",
    "DybbukChampionPortrait 1",
    "FatRatPortrait 1",
    "FetusPortrait 1",
    "FloatingHivePortrait 1",
    "GatekeeperChampionPortrait 1",
    "Guillotina2HeadPortrait 1",
    "NoHeadChampionPortrait 1",
    "SnakeChampionPortrait 1",
    "SnakeyBonesChampionPortrait 1",
    "T3HitlerChampionPortrait 1",
    "ThiefCatChampionPortrait 1",
    "WaterKittenPortrait 1"
)
foreach ($name in $duplicateFolders) {
    $path = Join-Path $spawnsDir $name
    if (Test-Path $path) { Remove-AssetFolder $path }
}

# ─────────────────────────────────────────────
# STEP 2: Remove extra PNGs from portrait folders (keep *Portrait.png)
# ─────────────────────────────────────────────
Write-Host "`n[STEP 2] Removing extra PNGs from portrait folders"
Get-ChildItem $spawnsDir -Directory | Where-Object { $_.Name -match "Portrait" } | ForEach-Object {
    $pngs = Get-ChildItem $_.FullName -Filter "*.png"
    if ($pngs.Count -gt 1) {
        $extras = $pngs | Where-Object { $_.Name -notmatch "Portrait\.png$" }
        foreach ($f in $extras) { Remove-Asset $f.FullName }
    }
}

# ─────────────────────────────────────────────
# STEP 3: Fix non-portrait multi-PNG folders
# ─────────────────────────────────────────────
Write-Host "`n[STEP 3] Fixing non-portrait multi-PNG folders"

# HarpoonTrap: 4 numbered files -> rename 5025 to "Harpoon Trap", delete rest
$ht = Join-Path $spawnsDir "HarpoonTrap"
if (Test-Path $ht) {
    foreach ($id in @(5025, 5026, 5027, 5028)) {
        $png = Join-Path $ht "$id.png"
        $txt = Join-Path $ht "$id.txt"
        if ($id -eq 5025) {
            if (Test-Path $png) { Rename-Asset $png "Harpoon Trap.png" }
            if (Test-Path $txt) { Rename-Asset $txt "Harpoon Trap.txt" }
        } else {
            if (Test-Path $png) { Remove-Asset $png }
            if (Test-Path $txt) { Remove-Asset $txt }
        }
    }
}

# Other non-portrait multi-PNG fixes (keep = spawns.gon name, delete = old/wrong name)
$fixes = [ordered]@{
    "BabyDeathWorm"  = @("Baby Death Worm.png")
    "CovenCandle"    = @("Small Candle.png")
    "Hydrant"        = @("Hydrant.png")
    "ManaPickup"     = @("Mana Pickup.png")
    "SpikyCactus2x2" = @("Spiky Cactus 2x 2.png")
    "SpikyCactusTall"= @("Spiky Cactus Tall.png")
    "Tire"           = @("Tire (Up).png")
    "TNTCrate"       = @("Mini Nuke.png")
}
foreach ($folder in $fixes.Keys) {
    $dir = Join-Path $spawnsDir $folder
    foreach ($bad in $fixes[$folder]) {
        $path = Join-Path $dir $bad
        if (Test-Path $path) { Remove-Asset $path }
    }
}

# ─────────────────────────────────────────────
# STEP 4: Rename Champion -> Elite everywhere
# ─────────────────────────────────────────────
Write-Host "`n[STEP 4] Renaming Champion folders/files to Elite"
$championFolders = Get-ChildItem $spawnsDir -Directory | Where-Object { $_.Name -match "Champion" } | Sort-Object Name

foreach ($folder in $championFolders) {
    $oldFolder = $folder.Name
    $newFolder = "Elite" + ($oldFolder -replace "Champion", "")

    # Rename files inside first
    Get-ChildItem $folder.FullName | Where-Object { $_.Extension -in ".png", ".txt" } | ForEach-Object {
        $oldFile = $_.Name
        $newFile = $oldFile -replace "^(.+?)\s*Champion\s*(.+)$", 'Elite $1 $2'
        if ($newFile -ne $oldFile) { Rename-Asset $_.FullName $newFile }
    }

    # Then rename the folder
    if ($newFolder -ne $oldFolder) { Rename-AssetFolder $folder.FullName $newFolder }
}

Write-Host "`n=== DONE: $renamedCount renames, $deletedCount deletions ===" -ForegroundColor Green
