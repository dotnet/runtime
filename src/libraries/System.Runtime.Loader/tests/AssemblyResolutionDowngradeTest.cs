// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Runtime.Loader.Tests
{
    public class AssemblyResolutionDowngradeTest : FileCleanupTestBase
    {
        private const string TestAssemblyName = "System.Runtime.Loader.Test.VersionDowngrade";
        
        /// <summary>
        /// Test that AppDomain.AssemblyResolve can resolve a higher version request with a lower version assembly.
        /// This tests the scenario where code requests assembly version 3.0.0 but the resolver provides 1.0.0.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AppDomainAssemblyResolve_CanDowngradeVersion()
        {
            RemoteExecutor.Invoke(() => {
                string assemblyV1Path = GetTestAssemblyPath("System.Runtime.Loader.Test.AssemblyVersion1");
                
                bool resolverCalled = false;
                
                ResolveEventHandler handler = (sender, args) =>
                {
                    Assert.Same(AppDomain.CurrentDomain, sender);
                    Assert.NotNull(args);
                    Assert.NotNull(args.Name);
                    
                    var requestedName = new AssemblyName(args.Name);
                    if (requestedName.Name == TestAssemblyName)
                    {
                        resolverCalled = true;
                        // Request is for version 3.0, but we return version 1.0 (downgrade)
                        Assert.Equal(new Version(3, 0, 0, 0), requestedName.Version);
                        return Assembly.LoadFile(assemblyV1Path);
                    }
                    return null;
                };

                AppDomain.CurrentDomain.AssemblyResolve += handler;

                try
                {
                    // Request version 3.0.0 but expect to get 1.0.0 via downgrade
                    var requestedAssemblyName = new AssemblyName($"{TestAssemblyName}, Version=3.0.0.0");
                    Assembly resolvedAssembly = Assembly.Load(requestedAssemblyName);
                    
                    Assert.NotNull(resolvedAssembly);
                    Assert.True(resolverCalled, "Assembly resolver should have been called");
                    
                    // Verify we got the 1.0.0 assembly (downgrade successful)
                    Assert.Equal(new Version(1, 0, 0, 0), resolvedAssembly.GetName().Version);
                    
                    // Verify the assembly works as expected
                    Type testType = resolvedAssembly.GetType("System.Runtime.Loader.Tests.VersionTestClass");
                    Assert.NotNull(testType);
                    
                    string version = (string)testType.GetMethod("GetVersion").Invoke(null, null);
                    Assert.Equal("1.0.0", version);
                }
                finally
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= handler;
                }
            }).Dispose();
        }

        /// <summary>
        /// Test that AssemblyLoadContext.Resolving event can resolve a higher version request with a lower version assembly.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void AssemblyLoadContextResolving_CanDowngradeVersion()
        {
            RemoteExecutor.Invoke(() => {
                string assemblyV1Path = GetTestAssemblyPath("System.Runtime.Loader.Test.AssemblyVersion1");
                
                bool resolverCalled = false;
                
                Func<AssemblyLoadContext, AssemblyName, Assembly> handler = (context, name) =>
                {
                    if (name.Name == TestAssemblyName)
                    {
                        resolverCalled = true;
                        // Request is for version 3.0, but we return version 1.0 (downgrade)
                        Assert.Equal(new Version(3, 0, 0, 0), name.Version);
                        return context.LoadFromAssemblyPath(assemblyV1Path);
                    }
                    return null;
                };

                AssemblyLoadContext.Default.Resolving += handler;

                try
                {
                    // Request version 3.0.0 but expect to get 1.0.0 via downgrade
                    var requestedAssemblyName = new AssemblyName($"{TestAssemblyName}, Version=3.0.0.0");
                    Assembly resolvedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(requestedAssemblyName);
                    
                    Assert.NotNull(resolvedAssembly);
                    Assert.True(resolverCalled, "Assembly resolver should have been called");
                    
                    // Verify we got the 1.0.0 assembly (downgrade successful)
                    Assert.Equal(new Version(1, 0, 0, 0), resolvedAssembly.GetName().Version);
                    
                    // Verify the assembly works as expected
                    Type testType = resolvedAssembly.GetType("System.Runtime.Loader.Tests.VersionTestClass");
                    Assert.NotNull(testType);
                    
                    string version = (string)testType.GetMethod("GetVersion").Invoke(null, null);
                    Assert.Equal("1.0.0", version);
                }
                finally
                {
                    AssemblyLoadContext.Default.Resolving -= handler;
                }
            }).Dispose();
        }

        /// <summary>
        /// Test that a custom AssemblyLoadContext.Load override can resolve a higher version request with a lower version assembly.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CustomAssemblyLoadContextLoad_CanDowngradeVersion()
        {
            RemoteExecutor.Invoke(() => {
                string assemblyV1Path = GetTestAssemblyPath("System.Runtime.Loader.Test.AssemblyVersion1");
                
                var customContext = new DowngradeAssemblyLoadContext(assemblyV1Path);
                
                // Request version 3.0.0 but expect to get 1.0.0 via downgrade
                var requestedAssemblyName = new AssemblyName($"{TestAssemblyName}, Version=3.0.0.0");
                Assembly resolvedAssembly = customContext.LoadFromAssemblyName(requestedAssemblyName);
                
                Assert.NotNull(resolvedAssembly);
                Assert.True(customContext.LoadCalled, "Custom Load method should have been called");
                
                // Verify we got the 1.0.0 assembly (downgrade successful)
                Assert.Equal(new Version(1, 0, 0, 0), resolvedAssembly.GetName().Version);
                
                // Verify the assembly works as expected
                Type testType = resolvedAssembly.GetType("System.Runtime.Loader.Tests.VersionTestClass");
                Assert.NotNull(testType);
                
                string version = (string)testType.GetMethod("GetVersion").Invoke(null, null);
                Assert.Equal("1.0.0", version);
                
                // Verify that the correct ALC loaded the assembly
                Assert.Equal(customContext, AssemblyLoadContext.GetLoadContext(resolvedAssembly));
            }).Dispose();
        }

        /// <summary>
        /// Test that normal runtime resolution (without extension mechanisms) will NOT allow downgrades.
        /// This test verifies the baseline behavior that downgrades only work via extension mechanisms.
        /// Note: On Mono, downgrades are allowed even in normal resolution, so this test behaves differently.
        /// </summary>
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void NormalResolution_CannotDowngradeVersion()
        {
            RemoteExecutor.Invoke(() => {
                string assemblyV1Path = GetTestAssemblyPath("System.Runtime.Loader.Test.AssemblyVersion1");
                
                // First, load the version 1.0.0 assembly into the default context
                Assembly loadedV1 = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyV1Path);
                Assert.Equal(new Version(1, 0, 0, 0), loadedV1.GetName().Version);
                
                // Now try to load version 3.0.0
                var requestedAssemblyName = new AssemblyName($"{TestAssemblyName}, Version=3.0.0.0");
                
                if (PlatformDetection.IsMonoRuntime)
                {
                    // On Mono, normal resolution allows downgrades, so this should succeed
                    // and return the already-loaded 1.0.0 assembly
                    Assembly resolvedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyName(requestedAssemblyName);
                    Assert.NotNull(resolvedAssembly);
                    Assert.Equal(new Version(1, 0, 0, 0), resolvedAssembly.GetName().Version);
                    Assert.Same(loadedV1, resolvedAssembly);
                }
                else
                {
                    // On CoreCLR, normal resolution should NOT automatically 
                    // downgrade to the already-loaded 1.0.0 version, it should fail
                    Assert.Throws<FileNotFoundException>(() => 
                        AssemblyLoadContext.Default.LoadFromAssemblyName(requestedAssemblyName));
                }
            }).Dispose();
        }

        private static string GetTestAssemblyPath(string assemblyProject)
        {
            // Map project names to actual embedded resource names
            string resourceName = assemblyProject switch
            {
                "System.Runtime.Loader.Test.AssemblyVersion1" => "System.Runtime.Loader.Tests.AssemblyVersion1.dll",
                _ => throw new ArgumentException($"Unknown test assembly project: {assemblyProject}")
            };
            
            // Extract the embedded assembly to a temporary file
            string tempPath = Path.Combine(Path.GetTempPath(), $"{assemblyProject}_{Guid.NewGuid()}.dll");
            
            using (Stream resourceStream = typeof(AssemblyResolutionDowngradeTest).Assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream is null)
                {
                    throw new FileNotFoundException($"Could not find embedded resource: {resourceName}");
                }
                
                using (FileStream fileStream = File.Create(tempPath))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }
            
            return tempPath;
        }

        /// <summary>
        /// Custom AssemblyLoadContext that can downgrade version requests.
        /// </summary>
        private class DowngradeAssemblyLoadContext : AssemblyLoadContext
        {
            private readonly string _downgradePath;
            
            public bool LoadCalled { get; private set; }
            
            public DowngradeAssemblyLoadContext(string downgradePath) : base("DowngradeContext")
            {
                _downgradePath = downgradePath;
            }
            
            protected override Assembly Load(AssemblyName assemblyName)
            {
                LoadCalled = true;
                
                if (assemblyName.Name == TestAssemblyName)
                {
                    // Request is for version 3.0, but we return version 1.0 (downgrade)
                    Assert.Equal(new Version(3, 0, 0, 0), assemblyName.Version);
                    return LoadFromAssemblyPath(_downgradePath);
                }
                
                return null;
            }
        }
    }
}
