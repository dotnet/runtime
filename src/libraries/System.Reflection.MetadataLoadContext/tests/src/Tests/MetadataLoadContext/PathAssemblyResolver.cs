// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class MetadataLoadContextTests
    {
        [Fact]
        public static void PathAssemblyResolverNullPaths()
        {
            Assert.Throws<ArgumentNullException>(() => new PathAssemblyResolver(null));
        }

        [Fact]
        public static void PathAssemblyResolverNullPath()
        {
            Assert.Throws<ArgumentException>(() => new PathAssemblyResolver(new string[] { null }));
        }

        [Fact]
        public static void PathAssemblyResolverEmptyPath()
        {
            Assert.Throws<ArgumentException>(() => new PathAssemblyResolver(new string[] { "" }));
        }

        [Fact]
        public static void PathAssemblyResolverEmptyFile()
        {
            Assert.Throws<ArgumentException>(() => new PathAssemblyResolver(new string[] { Path.DirectorySeparatorChar.ToString() }));
        }

        [Fact]
        public static void PathAssemblyFullyQualified()
        {
            // No exception should be thrown for duplicate entries during the constructor
            new PathAssemblyResolver(new string[] { "Hello", "Hello" });
        }

        [Fact]
        public static void PathAssemblyResolverWithNoPath()
        {
            var resolver = new PathAssemblyResolver(new string[] { });
            Assert.Throws<FileNotFoundException>(() => new MetadataLoadContext(resolver, "mscorlib"));
        }

        [Fact]
        public static void PathAssemblyResolverWithNoPathAndNoCoreAssemblyName()
        {
            var resolver = new PathAssemblyResolver(new string[] { });
            Assert.Throws<FileNotFoundException>(() => new MetadataLoadContext(resolver));
        }

        [Fact]
        public static void PathAssemblyResolverBasicPathWithRunningAssemblies()
        {
            string coreAssemblyPath = TestUtils.GetPathToCoreAssembly();

            // Obtain this test class
            string thisAssemblyPath = typeof(MetadataLoadContextTests).Assembly.Location;

            var resolver = new PathAssemblyResolver(new string[] { coreAssemblyPath, thisAssemblyPath });
            using (MetadataLoadContext lc = new MetadataLoadContext(resolver, TestUtils.GetNameOfCoreAssembly()))
            {
                AssemblyName thisAssemblyName = typeof(MetadataLoadContextTests).Assembly.GetName();
                Assembly assembly = lc.LoadFromAssemblyName(thisAssemblyName);

                Type t = assembly.GetType(typeof(MetadataLoadContextTests).FullName, throwOnError: true);
                Assert.Equal(t.FullName, typeof(MetadataLoadContextTests).FullName);
                Assert.Equal(t.Assembly.Location, thisAssemblyPath);
            }
        }

        [Fact]
        public static void ResolveToAssemblyNameCombinations()
        {
            using (TempDirectory dir = new TempDirectory())
            using (TempFile core = new TempFile(Path.Combine(dir.Path, TestData.s_PhonyCoreAssemblySimpleName), TestData.s_PhonyCoreAssemblyImage))
            using (TempFile tf1 = new TempFile(Path.Combine(dir.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleSignedVersioned100Image))
            {
                var resolver = new PathAssemblyResolver(new string[] { core.Path, tf1.Path });

                using (MetadataLoadContext lc = new MetadataLoadContext(resolver, TestData.s_PhonyCoreAssemblySimpleName))
                {
                    Assert.Equal(1, lc.GetAssemblies().Count());

                    Assembly assembly = lc.LoadFromAssemblyName(TestData.s_SimpleVersionedShortName);
                    Assert.NotNull(assembly);

                    // Name

                    {
                        Assembly assemblyAgain = lc.LoadFromAssemblyName(TestData.s_SimpleVersionedShortName.ToUpper());
                        Assert.NotNull(assemblyAgain);
                    }

                    AssemblyName assemblyName = new AssemblyName(TestData.s_SimpleVersionedShortName);

                    // Version

                    {
                        assemblyName.Version = new Version(999, 998);
                        lc.LoadFromAssemblyName(assemblyName);
                        Assert.Equal(2, lc.GetAssemblies().Count());
                    }

                    {
                        assemblyName.Version = null;
                        Assembly assemblyAgain = lc.LoadFromAssemblyName(assemblyName);
                        Assert.Same(assembly, assemblyAgain);
                    }

                    {
                        assemblyName.Version = new Version(0, 0, 0, 0);
                        Assembly assemblyAgain = lc.LoadFromAssemblyName(assemblyName);
                        Assert.Same(assembly, assemblyAgain);
                    }

                    {
                        assemblyName.Version = assembly.GetName().Version;
                        Assembly assemblyAgain = lc.LoadFromAssemblyName(assemblyName);
                        Assert.Same(assembly, assemblyAgain);
                    }

                    // CultureName; match not required

                    {
                        assemblyName.CultureName = "en";
                        Assembly assemblyAgain = lc.LoadFromAssemblyName(assemblyName);
                        Assert.Same(assembly, assemblyAgain);
                    }

                    {
                        assemblyName.CultureName = "";
                        Assembly assemblyAgain = lc.LoadFromAssemblyName(assemblyName);
                        Assert.Same(assembly, assemblyAgain);
                    }

                    {
                        assemblyName.CultureName = null;
                        Assembly assemblyAgain = lc.LoadFromAssemblyName(assemblyName);
                        Assert.Same(assembly, assemblyAgain);
                    }

                    assemblyName.CultureName = assembly.GetName().CultureName;

                    // PublicKeyToken

                    assemblyName.SetPublicKeyToken(new byte[] { 1 });
                    Assert.Throws<FileNotFoundException>(() => lc.LoadFromAssemblyName(assemblyName));

                    {
                        assemblyName.SetPublicKeyToken(null);
                        Assembly assemblyAgain = lc.LoadFromAssemblyName(assemblyName);
                        Assert.Same(assembly, assemblyAgain);
                    }

                    {
                        assemblyName.SetPublicKeyToken(Array.Empty<byte>());
                        Assembly assemblyAgain = lc.LoadFromAssemblyName(assemblyName);
                        Assert.Same(assembly, assemblyAgain);
                    }

                    {
                        assemblyName.SetPublicKeyToken(assembly.GetName().GetPublicKeyToken());
                        Assembly assemblyAgain = lc.LoadFromAssemblyName(assemblyName);
                        Assert.Same(assembly, assemblyAgain);
                    }

                    // None of the above should have affected the number of loaded assemblies.
                    Assert.Equal(2, lc.GetAssemblies().Count());
                }
            }
        }

        [Fact]
        public static void DuplicateSignedAssembliesSameMvid()
        {
            using (TempDirectory dir = new TempDirectory())
            using (TempDirectory dir2 = new TempDirectory())
            using (TempFile core = new TempFile(Path.Combine(dir.Path, TestData.s_PhonyCoreAssemblySimpleName), TestData.s_PhonyCoreAssemblyImage))
            using (TempFile tf1 = new TempFile(Path.Combine(dir.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleSignedVersioned100Image))
            using (TempFile tf2 = new TempFile(Path.Combine(dir2.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleSignedVersioned100Image))
            {
                var resolver = new PathAssemblyResolver(new string[] { core.Path, tf1.Path, tf2.Path });

                using (var lc = new MetadataLoadContext(resolver, TestData.s_PhonyCoreAssemblyFullName))
                {
                    Assert.Equal(1, lc.GetAssemblies().Count());

                    Assembly a1 = lc.LoadFromAssemblyName(TestData.s_SimpleVersionedShortName);
                    Assembly a2 = lc.LoadFromAssemblyName(TestData.s_SimpleSignedVersioned100FullName);
                    Assert.Same(a1, a2);
                    Assert.Equal(2, lc.GetAssemblies().Count());
                }
            }
        }

        [Fact]
        public static void DuplicateSignedAssembliesDifferentVersions()
        {
            using (TempDirectory dir = new TempDirectory())
            using (TempDirectory dir2 = new TempDirectory())
            using (TempFile core = new TempFile(Path.Combine(dir.Path, TestData.s_PhonyCoreAssemblySimpleName), TestData.s_PhonyCoreAssemblyImage))
            using (TempFile tf1 = new TempFile(Path.Combine(dir.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleSignedVersioned100Image))
            using (TempFile tf2 = new TempFile(Path.Combine(dir2.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleSignedVersioned200Image))
            {
                var resolver = new PathAssemblyResolver(new string[] { core.Path, tf1.Path, tf2.Path });

                using (var lc = new MetadataLoadContext(resolver, TestData.s_PhonyCoreAssemblyFullName))
                {
                    Assert.Equal(1, lc.GetAssemblies().Count());

                    // Using simple name will find first assembly that matches.
                    Assembly a1 = lc.LoadFromAssemblyName(TestData.s_SimpleVersionedShortName);
                    Assembly a2 = lc.LoadFromAssemblyName(TestData.s_SimpleSignedVersioned100FullName);
                    Assembly a3 = lc.LoadFromAssemblyName(TestData.s_SimpleSignedVersioned200FullName);
                    Assert.True(object.ReferenceEquals(a1, a2) || object.ReferenceEquals(a1, a3));

                    Assert.Equal(3, lc.GetAssemblies().Count());
                }
            }
        }

        [Fact]
        public static void DuplicateUnsignedAssembliesSameVersions()
        {
            using (TempDirectory dir = new TempDirectory())
            using (TempDirectory dir2 = new TempDirectory())
            using (TempFile core = new TempFile(Path.Combine(dir.Path, TestData.s_PhonyCoreAssemblySimpleName), TestData.s_PhonyCoreAssemblyImage))
            using (TempFile tf1 = new TempFile(Path.Combine(dir.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleUnsignedVersioned100Image))
            using (TempFile tf2 = new TempFile(Path.Combine(dir2.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleUnsignedVersioned100Image))
            {
                var resolver = new PathAssemblyResolver(new string[] { core.Path, tf1.Path, tf2.Path });
                using (var lc = new MetadataLoadContext(resolver, TestData.s_PhonyCoreAssemblyFullName))
                {
                    Assembly a1 = lc.LoadFromAssemblyName(TestData.s_SimpleVersionedShortName);
                    Assembly a2 = lc.LoadFromAssemblyName(TestData.s_SimpleUnsignedVersioned100FullName);
                    Assert.Same(a1, a2);
                }
            }
        }

       [Fact]
        public static void DuplicateUnsignedAssembliesSameVersionsDifferentLocale()
        {
            using (TempDirectory dir = new TempDirectory())
            using (TempDirectory dir2 = new TempDirectory())
            using (TempFile core = new TempFile(Path.Combine(dir.Path, TestData.s_PhonyCoreAssemblySimpleName), TestData.s_PhonyCoreAssemblyImage))
            using (TempFile tf1 = new TempFile(Path.Combine(dir.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleUnsignedVersioned100Image))
            using (TempFile tf2 = new TempFile(Path.Combine(dir2.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleUnsignedVersioned100EnImage))
            {
                var resolver = new PathAssemblyResolver(new string[] { core.Path, tf1.Path, tf2.Path });
                using (var lc = new MetadataLoadContext(resolver, TestData.s_PhonyCoreAssemblyFullName))
                {
                    Assert.Equal(1, lc.GetAssemblies().Count());

                    Assembly a1 = lc.LoadFromAssemblyName(TestData.s_SimpleVersionedShortName);
                    Assert.Equal(3, lc.GetAssemblies().Count());
                    Assert.Equal("", a1.GetName().CultureName);

                    Assembly a2 = lc.LoadFromAssemblyName(TestData.s_SimpleUnsignedVersioned100EnFullName);
                    Assert.Same(a1, a2);
                }
            }
        }

        [Fact]
        public static void DuplicateAssembliesPickHigherVersion()
        {
            using (TempDirectory dir = new TempDirectory())
            using (TempDirectory dir2 = new TempDirectory())
            using (TempFile core = new TempFile(Path.Combine(dir.Path, TestData.s_PhonyCoreAssemblySimpleName), TestData.s_PhonyCoreAssemblyImage))
            using (TempFile tf1 = new TempFile(Path.Combine(dir.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleUnsignedVersioned100Image))
            using (TempFile tf2 = new TempFile(Path.Combine(dir2.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleUnsignedVersioned200Image))
            {
                // tf1 first, then tf2.
                {
                    var resolver = new PathAssemblyResolver(new string[] { core.Path, tf1.Path, tf2.Path });
                    using (var lc = new MetadataLoadContext(resolver, TestData.s_PhonyCoreAssemblyFullName))
                    {
                        Assembly a1 = lc.LoadFromAssemblyName(TestData.s_SimpleUnsignedVersioned100FullName);
                        Assert.Equal("2.0.0.0", a1.GetName().Version.ToString());
                    }
                }

                // Reverse the order.
                {
                    var resolver = new PathAssemblyResolver(new string[] { core.Path, tf2.Path, tf1.Path });
                    using (var lc = new MetadataLoadContext(resolver, TestData.s_PhonyCoreAssemblyFullName))
                    {
                        Assembly a1 = lc.LoadFromAssemblyName(TestData.s_SimpleUnsignedVersioned100FullName);
                        Assert.Equal("2.0.0.0", a1.GetName().Version.ToString());
                    }
                }
            }
        }

        [Fact]
        public static void DuplicateSignedAndUnsignedAssemblies()
        {
            using (TempDirectory dir = new TempDirectory())
            using (TempDirectory dir2 = new TempDirectory())
            using (TempFile core = new TempFile(Path.Combine(dir.Path, TestData.s_PhonyCoreAssemblySimpleName), TestData.s_PhonyCoreAssemblyImage))
            using (TempFile tf1 = new TempFile(Path.Combine(dir.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleSignedVersioned100Image))
            using (TempFile tf2 = new TempFile(Path.Combine(dir2.Path, TestData.s_SimpleVersionedShortName), TestData.s_SimpleUnsignedVersioned100Image))
            {
                var resolver = new PathAssemblyResolver(new string[] { core.Path, tf1.Path, tf2.Path });

                using (var lc = new MetadataLoadContext(resolver, TestData.s_PhonyCoreAssemblyFullName))
                {
                    Assert.Equal(1, lc.GetAssemblies().Count());

                    // These are treated as different since one contains a PublicKeyToken and one does not.
                    Assembly a1 = lc.LoadFromAssemblyName(TestData.s_SimpleUnsignedVersioned100FullName);
                    Assembly a2 = lc.LoadFromAssemblyName(TestData.s_SimpleSignedVersioned100FullName);
                    Assert.NotSame(a1, a2);

                    Assert.Equal(3, lc.GetAssemblies().Count());
                }
            }
        }
    }
}
