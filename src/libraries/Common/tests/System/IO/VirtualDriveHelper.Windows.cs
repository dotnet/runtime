// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;

namespace System.IO
{
    // Adds test helper APIs to manipulate Windows virtual drives via SUBST.
    [SupportedOSPlatform("windows")]
    public class VirtualDriveHelper : IDisposable
    {
        // Temporary Windows directory that can be mounted to a drive letter using the subst command
        private string? _virtualDriveTargetDir = null;
        // Windows drive letter that points to a mounted directory using the subst command
        private char _virtualDriveLetter = default;

        /// <summary>
        /// If there is a SUBST'ed drive, Dispose unmounts it to free the drive letter.
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (VirtualDriveLetter != default)
                {
                    DeleteVirtualDrive(VirtualDriveLetter);
                    Directory.Delete(VirtualDriveTargetDir, recursive: true);
                }
            }
            catch { } // avoid exceptions on dispose
        }

        /// <summary>
        /// Returns the path of a folder that is to be mounted using SUBST.
        /// </summary>
        public string VirtualDriveTargetDir
        {
            get
            {
                if (_virtualDriveTargetDir == null)
                {
                    // Create a folder inside the temp directory so that it can be mounted to a drive letter with subst
                    _virtualDriveTargetDir = Path.Join(Path.GetTempPath(), Path.GetRandomFileName());
                    Directory.CreateDirectory(_virtualDriveTargetDir);
                }

                return _virtualDriveTargetDir;
            }
        }

        /// <summary>
        /// Returns the drive letter of a drive letter that represents a mounted folder using SUBST.
        /// </summary>
        public char VirtualDriveLetter
        {
            get
            {
                if (_virtualDriveLetter == default)
                {
                    // Mount the folder to a drive letter
                    _virtualDriveLetter = CreateVirtualDrive(VirtualDriveTargetDir);
                }
                return _virtualDriveLetter;
            }
        }

        ///<summary>
        /// On Windows, mounts a folder to an assigned virtual drive letter using the subst command.
        /// subst is not available in Windows Nano.
        /// </summary>
        private static char CreateVirtualDrive(string targetDir)
        {
            char driveLetter = GetNextAvailableDriveLetter();
            bool success = RunProcess(CreateProcessStartInfo("cmd", "/c", SubstPath, $"{driveLetter}:", targetDir));
            if (!success || !DriveInfo.GetDrives().Any(x => x.Name[0] == driveLetter))
            {
                throw new InvalidOperationException($"Could not create virtual drive {driveLetter}: with subst");
            }
            return driveLetter;

            // Finds the next unused drive letter and returns it.
            char GetNextAvailableDriveLetter()
            {
                List<char> existingDrives = DriveInfo.GetDrives().Select(x => x.Name[0]).ToList();

                // A,B are reserved, C is usually reserved
                IEnumerable<int> range = Enumerable.Range('D', 'Z' - 'D');
                IEnumerable<char> castRange = range.Select(x => Convert.ToChar(x));
                IEnumerable<char> allDrivesLetters = castRange.Except(existingDrives);

                if (!allDrivesLetters.Any())
                {
                    throw new ArgumentOutOfRangeException("No drive letters available");
                }

                return allDrivesLetters.First();
            }
        }

        /// <summary>
        /// On Windows, unassigns the specified virtual drive letter from its mounted folder.
        /// </summary>
        private static void DeleteVirtualDrive(char driveLetter)
        {
            bool success = RunProcess(CreateProcessStartInfo("cmd", "/c", SubstPath, "/d", $"{driveLetter}:"));
            if (!success || DriveInfo.GetDrives().Any(x => x.Name[0] == driveLetter))
            {
                throw new InvalidOperationException($"Could not delete virtual drive {driveLetter}: with subst");
            }
        }

        private static ProcessStartInfo CreateProcessStartInfo(string fileName, params string[] arguments)
        {
            var info = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            foreach (var argument in arguments)
            {
                info.ArgumentList.Add(argument);
            }

            return info;
        }

        private static bool RunProcess(ProcessStartInfo startInfo)
        {
            using var process = Process.Start(startInfo);
            process.WaitForExit();
            return process.ExitCode == 0;
        }

        private static string SubstPath
        {
            get
            {
                string systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? @"C:\Windows";
                return Path.Join(systemRoot, "System32", "subst.exe");
            }
        }
    }
}