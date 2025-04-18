// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;

namespace System.ComponentModel.Tests
{
    [SimpleUpdateTest]
    [Collection(nameof(DisableParallelization))] // Clears the cache which disrupts concurrent tests
    public class ReflectionCachesUpdateHandlerTests
    {
        private class TestAssemblyLoadContext : AssemblyLoadContext
        {
            private AssemblyDependencyResolver _resolver;

            public TestAssemblyLoadContext(string name, bool isCollectible, string mainAssemblyToLoadPath = null) : base(name, isCollectible)
            {
                if (!PlatformDetection.IsBrowser)
                    _resolver = new AssemblyDependencyResolver(mainAssemblyToLoadPath ?? Assembly.GetExecutingAssembly().Location);
            }

            protected override Assembly Load(AssemblyName name)
            {
                if (PlatformDetection.IsBrowser)
                {
                    return base.Load(name);
                }

                string assemblyPath = _resolver.ResolveAssemblyToPath(name);
                if (assemblyPath != null)
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }

                return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void ExecuteAndUnload(string assemblyfile, string typename, Action<Type> typeAction, out WeakReference alcWeakRef)
        {
            var fullPath = Path.GetFullPath(assemblyfile);
            var alc = new TestAssemblyLoadContext("XmlSerializerTests", true, fullPath);
            alcWeakRef = new WeakReference(alc);

            // Load assembly by path. By name, and it gets loaded in the default ALC.
            var asm = alc.LoadFromAssemblyPath(fullPath);

            // Ensure the type loaded in the intended non-Default ALC
            var type = asm.GetType(typename);
            Assert.Equal(AssemblyLoadContext.GetLoadContext(type.Assembly), alc);
            Assert.NotEqual(alc, AssemblyLoadContext.Default);

            // Perform action on the type from ALC
            typeAction(type);

            // Unload the ALC
            alc.Unload();
        }

        private static void ClearReflectionCaches()
        {
            MethodInfo clearCache = Type.GetType("System.ComponentModel.ReflectionCachesUpdateHandler, System.ComponentModel.TypeConverter", throwOnError: true).GetMethod("ClearCache");
            Assert.NotNull(clearCache);
            clearCache.Invoke(null, new object[] { null });
        }

        [Fact]
        public void ReflectionCachesUpdateHandler_CachesCleared()
        {
            AttributeCollection ac1 = TypeDescriptor.GetAttributes(typeof(ReflectionCachesUpdateHandlerTests));
            AttributeCollection ac2 = TypeDescriptor.GetAttributes(typeof(ReflectionCachesUpdateHandlerTests));
            Assert.Equal(ac1.Count, ac2.Count);
            Assert.Equal(2, ac1.Count);
            Assert.Same(ac1[0], ac2[0]);

            ClearReflectionCaches();

            AttributeCollection ac3 = TypeDescriptor.GetAttributes(typeof(ReflectionCachesUpdateHandlerTests));
            Assert.NotSame(ac1[0], ac3[0]);
        }

        [Fact]
        // Lack of AssemblyDependencyResolver results in assemblies that are not loaded by path to get
        // loaded in the default ALC, which causes problems for this test.
        [SkipOnPlatform(TestPlatforms.Browser, "AssemblyDependencyResolver not supported in wasm")]
        [ActiveIssue("34072", TestRuntimes.Mono)]
        public static void ClearCache_DoesNotLeakTypes()
        {
            ExecuteAndUnload("UnloadableTestTypes.dll", "UnloadableTestTypes.SimpleType",
                static (type) =>
                {
                    // Cache the type
                    _ = TypeDescriptor.GetAttributes(type);
                },
                out var weakRef);

            ClearReflectionCaches();

            for (int i = 0; weakRef.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Assert.True(!weakRef.IsAlive);
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class SimpleUpdateTestAttribute : Attribute { }
}
