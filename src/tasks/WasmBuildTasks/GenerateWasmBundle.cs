// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

public class GenerateWasmBundle : Task
{
    [Required]
    public string? InputDirectory { get; set; }

    [Required]
    public string? OutputFileName { get; set; }

    private (byte[] json_bytes, MemoryStream stream) EnumerateData()
    {
        var indices = new List<object[]>();
        var stream = new MemoryStream();

        var directoryInfo = new DirectoryInfo(InputDirectory!);

        foreach (var entry in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(InputDirectory!, entry.FullName);
            if (Path.DirectorySeparatorChar != '/')
                relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '/');

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
        if (!Directory.Exists(InputDirectory))
        {
            Log.LogError($"Input directory '{InputDirectory}' does not exist");
            return false;
        }

        (byte[] json_bytes, MemoryStream stream) data = EnumerateData();

        using (var file = File.Open(OutputFileName!, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
        {
            var lengthBytes = new byte[4];
            var magicBytes = Encoding.ASCII.GetBytes("talb");
            BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, data.json_bytes.Length);
            file.Write(magicBytes);
            file.Write(lengthBytes);
            file.Write(data.json_bytes);

            data.stream.CopyTo(file);
        }

        return true;
    }
}
