// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.DependencyResolution
{
    public abstract class DependencyResolutionBase
    {
        public abstract class SharedTestStateBase : IDisposable
        {
            public string Location { get; }

            private readonly TestArtifact _baseDirectory;
            private readonly List<TestApp> _apps = new List<TestApp>();

            public SharedTestStateBase()
            {
                _baseDirectory = TestArtifact.Create("dependencyResolution");
                Location = _baseDirectory.Location;
            }

            public DotNetBuilder DotNet(string name)
            {
                return new DotNetBuilder(_baseDirectory.Location, TestContext.BuiltDotNet.BinPath, name);
            }

            public TestApp CreateFrameworkReferenceApp(string fxName, string fxVersion, Action<NetCoreAppBuilder> customizer = null)
            {
                // Prepare the app mock - we're not going to run anything really, so we just need the basic files
                TestApp testApp = CreateTestApp(_baseDirectory.Location, "FrameworkReferenceApp");
                testApp.PopulateFrameworkDependent(fxName, fxVersion, customizer);
                return testApp;
            }

            protected TestApp CreateTestApp(string location, string name)
            {
                TestApp testApp;
                if (location == null)
                {
                    testApp = TestApp.CreateEmpty(name);
                }
                else
                {
                    string path = Path.Combine(location, name);
                    testApp = new TestApp(path);
                }

                _apps.Add(testApp);
                return testApp;
            }

            public TestApp CreateComponentWithNoDependencies(Action<NetCoreAppBuilder> customizer = null, string location = null)
            {
                TestApp componentWithNoDependencies = CreateTestApp(location, "ComponentWithNoDependencies");
                NetCoreAppBuilder builder = NetCoreAppBuilder.PortableForNETCoreApp(componentWithNoDependencies)
                    .WithProject(p => p.WithAssemblyGroup(null, g => g.WithMainAssembly()));
                customizer?.Invoke(builder);

                return builder.Build(componentWithNoDependencies);
            }

            public TestApp CreateSelfContainedAppWithMockCoreClr(string name, Action<NetCoreAppBuilder> customizer = null)
            {
                TestApp testApp = CreateTestApp(null, name);
                testApp.PopulateSelfContained(TestApp.MockedComponent.CoreClr, customizer);
                return testApp;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposing)
                    return;

                foreach (TestApp app in _apps)
                {
                    app.Dispose();
                }

                _apps.Clear();
                _baseDirectory.Dispose();
            }
        }
    }
}
