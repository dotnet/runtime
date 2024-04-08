// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class AssemblyTests
    {
        [Fact]
        public static void AssemblyGetName()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                AssemblyName name = a.GetName(copiedName: false);
                Assert.NotNull(name);
                string fullName = name.FullName;
                Assert.Equal(TestData.s_SimpleAssemblyFullName, fullName);
            }
        }

        [Fact]
        public static void AssemblyGetCopiedName()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                AssemblyName name = a.GetName(copiedName: true);   // Shadow-copying is irrevant for MetadataLoadContext-loaded assemblies so this parameter is ignored.
                Assert.NotNull(name);
                string fullName = name.FullName;
                Assert.Equal(TestData.s_SimpleAssemblyFullName, fullName);
            }
        }

        [Fact]
        public static void AssemblyGetNameAlwaysReturnsNewInstance()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                AssemblyName name1 = a.GetName();
                AssemblyName name2 = a.GetName();
                Assert.NotSame(name1, name2);
            }
        }

        [Fact]
        public static void AssemblyFullName()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                string fullName = a.FullName;
                Assert.Equal(TestData.s_SimpleAssemblyFullName, fullName);
            }
        }

        [Fact]
        public static void AssemblyGlobalAssemblyCache()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Assert.False(a.GlobalAssemblyCache);  // This property is meaningless for MetadataLoadContexts and always returns false.
            }
        }

        [Fact]
        public static void AssemblyHostContext()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Assert.Equal(0L, a.HostContext);  // This property is meaningless for MetadataLoadContexts and always returns 0.
            }
        }

        [Fact]
        public static void AssemblyIsDynamic()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Assert.False(a.IsDynamic);
            }
        }

        [Fact]
        public static void AssemblyReflectionOnly()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Assert.True(a.ReflectionOnly);
            }
        }

        [Fact]
        public static void AssemblyMetadataVersion4_0()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                string metadataVersion = a.ImageRuntimeVersion;
                Assert.Equal("v4.0.30319", metadataVersion);
            }
        }

        [Fact]
        public static void AssemblyMetadataVersion2_0()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_NetFx20AssemblyImage);
                string metadataVersion = a.ImageRuntimeVersion;
                Assert.Equal("v2.0.50727", metadataVersion);
            }
        }

        [Fact]
        public static void AssemblyLocationMemory()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                string location = a.Location;
                Assert.Equal(string.Empty, location);
            }
        }

        [Fact]
        public static void AssemblyLocationFile()
        {
            using (TempFile tf = TempFile.Create(TestData.s_SimpleAssemblyImage))
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromAssemblyPath(tf.Path);
                string location = a.Location;
                Assert.Equal(tf.Path, location);
            }
        }

        [Fact]
        public static void AssemblyGetTypesReturnsDifferentObjectsEachTime()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromStream(TestUtils.CreateStreamForCoreAssembly());
                TestUtils.AssertNewObjectReturnedEachTime<Type>(() => a.GetTypes());
            }
        }

        [Fact]
        public static void AssemblyDefinedTypeInfosReturnsDifferentObjectsEachTime()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromStream(TestUtils.CreateStreamForCoreAssembly());
                TestUtils.AssertNewObjectReturnedEachTime<TypeInfo>(() => a.DefinedTypes);
            }
        }

        [Fact]
        public static void AssemblyGetReferencedAssemblies()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_AssemblyReferencesTestImage);
                AssemblyName[] ans = a.GetReferencedAssemblies();
                Assert.Equal(3, ans.Length);
                {
                    AssemblyName an = ans.Single(an2 => an2.Name == "mscorlib");
                    Assert.Equal(default(AssemblyNameFlags), an.Flags);
                    Version v = an.Version;
                    Assert.NotNull(v);
                    Assert.Equal(4, v.Major);
                    Assert.Equal(0, v.Minor);
                    Assert.Equal(0, v.Build);
                    Assert.Equal(0, v.Revision);
                    Assert.Equal(string.Empty, an.CultureName);
                    Assert.Null(an.GetPublicKey());
                    byte[] pkt = an.GetPublicKeyToken();
                    byte[] expectedPkt = "b77a5c561934e089".HexToByteArray();
                    Assert.Equal<byte>(expectedPkt, pkt);
                }

                {
                    AssemblyName an = ans.Single(an2 => an2.Name == "dep1");
                    Assert.Equal(default(AssemblyNameFlags), an.Flags);
                    Version v = an.Version;
                    Assert.NotNull(v);
                    Assert.Equal(0, v.Major);
                    Assert.Equal(0, v.Minor);
                    Assert.Equal(0, v.Build);
                    Assert.Equal(0, v.Revision);
                    Assert.Equal(string.Empty, an.CultureName);
                    Assert.Null(an.GetPublicKey());
                    Assert.Equal(0, an.GetPublicKeyToken().Length);
                }

                {
                    AssemblyName an = ans.Single(an2 => an2.Name == "dep2");
                    Assert.Equal(default(AssemblyNameFlags), an.Flags);
                    Version v = an.Version;
                    Assert.NotNull(v);
                    Assert.Equal(1, v.Major);
                    Assert.Equal(2, v.Minor);
                    Assert.Equal(3, v.Build);
                    Assert.Equal(4, v.Revision);
                    Assert.Equal("pl-PL", an.CultureName);
                    Assert.Null(an.GetPublicKey());
                    Assert.Equal(0, an.GetPublicKeyToken().Length);
                }
            }
        }

        [Fact]
        public static void AssemblyGetReferencedAssembliesFullPublicKeyReference()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                // Ecma-335 allows an assembly reference to specify a full public key rather than the token.

                Assembly a = lc.LoadFromByteArray(TestData.s_AssemblyRefUsingFullPublicKeyImage);
                AssemblyName[] ans = a.GetReferencedAssemblies();
                Assert.Equal(1, ans.Length);
                AssemblyName an = ans[0];
                Assert.Equal("mscorlib", an.Name);
                Assert.Equal(AssemblyNameFlags.PublicKey, an.Flags);

                byte[] expectedPk = "00000000000000000400000000000000".HexToByteArray();
                byte[] actualPk = an.GetPublicKey();
                Assert.Equal<byte>(expectedPk, actualPk);
            }
        }

        [Fact]
        public static void AssemblyGetReferencedAssembliesPartialVersions()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_PartialVersionsImage);
                AssemblyName[] ans = a.GetReferencedAssemblies();
                Assert.Equal(6, ans.Length);

                ans = ans.OrderBy(aname => aname.Name).ToArray();

                AssemblyName an;

                an = ans[0];
                Assert.Equal("illegal1", an.Name);
                Assert.Null(an.Version);

                an = ans[1];
                Assert.Equal("illegal2", an.Name);
                Assert.Null(an.Version);

                an = ans[2];
                Assert.Equal("illegal3", an.Name);
                Assert.Null(an.Version);

                an = ans[3];
                Assert.Equal("nobuildrevision", an.Name);
                Assert.Equal(1, an.Version.Major);
                Assert.Equal(2, an.Version.Minor);
                Assert.Equal(-1, an.Version.Build);
                Assert.Equal(-1, an.Version.Revision);

                an = ans[4];
                Assert.Equal("nobuildrevisionalternate", an.Name);
                Assert.Equal(1, an.Version.Major);
                Assert.Equal(2, an.Version.Minor);
                Assert.Equal(-1, an.Version.Build);
                Assert.Equal(-1, an.Version.Revision);

                an = ans[5];
                Assert.Equal("norevision", an.Name);
                Assert.Equal(1, an.Version.Major);
                Assert.Equal(2, an.Version.Minor);
                Assert.Equal(3, an.Version.Build);
                Assert.Equal(-1, an.Version.Revision);
            }
        }

        [Fact]
        public static void AssemblyGetReferencedAssembliesReturnsDifferentObjectsEachTime()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_AssemblyReferencesTestImage);
                AssemblyName[] ans1 = a.GetReferencedAssemblies();
                AssemblyName[] ans2 = a.GetReferencedAssemblies();

                Assert.Equal(ans1.Length, ans2.Length);
                if (ans1.Length != 0)
                {
                    Assert.NotSame(ans1, ans2);

                    // Be absolutely sure we're comparing apples to apples.
                    ans1 = ans1.OrderBy(an => an.Name).ToArray();
                    ans2 = ans2.OrderBy(an => an.Name).ToArray();

                    for (int i = 0; i < ans1.Length; i++)
                    {
                        Assert.NotSame(ans1[i], ans2[i]);

                        {
                            byte[] pk1 = ans1[i].GetPublicKey();
                            byte[] pk2 = ans2[i].GetPublicKey();
                            if (pk1 != null)
                            {
                                Assert.Equal<byte>(pk1, pk2);
                                if (pk1.Length != 0)
                                {
                                    Assert.NotSame(pk1, pk2);
                                }
                            }
                        }

                        {
                            byte[] pkt1 = ans1[i].GetPublicKeyToken();
                            byte[] pkt2 = ans2[i].GetPublicKeyToken();
                            if (pkt1 != null)
                            {
                                Assert.Equal<byte>(pkt1, pkt2);

                                if (pkt1.Length != 0)
                                {
                                    Assert.NotSame(pkt1, pkt2);
                                }
                            }
                        }
                    }
                }
            }
        }

        [Fact]
        public static void AssemblyGetTypes1()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly upper = lc.LoadFromByteArray(TestData.s_UpperImage);
                Type[] types = upper.GetTypes().OrderBy(t => t.FullName).ToArray();
                string[] fullNames = types.Select(t => t.FullName).ToArray();

                string[] expected = [
                    "Outer1", "Outer1+Inner1", "Outer1+Inner2", "Outer1+Inner3", "Outer1+Inner4", "Outer1+Inner5",
                    "Outer2", "Outer2+Inner1", "Outer2+Inner2", "Outer2+Inner3", "Outer2+Inner4", "Outer2+Inner5",
                    "Upper1", "Upper4"
                ];

                Assert.Equal(expected, fullNames);
            }
        }

        [Fact]
        public static void AssemblyGetExportedTypes1()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly upper = lc.LoadFromByteArray(TestData.s_UpperImage);
                Type[] types = upper.GetExportedTypes().OrderBy(t => t.FullName).ToArray();
                string[] fullNames = types.Select(t => t.FullName).ToArray();

                string[] expected = [ "Outer1", "Outer1+Inner1", "Upper1" ];
                Assert.Equal(expected, fullNames);
            }
        }

        [Fact]
        public static void AssemblyGetForwardedTypes1()
        {
            // Note this is using SimpleAssemblyResolver in order to resolve names between assemblies.
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly upper = lc.LoadFromByteArray(TestData.s_UpperImage);
                Assembly middle = lc.LoadFromByteArray(TestData.s_MiddleImage);
                Assembly lower = lc.LoadFromByteArray(TestData.s_LowerImage);

                Type[] types = upper.GetForwardedTypesThunk().OrderBy(t => t.FullName).ToArray();
                string[] fullNames = types.Select(t => t.FullName).ToArray();

                string[] expected = [ "Middle2", "Upper2", "Upper3", "Upper3+Upper3a" ];
                Assert.Equal(expected, fullNames);
            }
        }

        [Fact]
        public static void AssemblyGetForwardedTypes2()
        {
            // Note this is using SimpleAssemblyResolver in order to resolve names between assemblies.
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly upper = lc.LoadFromByteArray(TestData.s_UpperImage);
                Assembly middle = lc.LoadFromByteArray(TestData.s_MiddleImage);

                ReflectionTypeLoadException re = Assert.Throws<ReflectionTypeLoadException>(() => upper.GetForwardedTypesThunk());
                Assert.Equal(3, re.Types.Length);
                Assert.Equal(3, re.LoaderExceptions.Length);

                Assert.Equal(2, re.Types.Count((t) => t == null));
                Assert.Contains<Type>(middle.GetType("Upper2", throwOnError: true), re.Types);

                Assert.Equal(1, re.LoaderExceptions.Count((t) => t == null));
                Assert.True(re.LoaderExceptions.All((t) => t == null || t is FileNotFoundException));
            }
        }

        [Fact]
        public static void AssemblyGetForwardedTypes3()
        {
            // Note this is using SimpleAssemblyResolver in order to resolve names between assemblies.
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly upper = lc.LoadFromByteArray(TestData.s_UpperImage);
                Assembly lower = lc.LoadFromByteArray(TestData.s_LowerImage);

                ReflectionTypeLoadException re = Assert.Throws<ReflectionTypeLoadException>(() => upper.GetForwardedTypesThunk());
                Assert.Equal(4, re.Types.Length);
                Assert.Equal(4, re.LoaderExceptions.Length);

                Assert.Equal(2, re.Types.Count((t) => t == null));
                Assert.Contains<Type>(lower.GetType("Upper3", throwOnError: true), re.Types);
                Assert.Contains<Type>(lower.GetType("Upper3+Upper3a", throwOnError: true), re.Types);

                Assert.Equal(2, re.LoaderExceptions.Count((t) => t == null));
                Assert.True(re.LoaderExceptions.All((t) => t == null || t is FileNotFoundException));
            }
        }

        [Fact]
        public static void AssemblyWithEmbeddedResources()
        {
            // Note this is using SimpleAssemblyResolver in order to resolve names between assemblies.
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_AssemblyWithEmbeddedResourcesImage);

                string[] names = a.GetManifestResourceNames().OrderBy(s => s).ToArray();
                Assert.Equal<string>([ "MyRes1", "MyRes2", "MyRes3" ], names);
                foreach (string name in names)
                {
                    ManifestResourceInfo mri = a.GetManifestResourceInfo(name);
                    Assert.Equal(ResourceLocation.Embedded | ResourceLocation.ContainedInManifestFile, mri.ResourceLocation);
                    Assert.Null(mri.FileName);
                    Assert.Null(mri.ReferencedAssembly);
                }

                using (Stream s = a.GetManifestResourceStream("MyRes1"))
                {
                    byte[] res = s.ToArray();
                    Assert.Equal<byte>(TestData.s_MyRes1, res);
                }

                using (Stream s = a.GetManifestResourceStream("MyRes2"))
                {
                    byte[] res = s.ToArray();
                    Assert.Equal<byte>(TestData.s_MyRes2, res);
                }

                using (Stream s = a.GetManifestResourceStream("MyRes3"))
                {
                    byte[] res = s.ToArray();
                    Assert.Equal<byte>(TestData.s_MyRes3, res);
                }
            }
        }


        [Fact]
        public static void AssemblyWithResourcesInManifestFile()
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

                    string[] names = a.GetManifestResourceNames().OrderBy(s => s).ToArray();
                    Assert.Equal<string>([ "MyRes1", "MyRes2", "MyRes3" ], names);
                    foreach (string name in names)
                    {
                        ManifestResourceInfo mri = a.GetManifestResourceInfo(name);
                        Assert.Equal(default(ResourceLocation), mri.ResourceLocation);
                        Assert.Equal(name, mri.FileName);
                        Assert.Null(mri.ReferencedAssembly);
                    }

                    using (Stream s = a.GetManifestResourceStream("MyRes1"))
                    {
                        byte[] res = s.ToArray();
                        Assert.Equal<byte>(TestData.s_MyRes1, res);
                    }

                    using (Stream s = a.GetManifestResourceStream("MyRes2"))
                    {
                        byte[] res = s.ToArray();
                        Assert.Equal<byte>(TestData.s_MyRes2, res);
                    }

                    using (Stream s = a.GetManifestResourceStream("MyRes3"))
                    {
                        byte[] res = s.ToArray();
                        Assert.Equal<byte>(TestData.s_MyRes3, res);
                    }
                }
            }
        }

        [Fact]
        public static void AssemblyWithResourcesInModule()
        {
            using (TempDirectory td = new TempDirectory())
            {
                string assemblyPath = Path.Combine(td.Path, "n.dll");
                string modulePath = Path.Combine(td.Path, "a.netmodule");
                File.WriteAllBytes(assemblyPath, TestData.s_AssemblyWithResourcesInModuleImage);
                File.WriteAllBytes(modulePath, TestData.s_ModuleForAssemblyWithResourcesInModuleImage);

                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(assemblyPath);

                    string[] names = a.GetManifestResourceNames().OrderBy(s => s).ToArray();
                    Assert.Equal<string>([ "MyRes1", "MyRes2", "MyRes3" ], names);
                    foreach (string name in names)
                    {
                        ManifestResourceInfo mri = a.GetManifestResourceInfo(name);
                        Assert.Equal(ResourceLocation.Embedded | ResourceLocation.ContainedInManifestFile, mri.ResourceLocation);
                        Assert.Null(mri.FileName);
                        Assert.Null(mri.ReferencedAssembly);
                    }

                    using (Stream s = a.GetManifestResourceStream("MyRes1"))
                    {
                        byte[] res = s.ToArray();
                        Assert.Equal<byte>(TestData.s_MyRes1, res);
                    }

                    using (Stream s = a.GetManifestResourceStream("MyRes2"))
                    {
                        byte[] res = s.ToArray();
                        Assert.Equal<byte>(TestData.s_MyRes2, res);
                    }

                    using (Stream s = a.GetManifestResourceStream("MyRes3"))
                    {
                        byte[] res = s.ToArray();
                        Assert.Equal<byte>(TestData.s_MyRes3, res);
                    }
                }
            }
        }

        [Fact]
        public static void AssemblyEntryPoint1()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_AssemblyWithEntryPointImage);
                MethodInfo m = a.EntryPoint;
                Assert.Equal(TestData.s_AssemblyWithEntryPointEntryPointToken, m.MetadataToken);
                Assert.Equal("Main", m.Name);
            }
        }

        [Fact]
        public static void AssemblyEntryPoint2()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                MethodInfo m = a.EntryPoint;
                Assert.Null(m);
            }
        }

        [Fact]
        public static void CrossAssemblyTypeRefToNestedType()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_AssemblyWithNestedTypeImage);
                Assembly n = lc.LoadFromByteArray(TestData.s_AssemblyWithTypeRefToNestedTypeImage);
                Type nt = n.GetType("N", throwOnError: true);
                Type bt = nt.BaseType;
                Type expected = a.GetType("Outer+Inner+ReallyInner", throwOnError: true);
                Assert.Equal(expected, bt);
            }
        }

        [Fact]
        public static void ResourceDoesNotExist_GetManifestResourceInfo_ReturnsNull()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_AssemblyWithEmbeddedResourcesImage);
                ManifestResourceInfo? r = a.GetManifestResourceInfo("ResourceThatDoesNotExist");
                Assert.Null(r);
            }
        }

        [Fact]
        public static void ResourceDoesNotExist_GetManifestResourceStream_ReturnsNull()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_AssemblyWithEmbeddedResourcesImage);
                Stream? r = a.GetManifestResourceStream("ResourceThatDoesNotExist");
                Assert.Null(r);
            }
        }
    }
}
