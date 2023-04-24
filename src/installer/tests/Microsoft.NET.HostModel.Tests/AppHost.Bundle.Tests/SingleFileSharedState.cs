// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.DotNet;
using Microsoft.DotNet.CoreSetup.Test;
using Xunit;

using BundleTests.Helpers;
using static AppHost.Bundle.Tests.BundleTestBase;

namespace AppHost.Bundle.Tests
{
    public class SingleFileSharedState : SharedTestStateBase, IDisposable
    {
        public TestProjectFixture TestFixture { get; set; }

        public TestProjectFixture PublishedSingleFile { get; set; }

        public SingleFileSharedState()
        {
            try
            {
                // We include mockcoreclr in our project to test native binaries extraction.
                TestFixture = PreparePublishedSelfContainedTestProject("SingleFileApiTests", $"/p:AddFile={Binaries.CoreClr.MockPath}");

                // This uses the repo's SDK to publish single file using the live-built singlefilehost,
                // such that the publish directory is the real scenario, rather than manually constructed
                // via the bundler API and copying files around. This does mean that the bundler used is
                // the version from the SDK.
                PublishedSingleFile = new TestProjectFixture("SingleFileApiTests", RepoDirectories);
                PublishedSingleFile
                    .EnsureRestoredForRid(PublishedSingleFile.CurrentRid)
                    .PublishProject(runtime: PublishedSingleFile.CurrentRid,
                                    outputDirectory: BundleHelper.GetPublishPath(PublishedSingleFile),
                                    selfContained: true,
                                    singleFile: true,
                                    extraArgs: new[] { $"/p:AddFile={Binaries.CoreClr.MockPath}", $"/p:SingleFileHostSourcePath={Binaries.SingleFileHost.FilePath}" });
            }
            catch (Exception e) when (TestUtils.FailFast(e)) // Fail fast to gather a crash dump
            {
                throw;
            }
        }

        public void Dispose()
        {
            TestFixture.Dispose();
            PublishedSingleFile.Dispose();
        }
    }
}
