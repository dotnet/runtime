// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.WebAssembly.AppHost;

public static class FileUtils
{
    private static readonly string[] s_extensions = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                                                        ? new[] { ".exe", ".cmd", ".bat" }
                                                        : new[] { "" };

    public static bool TryFindExecutableInPATH(string filename, [NotNullWhen(true)] out string? fullPath, [NotNullWhen(false)] out string? errorMessage)
    {
        errorMessage = null;
        fullPath = null;
        if (File.Exists(filename))
        {
            fullPath = Path.GetFullPath(filename);
            return true;
        }

        if (Path.IsPathRooted(filename))
        {
            fullPath = filename;
            return true;
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
        {
            errorMessage = "Could not find environment variable PATH";
            return false;
        }

        string[] searchPaths = path.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
        if (searchPaths.Length == 0)
        {
            errorMessage = $"No paths set in environment variable PATH";
            return false;
        }

        List<string> filenamesTried = new(s_extensions.Length);
        foreach (string extn in s_extensions)
        {
            string filenameWithExtn = filename + extn;
            filenamesTried.Add(filenameWithExtn);
            foreach (string searchPath in searchPaths)
            {
                var pathToCheck = Path.Combine(searchPath, filenameWithExtn);
                if (File.Exists(pathToCheck))
                {
                    fullPath = pathToCheck;
                    return true;
                }
            }
        }

        // Could not find the path
        errorMessage = $"Tried to look for {string.Join(", ", filenamesTried)} in PATH: {string.Join(", ", searchPaths)} .";
        return false;
    }
}
