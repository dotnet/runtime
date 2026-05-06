// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Compression;
using System.Globalization;
using System.Threading.Tasks;
using System.Resources;
using System.Runtime.InteropServices.JavaScript;
using System.Text;

public partial class ZipArchiveInteropTest
{
    [JSExport]
    public static async Task Run()
    {
        using var zipFileStream = new MemoryStream();
        using var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create);

        var entry = zipArchive.CreateEntry("sample.txt");
        using (var entryStream = entry.Open())
        {
            await entryStream.WriteAsync(Encoding.UTF8.GetBytes("Sample text content"));
        }

        TestOutput.WriteLine("Zip file created successfully.");
    }
}
