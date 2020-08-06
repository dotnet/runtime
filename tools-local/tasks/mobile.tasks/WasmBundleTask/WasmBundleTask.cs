using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Net;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;


public class WasmBundleTask : Task
{
    public string? InputDirectory { get; set; }
    public string? FileName { get; set; } 

    private (byte[] json_bytes, MemoryStream stream) EnumerateData() {
        var indices = new List<object[]>();
        var stream = new MemoryStream();
        
        var directoryInfo = new DirectoryInfo(InputDirectory!);
        
        foreach (var entry in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories)) {

            var relativePath = entry.FullName.Substring(InputDirectory!.Length).Trim('/');
            indices.Add(new object[] { relativePath, entry.Length });

            using (var readStream = entry.OpenRead())
                readStream.CopyTo(stream);
        }
        
        stream.Position = 0;
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(indices);
        
        return (jsonBytes, stream);
    }

    public override bool Execute()
    {
        (byte[] json_bytes, MemoryStream stream) data;

        if (InputDirectory == null) {
            throw new ArgumentException("Input directory doesn't exist.");
        }

        data = EnumerateData();
        
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
        
        return true;
    }
}

