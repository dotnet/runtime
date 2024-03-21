// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [Fact]
        public static void TestIsEnumAfterLoadingNonCoreAssembly()
        {
            using MetadataLoadContext context = new MetadataLoadContext(new Resolver(), "System.Runtime");
            Assembly? mscorlib = context.LoadFromAssemblyPath("mscorlib-net48.dll");
            foreach(var attr in mscorlib.GetCustomAttributesData())
            {
                Exception exception = Record.Exception(() => attr.ConstructorArguments.Count);
                Assert.Null(exception);
            }
        }

        class Resolver : MetadataAssemblyResolver
        {
            public override Assembly? Resolve(MetadataLoadContext context, AssemblyName assemblyName)
            {
                if(assemblyName.Name == "System.Runtime")
                {
                    return context.LoadFromAssemblyPath(typeof(object).Assembly.Location);
                }
                return null;
            }
        }
    }
}
