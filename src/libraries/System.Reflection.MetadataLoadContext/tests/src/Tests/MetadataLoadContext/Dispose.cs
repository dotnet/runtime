// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class MetadataLoadContextTests
    {
        [Fact]
        public static void DisposingReleasesFileLocks()
        {
            using (TempFile tf = TempFile.Create(TestData.s_SimpleAssemblyImage))
            {
                using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
                {
                    lc.LoadFromAssemblyPath(tf.Path);
                }

                try
                {
                    File.OpenWrite(tf.Path).Close();
                }
                catch (Exception)
                {
                    Assert.Fail("PE image file still locked after disposing MetadataLoadContext: " + tf.Path);
                }
            }
        }

        [Fact]
        public static void ExtraDisposesIgnored()
        {
            MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver());
            lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
            lc.Dispose();
            lc.Dispose();
            lc.Dispose();
        }

        [Fact]
        public static void MetadataLoadContextApisAfterDispose()
        {
            MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver());
            lc.Dispose();

            Assert.Throws<ObjectDisposedException>(() => lc.LoadFromAssemblyName(new AssemblyName("Foo")));
            Assert.Throws<ObjectDisposedException>(() => lc.LoadFromAssemblyName("Foo"));
            Assert.Throws<ObjectDisposedException>(() => lc.LoadFromAssemblyPath("Foo"));
            Assert.Throws<ObjectDisposedException>(() => lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage));
            Assert.Throws<ObjectDisposedException>(() => lc.LoadFromStream(new MemoryStream(TestData.s_SimpleAssemblyImage)));
            Assert.Throws<ObjectDisposedException>(() => lc.CoreAssembly);
            Assert.Throws<ObjectDisposedException>(() => lc.GetAssemblies());
        }

        [Fact]
        public static void MetadataLoadContextDispensedObjectsAfterDispose()
        {
            Assembly a;
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                a = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
            }

            Assert.Throws<ObjectDisposedException>(() => a.GetTypes());
        }
    }
}
