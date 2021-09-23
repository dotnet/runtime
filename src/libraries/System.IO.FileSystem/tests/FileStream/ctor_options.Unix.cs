// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO.Tests
{
    public partial class FileStream_ctor_options
    {
        private static long GetAllocatedSize(FileStream fileStream)
        {
            bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            // Call 'stat' to get the number of blocks, and size of blocks.
            using var px = Process.Start(new ProcessStartInfo
            {
                FileName = "stat",
                ArgumentList = { isOSX ? "-f"    : "-c",
                                 isOSX ? "%b %k" : "%b %B",
                                 fileStream.Name },
                RedirectStandardOutput = true
            });
            string stdout = px.StandardOutput.ReadToEnd();

            string[] parts = stdout.Split(' ');
            return long.Parse(parts[0]) * long.Parse(parts[1]);
        }

        private static bool SupportsPreallocation =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        // Mobile platforms don't support Process.Start.
        private static bool IsGetAllocatedSizeImplemented => !PlatformDetection.IsMobile;
    }
}
