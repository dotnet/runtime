// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;
using Xunit;

namespace System.Tests
{
    public class UnloadingAndProcessExitTests : RemoteExecutorTestBase
    {
        [ActiveIssue("https://github.com/dotnet/corefx/issues/23307", TargetFrameworkMonikers.Uap)]
        [Fact]
        public void UnloadingEventMustHappenBeforeProcessExitEvent()
        {
            string fileName = GetTestFilePath();

            File.WriteAllText(fileName, string.Empty);

            Func<string, int> otherProcess = f =>
            {
                Action<int> OnUnloading = i => File.AppendAllText(f, string.Format("u{0}", i));
                Action<int> OnProcessExit = i => File.AppendAllText(f, string.Format("e{0}", i));

                File.AppendAllText(f, "s");
                AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnProcessExit(0);
                System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += acl => OnUnloading(0);
                AppDomain.CurrentDomain.ProcessExit += (sender, e) => OnProcessExit(1);
                System.Runtime.Loader.AssemblyLoadContext.Default.Unloading += acl => OnUnloading(1);
                File.AppendAllText(f, "h");

                return SuccessExitCode;
            };

            using (var remote = RemoteInvoke(otherProcess, fileName))
            {
            }

            Assert.Equal("shu0u1e0e1", File.ReadAllText(fileName));
        }
    }
}
