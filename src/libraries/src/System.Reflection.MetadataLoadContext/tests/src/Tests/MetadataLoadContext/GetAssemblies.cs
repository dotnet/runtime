// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class MetadataLoadContextTests
    {
        [Fact]
        public static void GetAssemblies_EmptyMetadataLoadContext()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly[] loadedAssemblies = lc.GetAssemblies().ToArray();
                Assert.Equal(1, loadedAssemblies.Length);
            }
        }

        [Fact]
        public static void GetAssemblies()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly[] loadedAssemblies = lc.GetAssemblies().ToArray();
                Assert.Equal(1, loadedAssemblies.Length);

                // EmptyCoreMetadataAssemblyResolver already loaded s_SimpleNameOnlyImage
                Assembly a1 = lc.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                Assembly a2 = lc.LoadFromByteArray(TestData.s_SimpleNameOnlyImage);
                loadedAssemblies = lc.GetAssemblies().ToArray();
                Assert.Equal(2, loadedAssemblies.Length);
                Assert.Contains<Assembly>(a1, loadedAssemblies);
                Assert.Contains<Assembly>(a2, loadedAssemblies);
            }
        }

        [Fact]
        public static void GetAssemblies_SnapshotIsAtomic()
        {
            Assembly a1 = null;

            var resolver = new FuncMetadataAssemblyResolver(
                delegate (MetadataLoadContext context, AssemblyName refName)
                {
                    if (a1 == null)
                    {
                        // Initial request to load core assembly
                        return a1 = context.LoadFromByteArray(TestData.s_SimpleAssemblyImage);
                    }
                    return null;
                });

            using (MetadataLoadContext lc = new MetadataLoadContext(resolver))
            {
                IEnumerable<Assembly> loadedAssembliesSnapshot = lc.GetAssemblies();
                Assembly a2 = lc.LoadFromByteArray(TestData.s_SimpleNameOnlyImage);
                Assembly[] loadedAssemblies = loadedAssembliesSnapshot.ToArray();
                Assert.Equal(1, loadedAssemblies.Length);
                Assert.Equal(a1, loadedAssemblies[0]);
            }
        }
    }
}
