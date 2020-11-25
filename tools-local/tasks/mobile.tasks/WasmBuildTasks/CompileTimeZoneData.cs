// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class CompileTimeZoneData : Task
{
    [Required]
    public string? InputDirectory { get; set; }

    [Required]
    public string? OutputDirectory { get; set; }

    [Required]
    public string[]? TimeZones { get; set; }

    public bool FilterSystemTimeZones { get; set; }

    private const string ZoneTabFileName = "zone1970.tab";

    private bool CompileTimeZoneDataSource()
    {
        foreach (var f in TimeZones!)
        {
            using Process process = new ();

            // zic writes warnings over stderr
            process.ErrorDataReceived += (_, args) => Log.LogMessage(MessageImportance.Low, args.Data ?? string.Empty);
            process.OutputDataReceived += (_, args) => Log.LogMessage(MessageImportance.Low, args.Data ?? string.Empty);

            process.StartInfo  = new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = "zic",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Arguments = $"-d \"{OutputDirectory}\" \"{Path.Combine(InputDirectory!, f)}\""
            };

            Log.LogMessage(MessageImportance.Low, $"Running {process.StartInfo.FileName} {process.StartInfo.Arguments}");

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                Log.LogError($"{process.StartInfo.FileName} {process.StartInfo.Arguments} returned exit code {process.ExitCode}");
                return false;
            }
        }

        return true;
    }

    private void FilterTimeZoneData()
    {
        //  Remove unnecessary timezone files in the root dir
        //  for ex: `CST6CDT`, `MST`, etc.
        foreach (var entry in new DirectoryInfo (OutputDirectory!).EnumerateFiles())
        {
            File.Delete(entry.FullName);
            Log.LogMessage(MessageImportance.Low, $"Removing file created by zic: \"{entry.FullName}\".");
        }
    }

    private void FilterZoneTab(string[] filters, string ZoneTabFile)
    {
        var path = Path.Combine(OutputDirectory!, "zone.tab");
        using (StreamReader sr = new StreamReader(ZoneTabFile))
        using (StreamWriter sw = new StreamWriter(path))
        {
            string? line;
            while ((line = sr.ReadLine()) != null) {
                if (filters.Any(x => Regex.IsMatch(line, $@"\b{x}\b")))
                {
                    sw.WriteLine(line);
                }
            }
        }

        Log.LogMessage(MessageImportance.Low, $"Wrote \"{ZoneTabFile}\" filtered to \"{path}\".");
    }

    public override bool Execute()
    {
        string zoneTabFilePath = Path.Combine(InputDirectory!, ZoneTabFileName);

        if (!Directory.Exists(OutputDirectory))
            Directory.CreateDirectory(OutputDirectory!);

        if (!File.Exists(zoneTabFilePath))
        {
            Log.LogError($"Could not find required file {zoneTabFilePath}");
            return false;
        }

        if (!CompileTimeZoneDataSource())
            return !Log.HasLoggedErrors;

        FilterTimeZoneData();

        if (FilterSystemTimeZones)
        {
            string[] filtered = new string[] { "America/Los_Angeles", "Australia/Sydney", "Europe/London", "Pacific/Tongatapu",
                                "America/Sao_Paulo", "Australia/Perth", "Africa/Nairobi", "Europe/Berlin",
                                "Europe/Moscow", "Africa/Tripoli", "America/Argentina/Catamarca", "Europe/Lisbon",
                                "America/St_Johns"};
            FilterZoneTab(filtered, zoneTabFilePath);
        }
        else
        {
            string dest = Path.Combine(OutputDirectory!, "zone.tab");
            File.Copy(zoneTabFilePath, dest, true);
            Log.LogMessage(MessageImportance.Low, $"Copying file from \"{zoneTabFilePath}\" to \"{dest}\".");
        }

        return !Log.HasLoggedErrors;
    }
}
