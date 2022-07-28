// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;
using static AppHost.Bundle.Tests.BundleTestBase;

namespace AppHost.Bundle.Tests
{
    public class SingleFileSharedState : SharedTestStateBase, IDisposable
    {
        public TestProjectFixture TestFixture { get; set; }

        public SingleFileSharedState()
        {
            try
            {
                // We include mockcoreclr in our project to test native binaries extraction.
                string mockCoreClrPath = Path.Combine(RepoDirectories.Artifacts, "corehost_test",
                    RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("mockcoreclr"));
                TestFixture = PreparePublishedSelfContainedTestProject("SingleFileApiTests", $"/p:AddFile={mockCoreClrPath}");
            }
            catch (Exception e) when (TestUtils.FailFast(e)) // Fail fast to gather a crash dump
            {
                throw;
            }
        }

        public void Dispose()
        {
            TestFixture.Dispose();
        }
    }
}