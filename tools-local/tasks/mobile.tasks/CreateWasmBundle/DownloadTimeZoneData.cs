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

public class DownloadTimeZoneData : Task
{
    private const string _zone1970FileName = "zone1970.tab";

    [Required]
    public string[]? TimeZones { get; set; }

    [Required]
    public string? InputDirectory { get; set; }

    [Required]
    public string? OutputDirectory { get; set; }

    private void CompileTimeZoneDataSource()
    {
        using (Process process = new Process())
        {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = "zic";
            foreach (var tzName in TimeZones!)
            {
                process.StartInfo.Arguments = $"-d \"{OutputDirectory}\" \"{Path.Combine(InputDirectory!, tzName)}\"";
                process.Start();
                process.WaitForExit();
            }
        }
    }

    private void FilterTimeZoneData()
    {
        //  Remove unnecessary timezone files in the root dir,
        //  for named timezones like `CST6CDT`, `MST`, etc.
        foreach (var entry in new DirectoryInfo (OutputDirectory!).EnumerateFiles())
        {
            File.Delete(entry.FullName);
        }
    }

    private void FilterZoneTab(string zoneFile, string[] filters)
    {
        var path = Path.Combine(OutputDirectory!, "zone.tab");
        using (StreamReader sr = new StreamReader(zoneFile))
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
        if (!Directory.Exists(OutputDirectory))
            Directory.CreateDirectory(OutputDirectory!);

        var zone1970FullPath = Path.Combine(InputDirectory!, _zone1970FileName);
        if (!File.Exists(zone1970FullPath))
        {
            Log.LogError($"Could not find required file ${zone1970FullPath}");
            return false;
        }

        CompileTimeZoneDataSource();

        string[] filtered = new string[] { "America/Los_Angeles", "Australia/Sydney", "Europe/London", "Pacific/Tongatapu",
                                "America/Sao_Paulo", "Australia/Perth", "Africa/Nairobi", "Europe/Berlin",
                                "Europe/Moscow", "Africa/Tripoli", "America/Argentina/Catamarca", "Europe/Lisbon",
                                "America/St_Johns"};

        FilterTimeZoneData();
        FilterZoneTab(zone1970FullPath, filtered);

        return !Log.HasLoggedErrors;
    }
}
