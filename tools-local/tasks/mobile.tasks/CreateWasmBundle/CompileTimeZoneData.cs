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

    public const string ZoneTabFileName = "zone1970.tab";

    private void CompileTimeZoneDataSource() 
    {
        List<string> files = new List<string>() {"africa", "antarctica", "asia", "australasia", "etcetera", "europe", "northamerica", "southamerica"};    

        using (Process process = new Process()) 
        {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = "zic";
            foreach (var f in files) 
            {
                process.StartInfo.Arguments = $"-d \"{OutputDirectory}\" \"{Path.Combine(InputDirectory!, f)}\"";
                process.Start();
                process.WaitForExit();
            }
        }
    }

    private void FilterTimeZoneData() 
    {
        //  Remove unnecessary timezone files 
        foreach (var entry in new DirectoryInfo (OutputDirectory!).EnumerateFiles()) 
        {
            if (entry.Name != ZoneTabFileName)
                File.Delete(entry.FullName);
        }
    }

    private void FilterZoneTab(string[] filters) 
    {
        var oldPath = Path.Combine(InputDirectory!, ZoneTabFileName);
        var path = Path.Combine(OutputDirectory!, "zone.tab");
        using (StreamReader sr = new StreamReader(oldPath))
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
        File.Delete(oldPath);
    }

    public override bool Execute() 
    {
        if (!Directory.Exists(OutputDirectory))
            Directory.CreateDirectory(OutputDirectory!);

        if (!File.Exists(Path.Combine(InputDirectory!, ZoneTabFileName))) {
            Log.LogError($"Could not find required file {Path.Combine(InputDirectory!, ZoneTabFileName)}"); 
            return false;
        }

        CompileTimeZoneDataSource();
        
        string[] filtered = new string[] { "America/Los_Angeles", "Australia/Sydney", "Europe/London", "Pacific/Tongatapu", 
                                "America/Sao_Paulo", "Australia/Perth", "Africa/Nairobi", "Europe/Berlin",
                                "Europe/Moscow", "Africa/Tripoli", "America/Argentina/Catamarca", "Europe/Lisbon",
                                "America/St_Johns"};
        
        FilterTimeZoneData();
        FilterZoneTab(filtered);

        return true;
    }
}