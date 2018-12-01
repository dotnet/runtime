// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class MetadataLoadContextTests
    {
        [Fact]
        public static void LoadFromByteArraySimpleAssembly()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Assert.NotNull(a);

                string fullName = a.GetName().FullName;
                Assert.Equal(TestData.s_SimpleAssemblyFullName, fullName);

                Guid mvid = a.ManifestModule.ModuleVersionId;
                Assert.Equal(TestData.s_SimpleAssemblyMvid, mvid);

                Assert.Equal(string.Empty, a.Location);
            }
        }

        [Fact]
        public static void LoadFromFileSimpleAssembly()
        {
            using (TempFile tf = TempFile.Create(TestData.s_SimpleAssemblyImage))
            {
                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Assembly a = lc.LoadFromAssemblyPath(tf.Path);
                    Assert.NotNull(a);

                    string fullName = a.GetName().FullName;
                    Assert.Equal(TestData.s_SimpleAssemblyFullName, fullName);

                    Guid mvid = a.ManifestModule.ModuleVersionId;
                    Assert.Equal(TestData.s_SimpleAssemblyMvid, mvid);

                    Assert.Equal(tf.Path, a.Location);
                }
            }
        }

        [Fact]
        public static void LoadFromStreamMemorySimpleAssembly()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Stream peStream = new MemoryStream(TestData.s_SimpleAssemblyImage);
                Assembly a = lc.LoadFromStream(peStream);
                Assert.NotNull(a);

                string fullName = a.GetName().FullName;
                Assert.Equal(TestData.s_SimpleAssemblyFullName, fullName);

                Guid mvid = a.ManifestModule.ModuleVersionId;
                Assert.Equal(TestData.s_SimpleAssemblyMvid, mvid);

                Assert.Equal(string.Empty, a.Location);
            }
        }

        [Fact]
        public static void LoadFromStreamFileSimpleAssembly()
        {
            using (TempFile tf = TempFile.Create(TestData.s_SimpleAssemblyImage))
            {
                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    Stream fs = File.OpenRead(tf.Path);
                    Assembly a = lc.LoadFromStream(fs);
                    Assert.NotNull(a);

                    string fullName = a.GetName().FullName;
                    Assert.Equal(TestData.s_SimpleAssemblyFullName, fullName);

                    Guid mvid = a.ManifestModule.ModuleVersionId;
                    Assert.Equal(TestData.s_SimpleAssemblyMvid, mvid);

                    Assert.Equal(tf.Path, a.Location);
                }
            }
        }

        [Fact]
        public static void LoadFromNonZeroPositionedStreamMemorySimpleAssembly()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Stream peStream = new MemoryStream(TestData.s_SimpleAssemblyImage);
                peStream.Position = 1; 
                
                // The MetadataLoadContext takes ownership of the peStream. It will reset its position back to 0.
                Assembly a = lc.LoadFromStream(peStream);
                Assert.NotNull(a);

                string fullName = a.GetName().FullName;
                Assert.Equal(TestData.s_SimpleAssemblyFullName, fullName);

                Guid mvid = a.ManifestModule.ModuleVersionId;
                Assert.Equal(TestData.s_SimpleAssemblyMvid, mvid);

                Assert.Equal(string.Empty, a.Location);
            }
        }

        [Fact]
        public static void LoadFromAssemblyName()
        {
            // Note this is using SimpleAssemblyResolver in order to resolve LoadFromAssemblyName.
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Stream peStream = new MemoryStream(TestData.s_SimpleAssemblyImage);
                Assembly a = lc.LoadFromStream(peStream);
                Assert.NotNull(a);

                Assembly a1 = lc.LoadFromAssemblyName(TestData.s_SimpleAssemblyFullName);
                Assert.Equal(a, a1);

                Assembly a2 = lc.LoadFromAssemblyName(new AssemblyName(TestData.s_SimpleAssemblyFullName));
                Assert.Equal(a, a2);
            }
        }

        [Fact]
        public static void LoadFromDifferentLocations()
        {
            using (TempFile tf1 = TempFile.Create(TestData.s_SimpleAssemblyImage))
            using (TempFile tf2 = TempFile.Create(TestData.s_SimpleAssemblyImage))
            // Note this is using SimpleAssemblyResolver in order to resolve LoadFromAssemblyName.
            using (MetadataLoadContext lc = new MetadataLoadContext(new SimpleAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Assert.NotNull(a);

                Assembly a1 = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Assert.Equal(a, a1);

                Assembly a2 = lc.LoadFromAssemblyName(new AssemblyName(TestData.s_SimpleAssemblyFullName));
                Assert.Equal(a, a2);

                Assembly a3 = lc.LoadFromAssemblyPath(tf1.Path);
                Assert.Equal(a, a3);

                Assembly a4 = lc.LoadFromAssemblyPath(tf2.Path);
                Assert.Equal(a, a4);
            }
        }

        [Fact]
        public static void LoadFromDifferentLocationsMvidMismatch()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Assert.Equal(TestData.s_SimpleAssemblyFullName, a.GetName().FullName);
                Guid mvid = a.ManifestModule.ModuleVersionId;
                Assert.Equal(TestData.s_SimpleAssemblyMvid, mvid);
            }

            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyRecompiledImage);
                Assert.Equal(TestData.s_SimpleAssemblyFullName, a.GetName().FullName);
                Guid mvid = a.ManifestModule.ModuleVersionId;
                Assert.Equal(TestData.s_SimpleAssemblyRecompiledMvid, mvid);
            }

            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Assert.Throws<FileLoadException>(() => lc.LoadFromByteArray(TestData.s_SimpleAssemblyRecompiledImage));
            }
        }

        [Fact]
        public static void LoadFromAssemblyNameNullString()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assert.Throws<ArgumentNullException>(() => lc.LoadFromAssemblyName((string)null));
            }
        }

        [Fact]
        public static void LoadFromAssemblyNameNullAssemblyName()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assert.Throws<ArgumentNullException>(() => lc.LoadFromAssemblyName((AssemblyName)null));
            }
        }

        [Fact]
        public static void LoadFromAssemblyPathNull()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assert.Throws<ArgumentNullException>(() => lc.LoadFromAssemblyPath(null));
            }
        }

        [Fact]
        public static void LoadFromByteArrayNull()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assert.Throws<ArgumentNullException>(() => lc.LoadFromByteArray(null));
            }
        }

        [Fact]
        public static void LoadFromStreamNull()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assert.Throws<ArgumentNullException>(() => lc.LoadFromStream(null));
            }
        }

        [Fact]
        public static void BadImageFormat()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                for (int i = 0; i < 100; i++)
                {
                    Stream s = new MemoryStream(new byte[i]);
                    Assert.Throws<BadImageFormatException>(() => lc.LoadFromStream(s));
                }
            }
        }

        [Fact]
        public static void LoadFromAssemblyNameNeverLoadedAssembly()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assert.Throws<FileNotFoundException>(() => lc.LoadFromAssemblyName("NeverSawThis"));
            }
        }
    }
}
