// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using System.IO;
using System.Reflection;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class DependencyContextLoaderTests
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "GetEntryAssembly() returns null")]
        public void LoadLoadsExtraPaths()
        {
            string appDepsPath = "appPath.deps.json";
            string fxDepsPath = "fxPath.deps.json";
            string extraDepsPath = "extra1.deps.json";

            var fileSystem = FileSystemMockBuilder.Create()
                .AddFile(
                    appDepsPath,
@"{
    ""runtimeTarget"": {
        ""name"":"".NETCoreApp,Version=v1.0/osx.10.10-x64"",
        ""signature"":""target-signature""
    },
    ""targets"": {
        "".NETCoreApp,Version=v1.0/osx.10.10-x64"": {}
    }
}")
                .AddFile(
                    fxDepsPath,
@"{
    ""targets"": {
        "".NETCoreApp,Version=v1.0/osx.10.10-x64"": {
            
        }
    }
}")
                .AddFile(
                    extraDepsPath,
@"
 {
     ""targets"": {
         "".NETStandard,Version=v1.5"": {
             ""System.Banana/1.0.0"": {
                 ""runtimeTargets"": {
                     ""runtimes/unix/Banana.dll"": { ""rid"": ""unix"", ""assetType"": ""runtime"" },
                     ""runtimes/win7/Banana.dll"": { ""rid"": ""win7"",  ""assetType"": ""runtime"" },

                     ""runtimes/native/win7/Apple.dll"": { ""rid"": ""win7"",  ""assetType"": ""native"" },
                     ""runtimes/native/unix/libapple.so"": { ""rid"": ""unix"",  ""assetType"": ""native"" }
                 }
             }
         }
     },
     ""libraries"": {
         ""System.Banana/1.0.0"": {
             ""type"": ""package"",
             ""serviceable"": false,
             ""sha512"": ""HASH-System.Banana""
         }
     }
 }")
                .Build();

            var loader = new DependencyContextLoader(
                appDepsPath,
                new[] { fxDepsPath, extraDepsPath },
                fileSystem,
                () => new DependencyContextJsonReader());

            var context = loader.Load(Assembly.GetEntryAssembly());
            context.RuntimeLibraries.Should().Contain(l => l.Name == "System.Banana");
        }

        [Fact]
        public void LoadCanLoadANonEntryAssembly()
        {
            var loader = new DependencyContextLoader();
            var context = loader.Load(typeof(DependencyContextLoaderTests).Assembly);

            context.RuntimeLibraries.Should().Contain(l => l.Name == "nonentrypointassembly");
        }

        [Fact]
        public void LoadReturnsNullWhenNotFound()
        {
            var loader = new DependencyContextLoader();
            Assert.Null(loader.Load(typeof(Moq.Mock).Assembly));
        }

        [Fact]
        public void LoadReturnsNullWhenAssemblyLocationIsEmpty()
        {
            var loader = new DependencyContextLoader();
            Assert.Null(loader.Load(new EmptyLocationAssembly()));
        }

        private class EmptyLocationAssembly : Assembly
        {
            public override string Location => string.Empty;
            public override AssemblyName GetName() => new AssemblyName("EmptyLocation");
            public override Stream? GetManifestResourceStream(string name) => null;
        }
    }
}
