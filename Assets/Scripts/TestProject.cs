using UnityEngine;
using System;
using System.IO;
using SimpleFileBrowser;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;

[System.Serializable]
public static class TestProject
{
    public static void Launch(PersistentVariables vars)
    {
        string app = vars.MewgenicsDirectory;
        if (app == "" || app == null)
        {
            UnityEngine.Debug.Log("File Location Not Found");
            TestProject.SetGameDir(vars);
        }
        else
        {
            TestProject.AttemptLaunch(vars.MewgenicsDirectory, vars.defaultFileLocation);
        }
    }

    static IEnumerator SetGameDir(PersistentVariables vars)
    {
        EditorManager.Instance.mouseEnabled = false;
        // Set filters (optional)
		// It is sufficient to set the filters just once (instead of each time before showing the file browser dialog), 
		// if all the dialogs will be using the same filters
		FileBrowser.SetFilters( true, new FileBrowser.Filter( "Executable", ".exe"));

		// Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
		// Note that when you use this function, .lnk and .tmp extensions will no longer be
		// excluded unless you explicitly add them as parameters to the function
		FileBrowser.SetExcludedExtensions( ".lnk", ".tmp", ".zip", ".rar", ".exe" );

        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, "", null, "Select Game Executable", "Select" );

        if( FileBrowser.Success )
        {
            string fileDest = FileBrowser.Result[0];
            byte[] file = System.IO.File.ReadAllBytes(fileDest);
            UnityEngine.Debug.Log("File destination: " + fileDest);
            vars.MewgenicsDirectory = fileDest;
            TestProject.AttemptLaunch(vars.MewgenicsDirectory, vars.defaultFileLocation);
        }
        else
        {
            EditorManager.Instance.mouseEnabled = true;
        }
    }

    public static void AttemptLaunch(string app, string project)
    {
        string arguments = $"-dev_mode true -enable_debugconsole true -modpaths {project}";
        // 2. Configure the ProcessStartInfo
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = app;
        // Arguments containing spaces should be wrapped in quotes if required by the target app
        startInfo.Arguments = string.Format(@"""{0}""", arguments); 
        
        // Optional: Other useful properties
        startInfo.UseShellExecute = true; // Use the OS shell to start (default is true)
        // startInfo.WorkingDirectory = @"C:\path\to\working\directory"; // Set the working directory

        // 3. Start the process
        try
        {
            Process myProcess = Process.Start(startInfo);
            
            // Optional: Wait for the process to exit
            // myProcess.WaitForExit(); 
            // Console.WriteLine("External process exited with code: " + myProcess.ExitCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

}
