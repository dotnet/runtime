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

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.PortableApp
{
    public class GivenThatICareAboutPortableAppActivation
    {
        private static TestProjectFixture PortableTestProjectFixture { get; set; }
		private static string ExeExtension { get; set; }

		static GivenThatICareAboutPortableAppActivation()
        {
            ExeExtension = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? ".exe"
                : "";

            PortableTestProjectFixture = new TestProjectFixture("PortableApp", ExeExtension)
                .EnsureRestored()
                .BuildProject();
        }

        [Fact]
		public void Muxer_activation_of_Portable_DLL_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = PortableTestProjectFixture
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll).Execute().Should().Pass();
            dotnet.Exec("exec", appDll).Execute().Should().Pass();
        }

		
        [Fact]
        public void Muxer_Exec_activation_of_Portable_DLL_with_DepsJson_Local_and_RuntimeConfig_Remote_Succeeds()
        {
            var fixture = PortableTestProjectFixture
                .Copy()
                .MoveRuntimeConfigToSubdirectory();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var runtimeConfig = fixture.TestProject.RuntimeConfigJson;
			
            dotnet.Exec("exec", "--runtimeconfig", runtimeConfig, appDll).Execute().Should().Pass();
        }

        [Fact]
        public void Muxer_Exec_activation_of_Portable_DLL_with_DepsJson_Remote_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = PortableTestProjectFixture
                 .Copy()
                 .MoveDepsJsonToSubdirectory();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var depsJson = fixture.TestProject.DepsJson;

            dotnet.Exec("exec", "--depsfile", depsJson, appDll).Execute().Should().Pass();
        }
    }
}
