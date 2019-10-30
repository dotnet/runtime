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
        public static void MissingResolver()
        {
            Assert.Throws<ArgumentNullException>(() => new MetadataLoadContext(null));
        }

        [Fact]
        public static void InvalidAssemblyName()
        {
            Assert.Throws<ArgumentException>(() => new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver(), ""));
        }

        [Fact]
        public static void CoreAssemblyCanBeAFacade()
        {
            Assembly actualCoreAssembly = null;
            Assembly testAssembly = null;

            var resolver = new FuncMetadataAssemblyResolver(
                delegate (MetadataLoadContext context, AssemblyName refName)
                {
                    if (refName.Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase))
                    {
                        return actualCoreAssembly = context.LoadFromStream(TestUtils.CreateStreamForCoreAssembly());
                    }
                    else if (refName.Name == new AssemblyName(TestData.s_PhonyCoreAssemblyFullName).Name)
                    {
                        return testAssembly = context.LoadFromByteArray(TestData.s_PhonyCoreAssemblyImage);
                    }
                    return null;
                });

            using (MetadataLoadContext lc = new MetadataLoadContext(resolver, coreAssemblyName: TestData.s_PhonyCoreAssemblyFullName))
            {
                // This is a sanity check to ensure that "TestData.s_PhonyCoreAssemblyFullName" is actually the def-name of this
                // assembly. It better be since we told our MetadataLoadContext to use it as our core assembly.
                Assembly aAgain = lc.LoadFromAssemblyName(TestData.s_PhonyCoreAssemblyFullName);

                Type derived = testAssembly.GetType("Derived", throwOnError: true, ignoreCase: false);

                // Calling BaseType causes the MetadataLoadContext to parse the typespec "Base<object>". Since "object" is a primitive
                // type, it should be encoded using the short-form "ELEMENT_TYPE_OBJECT." Hence, the MetadataLoadContext is forced
                // to look up "System.Object" in the core assembly we assigned to it, which in this case is "PhonyCoreAssembly"
                // which type-forwards System.Object to "mscorlib".
                Type baseType = derived.BaseType;

                Assert.NotNull(actualCoreAssembly); // Ensure our resolve handler actually ran.
                Assert.NotEqual(testAssembly, actualCoreAssembly);
                Assert.True(baseType.IsConstructedGenericType);
                Type retrievedObjectType = baseType.GenericTypeArguments[0];
                Assert.Equal("System.Object", retrievedObjectType.FullName);
                Assert.Equal(actualCoreAssembly, retrievedObjectType.Assembly);
            }
        }
    }
}
