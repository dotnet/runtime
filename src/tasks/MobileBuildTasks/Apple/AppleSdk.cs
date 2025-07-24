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

        private string GetXCodeDevRoot()
        {
            string path = "";
            string output;

            if (!File.Exists ("/usr/bin/xcode-select"))
            {
                throw new Exception("Unable to locate Xcode via xcode-select. Please make sure Xcode is properly installed");
            }

            try
            {
                (int exitCode, output) = Utils.TryRunProcess(logger,
                                                                "/usr/bin/xcode-select",
                                                                "--print-path",
                                                                silent: true,
                                                                debugMessageImportance: MessageImportance.Low,
                                                                label: "xcode-select");

                output = output.Trim();
                if (Directory.Exists(output))
                {
                    if (output.EndsWith("/Contents/Developer", StringComparison.Ordinal))
                    {
                        output = output.Substring(0, output.Length - "/Contents/Developer".Length);
                    }

                    path = output;

                    if (string.IsNullOrEmpty(path))
                    {
                        throw new ArgumentException("Could not find the path to Xcode via xcode-select. Please make sure Xcode is properly installed.");
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not get installed Xcode location", e);
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
                "maccatalyst" => "MacOSX",
                _ => throw new ArgumentException($"{targetOS} does not have a valid platform name")
            };
    }
}
