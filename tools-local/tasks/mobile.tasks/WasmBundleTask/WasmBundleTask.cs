using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Diagnostics;

public class WasmBundleTask : Task
{
    public string? InputDirectory { get; set; }
    public string? OutputDirectory { get; set; }
    public string? Type { get; set; }
    public string? FileName { get; set; } 

    private (byte[] json_bytes, MemoryStream stream) enumerateData (string[] sub_folders, string output_folder) {
        var indices = new List<object[]>();
        var stream = new MemoryStream();

        foreach (var folder in sub_folders)
        {
            var path = Path.Combine(output_folder, folder);
            if (folder == "zone1970.tab") {
                var fileInfo = new FileInfo(path);
                indices.Add(new object[] { "zone.tab", fileInfo.Length});
                string[] systemtz = { "America/Los_Angeles", "Australia/Sydney", "Europe/London", "Pacific/Tongatapu", 
                                "America/Sao_Paulo", "Australia/Perth", "Africa/Nairobi", "Europe/Berlin",
                                "Europe/Moscow", "Africa/Tripoli", "America/Argentina/Catamarca", "Europe/Lisbon",
                                "America/St_Johns"};
                using (var readStream = fileInfo.OpenRead()) 
                using (StreamReader sr = new StreamReader(readStream)) 
                using (MemoryStream ms = new MemoryStream())
                using (StreamWriter ws = new StreamWriter(ms)){
                    string? line;
                    while ((line = sr.ReadLine()) != null) {
                        if (systemtz.Any(x => Regex.IsMatch(line, $@"\b{x}\b"))) {
                            ws.WriteLine(line);
                        }
                    }
                    ws.Flush();
                    ms.Position = 0;
                    ms.CopyTo(stream);
                }
            } else {
                var directoryInfo = new DirectoryInfo(path);
                foreach (var entry in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    var relativePath = entry.FullName.Substring(output_folder.Length).Trim('/');
                    indices.Add(new object[] { relativePath, entry.Length});

                    using (var readStream = entry.OpenRead())
                        readStream.CopyTo(stream);
                }
            }
        }
        
        stream.Position = 0;
        var options = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(indices);
        
        return (jsonBytes, stream);
    }

    private (byte[] json_bytes, MemoryStream stream) readTimeZone (string folder) {
        // https://en.wikipedia.org/wiki/Tz_database#Area
        var areas = new[] { "Africa", "America", "Antarctica", "Arctic", "Asia", "Atlantic", "Australia", "Europe", "Indian", "Pacific", "zone1970.tab"};

        return enumerateData (areas, folder);
    }

    private (byte[] json_bytes, MemoryStream stream) readGeneralData (string input_folder) {
        var DirectoryInfo = new DirectoryInfo (input_folder);
        string[] sub_folders = DirectoryInfo.EnumerateFileSystemInfos().Select(f => f.Name).ToArray();

        return enumerateData (sub_folders, input_folder);
    }

    private void DownloadTimeZoneData (string input_folder, string output_folder) {
        using (var client = new WebClient())
        {
            client.DownloadFile("https://data.iana.org/time-zones/tzdata-latest.tar.gz", $"{input_folder}/tzdata.tar.gz");
        }

        string[] files = {"africa", "antarctica", "asia", "australasia", "etcetera", "europe", "northamerica", "southamerica"};

        using (Process process = new Process()) {
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.FileName = "tar";
            process.StartInfo.Arguments = $"xvzf \"{input_folder}/tzdata.tar.gz\" -C \"{input_folder}\"";
            process.Start();
            process.WaitForExit();

            process.StartInfo.FileName = "zic";
            foreach (var f in files) {
                process.StartInfo.Arguments = $"-d \"{output_folder}\" \"{input_folder}/{f}\"";
                process.Start();
                process.WaitForExit();
            }
        }
        File.Copy(Path.Combine(input_folder,"zone1970.tab"), Path.Combine(output_folder,"zone1970.tab"));
    }

    public override bool Execute ()
    {
        (byte[] json_bytes, MemoryStream stream) data;
        
        if (OutputDirectory == null)
        {
            OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "obj", "data", "output");
        }
        Directory.CreateDirectory(OutputDirectory);

        if (InputDirectory == null) {
            InputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "obj", "data", "input");
        }
        Directory.CreateDirectory(InputDirectory);
        
        if (Type == "timezone") {
            DownloadTimeZoneData (InputDirectory, OutputDirectory);
            data = readTimeZone (OutputDirectory);
        }
        else {
            data = readGeneralData (InputDirectory);
        }
        
        if (FileName == null) {
            throw new ArgumentException($"Invalid file name");
        }
        using (var file = File.OpenWrite(FileName))
        {
            var jsonBytes = data.json_bytes;
            var stream = data.stream;
            var bytes = new byte[4];
            var magicBytes = Encoding.ASCII.GetBytes("talb");
            BinaryPrimitives.WriteInt32LittleEndian(bytes, jsonBytes.Length);
            file.Write(magicBytes);
            file.Write(bytes);
            file.Write(jsonBytes);
            
            stream.CopyTo(file);
        }
        Directory.Delete(InputDirectory, true);
        Directory.Delete(OutputDirectory, true);
        
        return true;
    }
}

