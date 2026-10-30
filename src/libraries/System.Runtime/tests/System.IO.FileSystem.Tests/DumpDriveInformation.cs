// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Tests
{
    public class DumpDriveInformation
    {
        // When running both inner and outer loop together, dump only once
        private static bool s_dumped = false;

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.Wasi, "Not applicable")]
        public void DumpDriveInformationToConsole()
        {
            if (s_dumped || !PlatformDetection.IsInHelix)
                return;

            s_dumped = true;

            // Not really a test, but useful to dump drive/volume information to the test log
            // to help debug environmental issues with mount volume tests in CI.
            // Follows the pattern of DescriptionNameTests.DumpRuntimeInformationToConsole.

            Console.WriteLine("### DRIVE INFORMATION");
            Console.WriteLine($"###   Machine: {Environment.MachineName}");
            Console.WriteLine($"###   OS: {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
            Console.WriteLine($"###   Current directory: {Directory.GetCurrentDirectory()}");

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                try
                {
                    string info = $"###   Drive {drive.Name}: Type={drive.DriveType}";
                    if (drive.IsReady)
                    {
                        info += $" Format={drive.DriveFormat} Size={drive.TotalSize / (1024 * 1024)}MB";

                        if (OperatingSystem.IsWindows())
                        {
                            char[] volName = new char[260];
                            bool hasGuid = DllImports.GetVolumeNameForVolumeMountPoint(drive.Name, volName, volName.Length);
                            info += hasGuid
                                ? $" VolumeGUID={new string(volName).TrimEnd('\0')}"
                                : $" VolumeGUID=NONE({Marshal.GetLastPInvokeErrorMessage()})";
                        }
                    }
                    else
                    {
                        info += " NotReady";
                    }
                    Console.WriteLine(info);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"###   Drive {drive.Name}: error probing: {ex.GetType().Name}: {ex.Message}");
                }
            }

            if (OperatingSystem.IsWindows())
            {
                string otherNtfs = IOServices.GetNtfsDriveOtherThanCurrent();
                Console.WriteLine($"###   GetNtfsDriveOtherThanCurrent() = {otherNtfs ?? "(null)"}");
            }
        }

        [Fact]
        [OuterLoop]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.Wasi, "Not applicable")]
        public void DumpDriveInformationToConsoleOuter()
        {
            // Outer loop runs don't run inner loop tests.
            // But we want to log this data for any Helix run.
            DumpDriveInformationToConsole();
        }
    }
}
