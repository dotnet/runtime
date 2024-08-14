// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessThreadTests
    {
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestPriorityLevelProperty_Unix()
        {
            CreateDefaultProcess();

            ProcessThread thread = _process.Threads[0];
            ThreadPriorityLevel level = ThreadPriorityLevel.Normal;

            if (OperatingSystem.IsMacOS())
            {
                Assert.Throws<PlatformNotSupportedException>(() => thread.PriorityLevel);
            }
            else
            {
                level = thread.PriorityLevel;
            }

            Assert.Throws<PlatformNotSupportedException>(() => thread.PriorityLevel = level);
        }

        private static int GetCurrentThreadId()
        {
            // The magic values come from https://github.com/torvalds/linux.
            int SYS_gettid = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm => 224,
                Architecture.Arm64 => 178,
                Architecture.X86 => 224,
                Architecture.X64 => 186,
                Architecture.S390x => 236,
                Architecture.Ppc64le => 207,
                Architecture.RiscV64 => 178,
                _ => 178,
            };

            return syscall(SYS_gettid);
        }

        [DllImport("libc")]
        private static extern int syscall(int nr);
    }
}
