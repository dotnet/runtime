// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class ModuleTests
    {
        [Fact]
        public static void LoadMultiModuleFromDisk_GetModule()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Module m = a.GetModule("Bob.netmodule");
                    Assert.Equal(a, m.Assembly);
                    Assert.Equal(bobNetModulePath, m.FullyQualifiedName);
                    Assert.Equal(Path.GetFileName(bobNetModulePath), m.Name);
                    Assert.Equal(TestData.s_JoeScopeName, m.ScopeName);
                    Assert.Equal(TestData.s_JoeNetModuleMvid, m.ModuleVersionId);
                }
            }
        }

        [Fact]
        public static void LoadMultiModuleFromDisk_GetModuleCaseInsensitive()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Module m = a.GetModule("bOB.nEtmODule");
                    Assert.Equal(a, m.Assembly);
                    Assert.Equal(bobNetModulePath, m.FullyQualifiedName);
                    Assert.Equal(Path.GetFileName(bobNetModulePath), m.Name);
                    Assert.Equal(TestData.s_JoeScopeName, m.ScopeName);
                    Assert.Equal(TestData.s_JoeNetModuleMvid, m.ModuleVersionId);
                }
            }
        }

        [Fact]
        public static void LoadMultiModuleFromDisk_GetModuleNameNotInManifest()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Module m = a.GetModule("NotThere.netmodule");
                    Assert.Null(m);
                }
            }
        }

        [Fact]
        public static void LoadMultiModuleFromDisk_GetModuleNameNotThere()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Assert.Throws<FileNotFoundException>(() => a.GetModule("Bob.netmodule"));
                }
            }
        }

        [Fact]
        public static void LoadMultiModuleFromDisk_Twice()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Module m1 = a.GetModule("Bob.netmodule");
                    Module m2 = a.GetModule("bob.netmodule");
                    Assert.Equal(m1, m2);
                }
            }
        }

        [Fact]
        public static void LoadMultiModuleFromDisk_GetModuleManifest()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Module m = a.GetModule("Main.dll");
                    Assert.Equal(a.ManifestModule, m);
                }
            }
        }

        [Fact]
        public static void LoadMultiModuleFromDisk_GetModuleNull()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Assert.Throws<ArgumentNullException>(() => a.GetModule(null));
                }
            }
        }

        [Fact]
        public static void LoadMultiModuleFromByteArray_GetModule()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_MultiModuleDllImage);
                Assert.Throws<FileNotFoundException>(() => a.GetModule("Bob.netmodule"));
            }
        }

        [Theory]
        [InlineData(new object[] { false })]
        [InlineData(new object[] { true })]
        public static void MultiModule_GetModules(bool getResourceModules)
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Module[] ms = a.GetModules(getResourceModules: getResourceModules);
                    Assert.Equal(2, ms.Length);
                    Module bob = a.GetModule("Bob.netmodule");
                    Assert.NotNull(bob);
                    Assert.Contains<Module>(a.ManifestModule, ms);
                    Assert.Contains<Module>(bob, ms);
                }
            }
        }

        [Fact]
        public static void LoadModule_Null()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_MultiModuleDllImage);
                Assert.Throws<ArgumentNullException>(() => a.LoadModule(null, TestData.s_JoeNetModuleImage));
                Assert.Throws<ArgumentNullException>(() => a.LoadModule("Bob.netmodule", null));
            }
        }

        [Fact]
        public static void LoadModule_CannotLoadModuleManifestModule()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_MultiModuleDllImage);
                Assert.Throws<ArgumentException>(() => a.LoadModule("Main.dll", TestData.s_JoeNetModuleImage));
            }
        }

        [Fact]
        public static void LoadModule_CannotLoadModuleNotInManifest()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_MultiModuleDllImage);
                Assert.Throws<ArgumentException>(() => a.LoadModule("NotInManifest.dll", TestData.s_JoeNetModuleImage));
            }
        }

        [Theory]
        [InlineData("Bob.netmodule")]
        [InlineData("bOB.NETMODULE")]
        public static void LoadModule(string moduleName)
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_MultiModuleDllImage);
                Module m = a.LoadModule(moduleName, TestData.s_JoeNetModuleImage);
                Module m1 = a.GetModule(moduleName);
                Assert.NotNull(m);
                Assert.Equal(m, m1);

                Assert.Equal(a, m.Assembly);
                Assert.Equal("<unknown>", m.FullyQualifiedName);
                Assert.Equal("<unknown>", m.Name);
                Assert.Equal("Joe.netmodule", m.ScopeName);
                Assert.Equal(TestData.s_JoeNetModuleMvid, m.ModuleVersionId);
            }
        }

        [Fact]
        public static void LoadModuleTwiceQuirk()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_MultiModuleDllImage);
                Module m1 = a.LoadModule("Bob.netmodule", TestData.s_JoeNetModuleImage);
                Module m2 = a.LoadModule("Bob.netmodule", TestData.s_JoeNetModuleImage);
                Module winner = a.GetModule("Bob.netmodule");

                Assert.NotNull(winner);
                Assert.Equal(winner, m1);

                // Compat quirk: Why does the second Assembly.LoadModule() call not return the module that actually won the race 
                // like the LoadAssemblyFrom() apis do?
                Assert.NotEqual(m1, m2);
            }
        }

        [Fact]
        public static void ModuleResolveEvent()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Module moduleReturnedFromEventHandler = null;

                Assembly a = lc.LoadFromByteArray(TestData.s_MultiModuleDllImage);
                a.ModuleResolve +=
                    delegate (object context, ResolveEventArgs e)
                    {
                        Assert.Same(a, context);
                        Assert.Null(moduleReturnedFromEventHandler); // We're not doing anything to cause this to trigger twice!
                        Assert.Equal("Bob.netmodule", e.Name);
                        moduleReturnedFromEventHandler = a.LoadModule("Bob.netmodule", TestData.s_JoeNetModuleImage);
                        return moduleReturnedFromEventHandler;
                    };

                Module m = a.GetModule("Bob.netmodule");
                Assert.NotNull(m);
                Assert.Equal(moduleReturnedFromEventHandler, m);

                // Make sure the event doesn't get raised twice. For a single-threaded case like this, that's a reasonable assumption.
                Module m1 = a.GetModule("Bob.netmodule");
            }
        }

        [Fact]
        public static void MultiModule_AssemblyGetTypes()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Type[] types = a.GetTypes();
                    AssertContentsOfMultiModule(types, a);
                }
            }
        }

        [Fact]
        public static void MultiModule_AssemblyDefinedTypes()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Type[] types = a.DefinedTypes.ToArray();
                    AssertContentsOfMultiModule(types, a);
                }
            }
        }

        [Fact]
        public static void MultiModule_AssemblyGetTypes_ReturnsDifferentObjectEachType()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    TestUtils.AssertNewObjectReturnedEachTime(() => a.GetTypes());
                }
            }
        }

        [Fact]
        public static void MultiModule_AssemblyDefinedTypes_ReturnsDifferentObjectEachType()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    TestUtils.AssertNewObjectReturnedEachTime(() => a.DefinedTypes);
                }
            }
        }

        [Fact]
        public static void ModuleGetTypes()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Type[] types = a.ManifestModule.GetTypes();
                    Assert.Equal(2, types.Length);
                    AssertMainModuleTypesFound(types, a);

                    Module bob = a.GetModule("Bob.netmodule");
                    Assert.NotNull(bob);
                    Type[] bobTypes = bob.GetTypes();
                    Assert.Equal(2, bobTypes.Length);
                    AssertBobModuleTypesFound(bobTypes, a);
                }
            }
        }

        [Fact]
        public static void ModuleGetTypesReturnsNewObjectEachType()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    TestUtils.AssertNewObjectReturnedEachTime(() => a.ManifestModule.GetTypes());
                }
            }
        }

        [Fact]
        public static void CrossModuleTypeRefResolution()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "MultiModule.dll");
                string bobNetModulePath = Path.Combine(td.Path, "Bob.netmodule");

                File.WriteAllBytes(assemblyPath, TestData.s_MultiModuleDllImage);
                File.WriteAllBytes(bobNetModulePath, TestData.s_JoeNetModuleImage); // Note: ScopeName ("Joe") intentionally different from manifest name ("Bob")

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Module bob = a.GetModule("Bob.netmodule");
                    Type mainType1 = a.ManifestModule.GetType("MainType1", throwOnError: true, ignoreCase: false);
                    Type baseType = mainType1.BaseType;
                    Assert.Equal("JoeType1", baseType.FullName);
                    Assert.Equal(bob, baseType.Module);
                }
            }
        }

        private static void AssertContentsOfMultiModule(Type[] types, Assembly a)
        {
            Assert.Equal(4, types.Length);
            AssertMainModuleTypesFound(types, a);
            AssertBobModuleTypesFound(types, a);
        }

        private static void AssertMainModuleTypesFound(Type[] types, Assembly a)
        {
            Assert.Contains(types, (t) => t.Module == a.ManifestModule && t.FullName == "MainType1");
            Assert.Contains(types, (t) => t.Module == a.ManifestModule && t.FullName == "MainType2");
        }

        private static void AssertBobModuleTypesFound(Type[] types, Assembly a)
        {
            Module bob = a.GetModule("Bob.netmodule");
            Assert.NotNull(bob);
            Assert.Contains(types, (t) => t.Module == bob && t.FullName == "JoeType1");
            Assert.Contains(types, (t) => t.Module == bob && t.FullName == "JoeType2");
        }

        [Fact]
        public static void ResourceOnlyModules()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "n.dll");
                string myRes1Path = Path.Combine(td.Path, "MyRes1");
                string myRes2Path = Path.Combine(td.Path, "MyRes2");
                string myRes3Path = Path.Combine(td.Path, "MyRes3");

                File.WriteAllBytes(assemblyPath, TestData.s_AssemblyWithResourcesInManifestFilesImage);
                File.WriteAllBytes(myRes1Path, TestData.s_MyRes1);
                File.WriteAllBytes(myRes2Path, TestData.s_MyRes2);
                File.WriteAllBytes(myRes3Path, TestData.s_MyRes3);

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Module[] modules1 = a.GetModules(getResourceModules: false);
                    Assert.Equal<Module>(new Module[] { a.ManifestModule }, modules1);
                    Module[] modules2 = a.GetModules(getResourceModules: true);
                    Assert.Equal(4, modules2.Length);

                    Module m = a.GetModule("MyRes2");
                    Assert.NotNull(m);
                    Assert.True(m.IsResource());
                    Assert.Throws<InvalidOperationException>(() => m.ModuleVersionId);
                    Assert.Equal(0, m.MetadataToken);
                    Assert.Equal("MyRes2", m.ScopeName);
                    Assert.Equal("MyRes2", m.Name);
                    Assert.Equal(myRes2Path, m.FullyQualifiedName);

                    m.GetPEKind(out PortableExecutableKinds peKind, out ImageFileMachine machine);
                    Assert.Equal(PortableExecutableKinds.NotAPortableExecutableImage, peKind);
                    Assert.Equal(default(ImageFileMachine), machine);

                    Assert.True(!m.GetCustomAttributesData().Any());

                    const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                    Assert.Null(m.GetField("ANY", bf));
                    Assert.Null(m.GetMethod("ANY"));
                    Assert.True(!m.GetFields(bf).Any());
                    Assert.True(!m.GetMethods(bf).Any());
                    Assert.True(!m.GetTypes().Any());
                }
            }
        }

        [Fact]
        public static void GetLoadModules1()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_MultiModuleDllImage);

                {
                    Module[] loadedModules = a.GetLoadedModules(getResourceModules: true);
                    Assert.Equal(1, loadedModules.Length);
                    Assert.Equal(a.ManifestModule, loadedModules[0]);
                }

                {
                    Module[] loadedModules = a.GetLoadedModules(getResourceModules: false);
                    Assert.Equal(1, loadedModules.Length);
                    Assert.Equal(a.ManifestModule, loadedModules[0]);
                }

                Module m1 = a.LoadModule("Bob.netmodule", TestData.s_JoeNetModuleImage);

                {
                    Module[] loadedModules = a.GetLoadedModules(getResourceModules: true);
                    Assert.Equal(2, loadedModules.Length);
                    Assert.Contains<Module>(a.ManifestModule, loadedModules);
                    Assert.Contains<Module>(m1, loadedModules);
                }

                {
                    Module[] loadedModules = a.GetLoadedModules(getResourceModules: false);
                    Assert.Equal(2, loadedModules.Length);
                    Assert.Contains<Module>(a.ManifestModule, loadedModules);
                    Assert.Contains<Module>(m1, loadedModules);
                }
            }
        }

        [Fact]
        public static void GetLoadModules2()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "n.dll");
                string myRes1Path = Path.Combine(td.Path, "MyRes1");
                string myRes2Path = Path.Combine(td.Path, "MyRes2");

                File.WriteAllBytes(assemblyPath, TestData.s_AssemblyWithResourcesInManifestFilesImage);
                File.WriteAllBytes(myRes1Path, TestData.s_MyRes1);
                File.WriteAllBytes(myRes2Path, TestData.s_MyRes2);

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);
                    Module res1 = a.GetModule("MyRes1");
                    Module res2 = a.GetModule("MyRes2");

                    {
                        Module[] modules = a.GetLoadedModules(getResourceModules: true);
                        Assert.Equal(3, modules.Length);
                        Assert.Contains<Module>(a.ManifestModule, modules);
                        Assert.Contains<Module>(res1, modules);
                        Assert.Contains<Module>(res2, modules);
                    }

                    {
                        Module[] modules = a.GetLoadedModules(getResourceModules: false);
                        Assert.Equal(1, modules.Length);
                        Assert.Equal(a.ManifestModule, modules[0]);
                    }
                }
            }
        }

        [Fact]
        public static void GetLoadModulesReturnsUniqueArrays()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_MultiModuleDllImage);
                Module m1 = a.LoadModule("Bob.netmodule", TestData.s_JoeNetModuleImage);
                TestUtils.AssertNewObjectReturnedEachTime(() => a.GetLoadedModules(getResourceModules: true));
                TestUtils.AssertNewObjectReturnedEachTime(() => a.GetLoadedModules(getResourceModules: false));
            }
        }
    }
}
