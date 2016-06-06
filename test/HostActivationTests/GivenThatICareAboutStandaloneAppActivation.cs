// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Xunit;
using FluentAssertions;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.DotNet.Cli.Build.Framework;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.StandaloneApp
{
    public class GivenThatICareAboutStandaloneAppActivation
    {
        private static TestProjectFixture PreviouslyBuiltAndRestoredStandaloneTestProjectFixture { get; set; }
        private static TestProjectFixture PreviouslyPublishedAndRestoredStandaloneTestProjectFixture { get; set; }
        private static RepoDirectoriesProvider RepoDirectories { get; set; }

        static GivenThatICareAboutStandaloneAppActivation()
        {
            RepoDirectories = new RepoDirectoriesProvider();

            var buildFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
            buildFixture
                .EnsureRestored(RepoDirectories.CorehostPackages, RepoDirectories.CorehostDummyPackages)
                .BuildProject(runtime: buildFixture.CurrentRid)
                .ReplaceTestProjectOutputHostFromDotnet(buildFixture.BuiltDotnet);

            var publishFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
            publishFixture
                .EnsureRestored(RepoDirectories.CorehostPackages, RepoDirectories.CorehostDummyPackages)
                .PublishProject(runtime: publishFixture.CurrentRid)
                .ReplaceTestProjectOutputHostFromDotnet(publishFixture.BuiltDotnet);

            PreviouslyBuiltAndRestoredStandaloneTestProjectFixture = buildFixture;
            PreviouslyPublishedAndRestoredStandaloneTestProjectFixture = publishFixture;
        }

        [Fact]
        public void Running_Build_Output_Standalone_EXE_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = PreviouslyBuiltAndRestoredStandaloneTestProjectFixture
                .Copy();
            
            var appExe = fixture.TestProject.AppExe;

            Command.Create(appExe).Execute().Should().Pass();
        }

        [Fact]
        public void Running_Publish_Output_Standalone_EXE_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = PreviouslyPublishedAndRestoredStandaloneTestProjectFixture
                .Copy();

            var appExe = fixture.TestProject.AppExe;

            Command.Create(appExe).Execute().Should().Pass();
        }
    }
}
