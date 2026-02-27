// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.IO.Compression;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length > 0)
        {
            ZipFile.ExtractToDirectory(args[0], args[1]);

            return 100;
        }

        // When no arguments are provided, check whether ZstandardStream is rooted.
        // Since ZipArchive references ZstandardStream for decompression, the trimmer should keep it.
        Type zstandardStream = typeof(ZipArchive).Assembly.GetType("System.IO.Compression.ZstandardStream", throwOnError: false);

        if (zstandardStream is null)
        {
            Console.WriteLine("FAIL: ZstandardStream was trimmed unexpectedly.");
            return -1;
        }

        Console.WriteLine("PASS: ZstandardStream is present as expected.");
        return 100;
    }
}
