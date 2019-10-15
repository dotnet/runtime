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
        public static void LoadExternalAssembly1()
        {
            Assembly runtimeAssembly = typeof(object).Assembly;  // Intentionally not projected.

            using (MetadataLoadContext lc = new MetadataLoadContext(
                new FuncMetadataAssemblyResolver(
                    delegate (MetadataLoadContext context, AssemblyName assemblyName)
                    {
                        if (assemblyName.Name == "SomeAssembly")
                        {
                            return runtimeAssembly;
                        }
                        else if (assemblyName.Name == "mscorlib")
                        {
                            return context.LoadFromByteArray(TestData.s_SimpleNameOnlyImage);
                        }
                        return null;
                    }
                    )))
            {
                string location = runtimeAssembly.Location;

                Assert.Throws<FileLoadException>(() => lc.LoadFromAssemblyName("SomeAssembly"));
            }
        }
    }
}
