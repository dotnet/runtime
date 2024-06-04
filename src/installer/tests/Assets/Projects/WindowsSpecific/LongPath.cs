// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WindowsSpecific
{
    public static partial class Program
    {
        public static void LongPath(string dir)
        {
            // Create a path that is longer than MAX_PATH
            DirectoryInfo newDir = Directory.CreateDirectory(Path.Combine(dir, "longPath"));
            string longPath = Path.Combine(newDir.FullName, new string('x', 255));

            Console.WriteLine($"Trying to create `{longPath}`");
            bool success = CreateDirectoryW(longPath, IntPtr.Zero);
            Console.WriteLine($"CreateDirectoryW with long path {(success ? "succeeded" : $"failed with {Marshal.GetLastWin32Error()}" )}");

            Directory.Delete(newDir.FullName, true /*recursive*/);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool CreateDirectoryW(string path, IntPtr securityAttributes);
    }
}
