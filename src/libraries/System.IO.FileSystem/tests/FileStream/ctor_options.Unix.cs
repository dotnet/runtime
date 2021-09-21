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
            // Call 'stat' to get the number of blocks, and size of blocks.
            using var px = Process.Start(new ProcessStartInfo
            {
                FileName = "stat",
                ArgumentList = { "-c", "%b %B", fileStream.Name },
                RedirectStandardOutput = true
            });
            string stdout = px.StandardOutput.ReadToEnd();

            string[] parts = stdout.Split(' ');
            return long.Parse(parts[0]) * long.Parse(parts[1]);
        }

        private static bool SupportsPreallocation =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}
