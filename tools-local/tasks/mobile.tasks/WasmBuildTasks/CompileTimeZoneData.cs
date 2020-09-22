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

    private const string ZoneTabFileName = "zone1970.tab";

    private void CompileTimeZoneDataSource()
    {
        using (Process process = new Process())
        {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = "zic";
            foreach (var f in TimeZones!)
            {
                process.StartInfo.Arguments = $"-d \"{OutputDirectory}\" \"{Path.Combine(InputDirectory!, f)}\"";
                process.Start();
                process.WaitForExit();
            }
        }
    }

    private void FilterTimeZoneData()
    {
        //  Remove unnecessary timezone files in the root dir
        //  for ex: `CST6CDT`, `MST`, etc.
        foreach (var entry in new DirectoryInfo (OutputDirectory!).EnumerateFiles())
        {
            File.Delete(entry.FullName);
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
    }

    public override bool Execute()
    {
        string ZoneTabFile = Path.Combine(InputDirectory!, ZoneTabFileName);

        if (!Directory.Exists(OutputDirectory))
            Directory.CreateDirectory(OutputDirectory!);

        if (!File.Exists(ZoneTabFile))
        {
            Log.LogError($"Could not find required file {ZoneTabFile}");
            return false;
        }

        CompileTimeZoneDataSource();

        string[] filtered = new string[] { "America/Los_Angeles", "Australia/Sydney", "Europe/London", "Pacific/Tongatapu",
                                "America/Sao_Paulo", "Australia/Perth", "Africa/Nairobi", "Europe/Berlin",
                                "Europe/Moscow", "Africa/Tripoli", "America/Argentina/Catamarca", "Europe/Lisbon",
                                "America/St_Johns"};

        FilterTimeZoneData();
        FilterZoneTab(filtered, ZoneTabFile);

        return !Log.HasLoggedErrors;
    }
}
