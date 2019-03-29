// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class ExecutorTests : FileCleanupTestBase
    {
        private static readonly string s_cmd = PlatformDetection.IsWindows ? "ipconfig" : "printenv"; // arbitrary commands to validate output

        [SkipOnTargetFramework(~TargetFrameworkMonikers.Netcoreapp, "Not supported on .NET Core")]
        [Fact]
        public void Impersonation_Unsupported_Throws()
        {
            string outputName = null, errorName = null;
            Assert.Throws<PlatformNotSupportedException>(() => Executor.ExecWaitWithCapture((IntPtr)1, null, null, ref outputName, ref errorName));
            Assert.Throws<PlatformNotSupportedException>(() => Executor.ExecWaitWithCapture((IntPtr)1, null, null, null, ref outputName, ref errorName));
        }

        [Fact]
        public void NullTempFileCollection_Required_Throws()
        {
            string outputName = null, errorName = null;
            Assert.Throws<NullReferenceException>(() => Executor.ExecWaitWithCapture("", null, ref outputName, ref errorName));
        }

        // NOTE: RemoteInvoke is used because Executor creates inheritable files, which causes a problem
        // for the tests if other tests run concurrently and launch child processes, as those child
        // processes may then extend the lifetime of the opened files, leading to sharing errors in tests.

        [Fact]
        public void ExecWait_OutputCaptured()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var tfc = new TempFileCollection(TestDirectory))
                {
                    Executor.ExecWait(s_cmd, tfc);
                    Assert.Equal(3, tfc.Count);
                    Assert.NotEmpty(File.ReadAllText(tfc.BasePath + ".out"));
                    Assert.Empty(File.ReadAllText(tfc.BasePath + ".err"));
                }
            }).Dispose();
        }

        [Fact]
        public void ExecWaitWithCapture_NullNames_OutputCaptured()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var tfc = new TempFileCollection())
                {
                    string outputName = null, errorName = null;

                    Assert.Equal(0, Executor.ExecWaitWithCapture(s_cmd, tfc, ref outputName, ref errorName));

                    Assert.Equal(3, tfc.Count);

                    Assert.NotEmpty(outputName);
                    Assert.NotEmpty(errorName);

                    Assert.Contains(outputName, tfc.Cast<string>());
                    Assert.Contains(errorName, tfc.Cast<string>());

                    Assert.NotEmpty(File.ReadAllText(outputName));
                    Assert.Empty(File.ReadAllText(errorName));
                }
            }).Dispose();
        }

        [Fact]
        public void ExecWaitWithCapture_SpecifiedNames_OutputCaptured()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var tfc = new TempFileCollection())
                {
                    string outputName = GetTestFilePath();
                    string errorName = GetTestFilePath();

                    Assert.Equal(0, Executor.ExecWaitWithCapture(s_cmd, tfc, ref outputName, ref errorName));

                    Assert.Equal(0, tfc.Count);

                    Assert.NotEmpty(File.ReadAllText(outputName));
                    Assert.Empty(File.ReadAllText(errorName));
                }
            }).Dispose();
        }

        [Fact]
        public void ExecWaitWithCapture_CurrentDirectorySpecified_OutputIncludesSpecifiedDirectory()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var tfc = new TempFileCollection(TestDirectory))
                {
                    string outputName = GetTestFilePath();
                    string errorName = GetTestFilePath();

                    Assert.Equal(0, Executor.ExecWaitWithCapture(s_cmd, TestDirectory, tfc, ref outputName, ref errorName));

                    Assert.Contains(TestDirectory, File.ReadAllText(outputName));
                }
            }).Dispose();
        }

        [Fact]
        public void ExecWaitWithCapture_OutputIncludesCurrentDirectory()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var tfc = new TempFileCollection(TestDirectory))
                {
                    string outputName = GetTestFilePath();
                    string errorName = GetTestFilePath();

                    Assert.Equal(0, Executor.ExecWaitWithCapture(s_cmd, tfc, ref outputName, ref errorName));

                    Assert.Contains(Environment.CurrentDirectory, File.ReadAllText(outputName));
                }
            }).Dispose();
        }
    }
}
