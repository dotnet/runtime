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
    [Required]
    public string? InputDirectory { get; set; }

    [Required]
    public string? OutputDirectory { get; set; }

    [Required]
    public string? Version { get; set; }

    private void DownloadTimeZoneDataSource() 
    {
        List<string> files = new List<string>() {"africa", "antarctica", "asia", "australasia", "etcetera", "europe", "northamerica", "southamerica", "zone1970.tab"};
        using (var client = new WebClient())
        {
            Console.WriteLine("Downloading TimeZone data files");
            foreach (var file in files) 
            {
                client.DownloadFile($"https://data.iana.org/time-zones/tzdb-{Version}/{file}", $"{Path.Combine(InputDirectory!, file)}");
            }
        }

        files.Remove("zone1970.tab");        

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
        File.Copy(Path.Combine(InputDirectory!,"zone1970.tab"), Path.Combine(OutputDirectory!,"zone1970.tab"));
    }

    private void FilterTimeZoneData() 
    {
        //  Remove unnecessary timezone files 
        foreach (var entry in new DirectoryInfo (OutputDirectory!).EnumerateFiles()) 
        {
            if (entry.Name != "zone1970.tab")
                File.Delete(entry.FullName);
        }
    }

    private void FilterZoneTab(string[] filters) 
    {
        var oldPath = Path.Combine(OutputDirectory!, "zone1970.tab");
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

        if (!Directory.Exists(InputDirectory))
            Directory.CreateDirectory(InputDirectory!);
        
        if (!Directory.Exists(OutputDirectory))
            Directory.CreateDirectory(OutputDirectory!);

        DownloadTimeZoneDataSource();
        
        string[] filtered = new string[] { "America/Los_Angeles", "Australia/Sydney", "Europe/London", "Pacific/Tongatapu", 
                                "America/Sao_Paulo", "Australia/Perth", "Africa/Nairobi", "Europe/Berlin",
                                "Europe/Moscow", "Africa/Tripoli", "America/Argentina/Catamarca", "Europe/Lisbon",
                                "America/St_Johns"};
        
        FilterTimeZoneData();
        FilterZoneTab(filtered);

        return true;
    }
}