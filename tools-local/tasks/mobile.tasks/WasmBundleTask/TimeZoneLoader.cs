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

public class TimeZoneLoader : Task
{
    [Required]
    public string? InputDirectory { get; set; }

    [Required]
    public string? OutputDirectory { get; set; }

    [Required]
    public string? Version { get; set; }

    private void DownloadTimeZoneData() 
    {
        List<string> files = new List<string>() {"africa", "antarctica", "asia", "australasia", "etcetera", "europe", "northamerica", "southamerica", "zone1970.tab"};
        using (var client = new WebClient())
        {
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

    private void FilterTimeZoneData(string[] areas) 
    {
        var directoryInfo = new DirectoryInfo (OutputDirectory!);
        foreach (var entry in directoryInfo.EnumerateDirectories()) 
        {
            if (Array.IndexOf(areas, entry.Name) == -1) 
            {
                Directory.Delete(entry.FullName, true);
            }
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

        DownloadTimeZoneData();

        string[] areas = new string[] { "Africa", "America", "Antarctica", "Arctic", "Asia", "Atlantic", "Australia", "Europe", "Indian", "Pacific", "zone1970.tab"};
        
        string[] filtered = new string[] { "America/Los_Angeles", "Australia/Sydney", "Europe/London", "Pacific/Tongatapu", 
                                "America/Sao_Paulo", "Australia/Perth", "Africa/Nairobi", "Europe/Berlin",
                                "Europe/Moscow", "Africa/Tripoli", "America/Argentina/Catamarca", "Europe/Lisbon",
                                "America/St_Johns"};
        
        FilterTimeZoneData(areas);
        FilterZoneTab(filtered);

        return true;
    }
}