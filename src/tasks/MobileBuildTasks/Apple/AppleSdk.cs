// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Apple.Build
{
    public sealed class AppleSdk
    {
        private string devRoot;
        private string platformName;
        private string sdkDir;
        private string sdkRoot;

        private TaskLoggingHelper logger;

        public AppleSdk(string targetOS, TaskLoggingHelper logger)
        {
            this.logger = logger;

            platformName = GetPlatformName(targetOS);
            devRoot = GetXCodeDevRoot();

            sdkDir = Path.Combine(devRoot, "Contents", "Developer", "Platforms", $"{platformName}.platform", "Developer", "SDKs");
            sdkRoot = GetSdkRoot(sdkDir, platformName);
        }

        public string DeveloperRoot
        {
            get => devRoot;
        }

        public string SdkRoot
        {
            get => sdkRoot;
        }

        private static string GetSdkRoot(string sdkDir, string platformName)
        {
            string sdkRoot = "";

            if (!Directory.Exists(sdkDir))
            {
                return sdkRoot;
            }

            List<string> sdks = new List<string>();

            foreach (var dir in Directory.GetDirectories(sdkDir))
            {
                if (!File.Exists(Path.Combine(dir, "SDKSettings.plist")))
                {
                    continue;
                }

                string d = Path.GetFileName(dir);
                if (!d.StartsWith(platformName, StringComparison.Ordinal))
                {
                    continue;
                }

                d = d.Substring(platformName.Length);
                if (!d.EndsWith(".sdk", StringComparison.Ordinal))
                {
                    continue;
                }

                d = d.Substring(0, d.Length - ".sdk".Length);
                if (d.Length > 0)
                {
                    sdks.Add(d);
                }
            }

            if (sdks.Count > 0)
            {
                string version = sdks[sdks.Count - 1];
                sdkRoot = Path.Combine(sdkDir, $"{platformName}{version}.sdk");
            }

            return sdkRoot;
        }

        private static string GetXCodeDevRoot()
        {
            string path = "";

            if (!File.Exists ("/usr/bin/xcode-select"))
            {
                return path;
            }

            try
            {
                Process process = new Process ();
                process.StartInfo.FileName = "/usr/bin/xcode-select";
                process.StartInfo.Arguments = "--print-path";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.Start();

                string stdout = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                stdout = stdout.Trim();
                if (Directory.Exists(stdout))
                {
                    if (stdout.EndsWith("/Contents/Developer", StringComparison.Ordinal))
                    {
                        stdout = stdout.Substring(0, stdout.Length - "/Contents/Developer".Length);
                    }

                    path = stdout;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not get installed xcode location", e);
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new Exception("Could not get installed xcode location");
            }

            return path;
        }

        public static string GetPlatformName(string targetOS) =>
            targetOS switch
            {
                "ios" => "iPhoneOS",
                "iphonesimulator" => "iPhoneSimulator",
                "tvos" => "AppleTVOS",
                "tvos-simulator" => "AppleTVSimulator",
                _ => throw new ArgumentException($"{targetOS} does not have a valid platform name")
            };
    }
}
