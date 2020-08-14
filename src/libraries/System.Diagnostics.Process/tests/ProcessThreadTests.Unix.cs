// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
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
    }
}
