// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
            else
            {
                // Use nm to check for exported dynamic symbols.
                // On macOS, nm shows all symbols; on Linux, -D shows dynamic symbols.
                string args = OperatingSystem.IsMacOS()
                    ? singleFileHostPath
                    : $"-D {singleFileHostPath}";

                CommandResult result = Command.Create("nm", args)
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
    }
}
