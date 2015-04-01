// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

public static partial class StreamHelpers
{
    public static String GetTmpFileName()
    {
        return Path.GetRandomFileName();
    }

    const string dirPrefix = "ZipTests";
    static int dirCount = 1;
    public static String GetTmpPath(bool create = false)
    {
        var root = Path.GetTempPath();
        string subDir = dirPrefix + dirCount.ToString();
        var tempPath = Path.Combine(root, subDir);
        dirCount++;

        while (Directory.Exists(tempPath))
        {
            subDir = dirPrefix + dirCount.ToString();
            tempPath = Path.Combine(root, subDir);
            dirCount++;
        }
        
        if (create)
        {
            Directory.CreateDirectory(tempPath);
        }

        return tempPath;
    }

    public static async Task<Stream> CreateTempCopyStream(String path)
    {
        Console.WriteLine("CreateTempCopyStream for: " + path);

        var bytes = File.ReadAllBytes(path);

        var ms = new MemoryStream();
        await ms.WriteAsync(bytes, 0 , bytes.Length);
        ms.Position = 0;

        return ms;
    }

    public static String CreateTempCopyFile(String path)
    {
        Console.WriteLine("CreateTempCopyFile for: " + path);
        var bytes = File.ReadAllBytes(path);

        var dir = Path.GetDirectoryName(path);
        var newN = Path.Combine(dir, GetTmpFileName());
        File.WriteAllBytes(newN, bytes);

        return newN;
    }

}
