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

        // When ZipArchive.AllowZstandard is disabled, ZstandardStream should be trimmed.
        Type zstandardStream = GetCompressionType("System.IO.Compression.ZstandardStream");

        if (zstandardStream is not null)
        {
            Console.WriteLine("FAIL: ZstandardStream was not trimmed.");
            return -1;
        }

        Console.WriteLine("PASS: ZstandardStream was trimmed as expected.");
        return 100;
    }

    // The intention of this method is to ensure the trimmer doesn't preserve the Type.
    private static Type GetCompressionType(string name) =>
        typeof(ZipArchive).Assembly.GetType(name, throwOnError: false);
}
