// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

namespace AppHost.Bundle.Tests
{
    public class StaticHost
    {
        [Fact]
        private void NotMarkedAsBundle_RunSelfContainedApp()
        {
            using (TestApp app = TestApp.CreateFromBuiltAssets("HelloWorld"))
            {
                app.PopulateSelfContained(TestApp.MockedComponent.None);
                app.CreateSingleFileHost();

                Command.Create(app.AppExe)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World");
            }
        }

        [Fact]
        private void ExpectedExports()
        {
            string[] expectedExports =
            [
                "DotNetRuntimeInfo",
                "g_dacTable",
                "MetaDataGetDispenser",
                "DotNetRuntimeContractDescriptor",
            ];

            string singleFileHostPath = Binaries.SingleFileHost.FilePath;

            if (OperatingSystem.IsWindows())
            {
                IntPtr handle = NativeLibrary.Load(singleFileHostPath);
                try
                {
                    foreach (string exportName in expectedExports)
                    {
                        Assert.True(
                            NativeLibrary.TryGetExport(handle, exportName, out _),
                            $"Expected singlefilehost to export {exportName}");
                    }
                }
                finally
                {
                    NativeLibrary.Free(handle);
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                // Use nm to check for exported defined symbols.
                // On macOS, -gUj shows external defined symbol names only.
                CommandResult result = Command.Create("nm", "-gUj", singleFileHostPath)
                    .CaptureStdOut()
                    .CaptureStdErr()
                    .Execute();
                result.Should().Pass();

                foreach (string exportName in expectedExports)
                {
                    Assert.Contains(exportName, result.StdOut);
                }
            }
            else
            {
                // Use nm to check for exported defined symbols.
                // On Linux, -D --defined-only shows defined dynamic symbols.
                // Fall back to llvm-nm (e.g., on musl/Alpine environments that have llvm but not binutils).
                string? nmTool = FindToolInPath("nm") ?? FindToolInPath("llvm-nm");
                Assert.SkipUnless(nmTool is not null, "nm or llvm-nm is not available");

                CommandResult result = Command.Create(nmTool!, "-D", "--defined-only", singleFileHostPath)
                    .CaptureStdOut()
                    .CaptureStdErr()
                    .Execute();
                result.Should().Pass();

                foreach (string exportName in expectedExports)
                {
                    Assert.Contains(exportName, result.StdOut);
                }
            }
        }

        private static string? FindToolInPath(string tool)
        {
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv is not null)
            {
                foreach (string dir in pathEnv.Split(Path.PathSeparator))
                {
                    string fullPath = Path.Combine(dir, tool);
                    if (File.Exists(fullPath))
                        return tool;
                }
            }
            return null;
        }
    }
}
