// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class KillOnParentExitTests : ProcessTestBase
    {
        [Fact]
        public void KillOnParentExit_DefaultsToFalse()
        {
            ProcessStartInfo startInfo = new();

            Assert.False(startInfo.KillOnParentExit);
        }

        [Fact]
        public void KillOnParentExit_CanBeSetToTrue()
        {
            ProcessStartInfo startInfo = new()
            {
                KillOnParentExit = true
            };

            Assert.True(startInfo.KillOnParentExit);
        }

        [Fact]
        public void KillOnParentExit_WithUseShellExecute_Throws()
        {
            ProcessStartInfo startInfo = new("dummy")
            {
                KillOnParentExit = true,
                UseShellExecute = true
            };

            Assert.Throws<InvalidOperationException>(() => Process.Start(startInfo));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void KillOnParentExit_ProcessStartsAndExitsNormally()
        {
            using Process process = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            process.StartInfo.KillOnParentExit = true;
            process.Start();

            Assert.True(process.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, process.ExitCode);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)] // (false, true) is tested by ProcessHandleTests
        public void KillOnParentExit_KillsTheChild_WhenParentExits(bool enabled, bool restrictInheritance)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;
            remoteInvokeOptions.StartInfo.RedirectStandardInput = true;

            using RemoteInvokeHandle childHandle = RemoteExecutor.Invoke(
                (enabledStr, limitInheritanceStr) =>
                {
                    using Process grandChild = CreateProcessLong();
                    grandChild.StartInfo.KillOnParentExit = bool.Parse(enabledStr);
                    grandChild.StartInfo.InheritedHandles = bool.Parse(limitInheritanceStr) ? [] : null;

                    grandChild.Start();
                    Console.WriteLine(grandChild.Id);

                    // This will block the child until parent provides input.
                    _ = Console.ReadLine();
                },
                enabled.ToString(),
                restrictInheritance.ToString(),
                remoteInvokeOptions);

            int grandChildPid = int.Parse(childHandle.Process.StandardOutput.ReadLine());

            // Obtain a Process instance before the child exits to avoid PID reuse issues.
            using Process grandchild = Process.GetProcessById(grandChildPid);

            try
            {
                childHandle.Process.StandardInput.WriteLine("You can exit now.");

                Assert.True(childHandle.Process.WaitForExit(WaitInMS));
                // Use shorter wait time when the process is expected to survive
                Assert.Equal(enabled, grandchild.WaitForExit(enabled ? WaitInMS : 300));
            }
            finally
            {
                grandchild.Kill();
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)] // (false, true) is tested by ProcessHandleTests
        public void KillOnParentExit_KillsTheChild_WhenParentIsKilled(bool enabled, bool restrictInheritance)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;
            remoteInvokeOptions.StartInfo.RedirectStandardInput = true;

            using RemoteInvokeHandle childHandle = RemoteExecutor.Invoke(
                (enabledStr, limitInheritanceStr) =>
                {
                    using Process grandChild = CreateProcessLong();
                    grandChild.StartInfo.KillOnParentExit = bool.Parse(enabledStr);
                    grandChild.StartInfo.InheritedHandles = bool.Parse(limitInheritanceStr) ? [] : null;

                    grandChild.Start();
                    Console.WriteLine(grandChild.Id);

                    // This will block the child until parent kills it.
                    _ = Console.ReadLine();
                },
                enabled.ToString(),
                restrictInheritance.ToString(),
                remoteInvokeOptions);

            int grandChildPid = int.Parse(childHandle.Process.StandardOutput.ReadLine());

            // Obtain a Process instance before the child is killed to avoid PID reuse issues.
            using Process grandchild = Process.GetProcessById(grandChildPid);

            try
            {
                childHandle.Process.Kill();

                Assert.True(childHandle.Process.WaitForExit(WaitInMS));
                Assert.NotEqual(0, childHandle.Process.ExitCode);
                // Use shorter wait time when the process is expected to survive
                Assert.Equal(enabled, grandchild.WaitForExit(enabled ? WaitInMS : 300));
            }
            finally
            {
                grandchild.Kill();
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, false)] // (false, true) is tested by ProcessHandleTests
        [OuterLoop("May create a memory dump")]
        public void KillOnParentExit_KillsTheChild_WhenParentCrashes(bool enabled, bool restrictInheritance)
        {
            RemoteInvokeOptions remoteInvokeOptions = new() { CheckExitCode = false };
            remoteInvokeOptions.StartInfo.RedirectStandardOutput = true;
            remoteInvokeOptions.StartInfo.RedirectStandardInput = true;

            using RemoteInvokeHandle childHandle = RemoteExecutor.Invoke(
                (enabledStr, limitInheritanceStr) =>
                {
                    using Process grandChild = CreateProcessLong();
                    grandChild.StartInfo.KillOnParentExit = bool.Parse(enabledStr);
                    grandChild.StartInfo.InheritedHandles = bool.Parse(limitInheritanceStr) ? [] : null;

                    grandChild.Start();
                    Console.WriteLine(grandChild.Id);

                    // This will block the child until parent writes input.
                    _ = Console.ReadLine();

                    // Guaranteed Access Violation - write to null pointer
                    Marshal.WriteInt32(IntPtr.Zero, 42);
                },
                enabled.ToString(),
                restrictInheritance.ToString(),
                remoteInvokeOptions);

            int grandChildPid = int.Parse(childHandle.Process.StandardOutput.ReadLine());

            // Obtain a Process instance before the child crashes to avoid PID reuse issues.
            using Process grandchild = Process.GetProcessById(grandChildPid);

            try
            {
                childHandle.Process.StandardInput.WriteLine("One AccessViolationException please.");

                Assert.True(childHandle.Process.WaitForExit(WaitInMS));
                Assert.NotEqual(0, childHandle.Process.ExitCode);
                // Use shorter wait time when the process is expected to survive
                Assert.Equal(enabled, grandchild.WaitForExit(enabled ? WaitInMS : 300));
            }
            finally
            {
                grandchild.Kill();
            }
        }
    }
}
