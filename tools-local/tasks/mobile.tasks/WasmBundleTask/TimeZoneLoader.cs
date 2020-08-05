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
    public string? InputDirectory { get; set; }
    public string? OutputDirectory { get; set; }

    private void DownloadTimeZoneData() {
        using (var client = new WebClient())
        {
            client.DownloadFile("https://data.iana.org/time-zones/tzdata-latest.tar.gz", $"{InputDirectory}/tzdata.tar.gz");
        }

        string[] files = {"africa", "antarctica", "asia", "australasia", "etcetera", "europe", "northamerica", "southamerica"};

        using (Process process = new Process()) {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = "tar";
            process.StartInfo.Arguments = $"xvzf \"{InputDirectory}/tzdata.tar.gz\" -C \"{InputDirectory}\"";
            process.Start();
            process.WaitForExit();

            process.StartInfo.FileName = "zic";
            foreach (var f in files) {
                process.StartInfo.Arguments = $"-d \"{OutputDirectory}\" \"{InputDirectory}/{f}\"";
                process.Start();
                process.WaitForExit();
            }
        }
        File.Copy(Path.Combine(InputDirectory!,"zone1970.tab"), Path.Combine(OutputDirectory!,"zone1970.tab"));
    }

    private void FilterTimeZoneData(string[] areas) {
        var directoryInfo = new DirectoryInfo (OutputDirectory!);
        foreach (var entry in directoryInfo.EnumerateDirectories()) {
            if (Array.IndexOf(areas, entry.Name) == -1) {
                Directory.Delete(entry.FullName, true);
            }
        }
    }

    private void FilterZoneTab(string[] filters) {
        var oldPath = Path.Combine(OutputDirectory!, "zone1970.tab");
        var path = Path.Combine(OutputDirectory!, "zone.tab");
        var fileInfo = new FileInfo(oldPath);
        using (var readStream = fileInfo.OpenRead())
        using (StreamReader sr = new StreamReader(readStream))
        using (FileStream fs = File.OpenWrite(path)) 
        using (StreamWriter sw = new StreamWriter(fs)){
            string? line;
            while ((line = sr.ReadLine()) != null) {
                if (filters.Any(x => Regex.IsMatch(line, $@"\b{x}\b"))) {
                    sw.WriteLine(line);
                }
            }
        }
        File.Delete(oldPath);
    }

    public override bool Execute() {
        if (InputDirectory == null) {
            InputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "obj", "data", "input");
        }
        Directory.CreateDirectory(InputDirectory);

        if (OutputDirectory == null) {
            OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "obj", "data", "output");
        }
        Directory.CreateDirectory(OutputDirectory);

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