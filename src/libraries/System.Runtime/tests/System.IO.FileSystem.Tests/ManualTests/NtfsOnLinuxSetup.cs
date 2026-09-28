// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Xunit.Sdk;

namespace System.IO.ManualTests
{
    public class NtfsOnLinuxSetup : IDisposable
    {
        public NtfsOnLinuxSetup()
        {
            if (!NtfsOnLinuxTests.IsManualTestsEnabledAndElevated)
                throw new XunitException("Set MANUAL_TESTS envvar and run as elevated to execute this test setup.");
            
            ExecuteShell("""
                dd if=/dev/zero of=my_loop_device.img bs=1M count=100
                losetup /dev/loop99 my_loop_device.img
                mkfs -t ntfs /dev/loop99
                mkdir -p /mnt/ntfs
                mount /dev/loop99 /mnt/ntfs
                """);
        }

        public void Dispose()
        {
            ExecuteShell("""
                umount /mnt/ntfs
                losetup -d /dev/loop99
                rm my_loop_device.img
                """);
        }
        
        private static void ExecuteShell(string command)
        {
            using Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    ArgumentList = { "-c", command },
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.OutputDataReceived += (sender, e) => Console.WriteLine($"[OUTPUT] {e.Data}");
            process.ErrorDataReceived += (sender, e) => Console.WriteLine($"[ERROR] {e.Data}");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
    }
}
