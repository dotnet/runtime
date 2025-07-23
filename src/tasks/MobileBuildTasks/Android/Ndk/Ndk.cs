// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;


namespace Microsoft.Android.Build.Ndk
{
    public sealed class Ndk
    {
        private static string ndkPath = "";
        private static NdkVersion? ndkVersion;

        public static string NdkPath
        {
            get
            {
                if (string.IsNullOrEmpty(ndkPath))
                {
                    ndkPath = GetNdkPath(GetProbingPaths());
                }

                return ndkPath!;
            }
        }

        public static NdkVersion NdkVersion
        {
            get
            {
#pragma warning disable IDE0074 // Use compound assignment
                if (ndkVersion == null)
                {
                    ndkVersion = ReadVersion();
                }
#pragma warning restore IDE0074

                return ndkVersion;
            }
        }

        private static string GetNdkPath(IEnumerable<string> probingPaths)
        {
            string ret = "";

            foreach (string path in probingPaths)
            {
                if (Directory.Exists(path))
                {
                    ret = path;
                    break;
                }
            }

            return ret;
        }

        private static List<string> GetProbingPaths()
        {
            List<string> paths = new List<string>();

            string? ndkEnvPath = Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT");

            string[] fixedNdkPaths = (Utils.IsWindows()) ?
                new string[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk", "ndk-bundle"),
                    Path.Combine(Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86), "Android", "android-sdk-windows", "ndk-bundle"),
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ProgramW6432"))
                        ? Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? "", "Android", "android-sdk", "ndk-bundle")
                        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "android-sdk", "ndk-bundle"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "android-sdk", "ndk-bundle"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Android", "android-sdk", "ndk-bundle"),
                    @"C:\android-sdk-windows"
                }
                :
                new string[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Android", "sdk")
                };

            if (!string.IsNullOrEmpty(ndkEnvPath))
            {
                paths.Add(ndkEnvPath!);
            }

            paths.AddRange(fixedNdkPaths);

            return paths;
        }

        private static NdkVersion ReadVersion()
        {
            string sourcePropertiesPath = Path.Combine(NdkPath, "source.properties");
            if (!File.Exists(sourcePropertiesPath))
            {
                throw new Exception("Could not find NDK version information");
            }

            var splitChars = new char[] {'='};
            string? ver = null;
            foreach (string l in File.ReadAllLines(sourcePropertiesPath))
            {
                string line = l.Trim ();
                if (!line.StartsWith("Pkg.Revision", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] parts = line.Split(splitChars, 2);
                if (parts.Length != 2)
                {
                    throw new Exception($"Invalid NDK version format in '{sourcePropertiesPath}'");
                }

                ver = parts [1].Trim();
            }

            return new NdkVersion(ver);
        }
    }
}
