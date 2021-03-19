// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public class UnloadingAndProcessExitTests : FileCleanupTestBase
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
        public void UnloadingEventMustHappenBeforeProcessExitEvent()
        {
            string fileName = GetTestFilePath();

            File.WriteAllText(fileName, string.Empty);

            Action<string> otherProcess = f =>
            {
                Action<int> OnUnloading = i => File.AppendAllText(f, string.Format("u{0}", i));
                Action<int> OnProcessExit = i => File.AppendAllText(f, string.Format("e{0}", i));

                File.AppendAllText(f, "s");
                AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnProcessExit(0);
                System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += acl => OnUnloading(0);
                AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnProcessExit(1);
                System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += acl => OnUnloading(1);
                File.AppendAllText(f, "h");
            };

            using (var remote = RemoteExecutor.Invoke(otherProcess, fileName))
            {
            }

            Assert.Equal("shu0u1e0e1", File.ReadAllText(fileName));
        }
    }
}
