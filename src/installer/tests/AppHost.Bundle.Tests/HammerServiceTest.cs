// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.Extensions.DependencyModel;
using Xunit;
using static Microsoft.DotNet.CoreSetup.Test.NetCoreAppBuilder;

namespace AppHost.Bundle.Tests
{
    public class HammerServiceTest : IClassFixture<HammerServiceTest.SharedTestState>
    {
        private SharedTestState sharedTestState;

        public HammerServiceTest(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Windows, "On Windows, the hammer servicing location is %ProgramFiles%\\coreservicing. Since writing to this location requires administrative privilege, we do not run the test on Windows.")]
        private void SingleFile_Apps_Are_Serviced()
        {
            var singleFile = sharedTestState.App.Bundle();

            // Create the servicing directory, and copy the serviced DLL from service fixture to the servicing directory.
            var serviced = sharedTestState.ServicedLibrary;
            var serviceBasePath = Path.Combine(sharedTestState.App.Location, "coreservicing");
            var servicePath = Path.Combine(serviceBasePath, "pkgs", serviced.Name, "1.0.0");
            Directory.CreateDirectory(servicePath);
            File.Copy(serviced.AppDll, Path.Combine(servicePath, Path.GetFileName(serviced.AppDll)));

            // Verify that the test DLL is loaded from the bundle when not being serviced
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("SharedLibrary.SharedType.Value = SharedLibrary");

            // Verify that the test DLL is loaded from the servicing location when being serviced
            // On Unix systems, the servicing location is obtained from the environment variable $CORE_SERVICING.
            Command.Create(singleFile)
                .CaptureStdErr()
                .CaptureStdOut()
                .EnableHostTracing()
                .EnvironmentVariable(Constants.CoreServicing.EnvironmentVariable, serviceBasePath)
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("SharedLibrary.SharedType.Value = ServicedLibrary");
        }

        public class SharedTestState : IDisposable
        {
            public SingleFileTestApp App { get; set; }
            public TestApp ServicedLibrary { get; set; }

            public SharedTestState()
            {
                App = SingleFileTestApp.CreateSelfContained("HammerServicing");
                ServicedLibrary = TestApp.CreateFromBuiltAssets("SharedLibrary", "ServicedLibrary");

                // Add the SharedLibrary as a dependency and annotate it as serviceable
                string depsJson = Path.Combine(App.NonBundledLocation, $"{App.Name}.deps.json");
                using (FileStream fileStream = File.Open(depsJson, FileMode.Open, FileAccess.ReadWrite))
                using (DependencyContextJsonReader reader = new DependencyContextJsonReader())
                {
                    DependencyContext context = reader.Read(fileStream);
                    DependencyContext newContext = new DependencyContext(
                        context.Target,
                        context.CompilationOptions,
                        context.CompileLibraries,
                        context.RuntimeLibraries.Append(new RuntimeLibrary(
                            RuntimeLibraryType.project.ToString(),
                            ServicedLibrary.Name,
                            "1.0.0",
                            string.Empty,
                            new[] { new RuntimeAssetGroup(string.Empty, "SharedLibrary.dll" ) },
                            Array.Empty<RuntimeAssetGroup>(),
                            Enumerable.Empty<ResourceAssembly>(),
                            Enumerable.Empty<Dependency>(),
                            serviceable: true)),
                        context.RuntimeGraph);

                    fileStream.Seek(0, SeekOrigin.Begin);
                    DependencyContextWriter writer = new DependencyContextWriter();
                    writer.Write(newContext, fileStream);
                }
            }

            public void Dispose()
            {
                App?.Dispose();
                ServicedLibrary?.Dispose();
            }
        }
    }
}
