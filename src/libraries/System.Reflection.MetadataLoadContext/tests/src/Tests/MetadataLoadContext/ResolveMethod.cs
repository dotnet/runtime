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
        public static void NoResolver()
        {
            using (MetadataLoadContext lc = new MetadataLoadContext(new EmptyCoreMetadataAssemblyResolver()))
            {
                Assembly derived = lc.LoadFromByteArray(TestData.s_DerivedClassWithVariationsOnFooImage);
                Type t = derived.GetType("Derived1", throwOnError: true);

                Assert.Throws<FileNotFoundException>(() => t.BaseType);
            }
        }

        [Fact]
        public static void ResolverMissingCore()
        {
            var resolver = new ResolverReturnsNull();
            using (MetadataLoadContext lc = new MetadataLoadContext(resolver, "EmptyCore"))
            {
                Assert.Same(lc, resolver.Context);
                Assert.Equal(1, resolver.CallCount);

                Assembly derived = lc.LoadFromByteArray(TestData.s_DerivedClassWithVariationsOnFooImage);
                Type t = derived.GetType("Derived1", throwOnError: true);
                Assert.Throws<FileNotFoundException>(() => t.BaseType);

                Assert.Same(lc, resolver.Context);
                Assert.Equal(resolver.AssemblyName.Name, "Foo");
                Assert.Equal(2, resolver.CallCount);
            }
        }

        [Fact]
        public static void ResolverReturnsSomething()
        {
            var resolver = new ResolverReturnsSomething();
            using (MetadataLoadContext lc = new MetadataLoadContext(resolver))
            {
                Assert.Same(lc, resolver.Context);
                Assert.Equal(1, resolver.CallCount);

                Assembly derived = lc.LoadFromByteArray(TestData.s_DerivedClassWithVariationsOnFooImage);
                Type t = derived.GetType("Derived1", throwOnError: true);
                Type bt = t.BaseType;

                Assert.Same(lc, resolver.Context);
                Assert.Equal(resolver.AssemblyName.Name, "Foo");
                Assert.Equal(2, resolver.CallCount);

                Assembly a = bt.Assembly;
                Assert.Equal(a, resolver.Assembly);
            }
        }

        [Fact]
        public static void ResolverThrows()
        {
            var resolver = new ResolverThrows();
            using (MetadataLoadContext lc = new MetadataLoadContext(resolver, "EmptyCore"))
            {
                Assert.Null(resolver.Context);
                Assert.Equal(0, resolver.CallCount);

                Assembly derived = lc.LoadFromByteArray(TestData.s_DerivedClassWithVariationsOnFooImage);
                Type t = derived.GetType("Derived1", throwOnError: true);
                TargetParameterCountException e = Assert.Throws<TargetParameterCountException>(() => t.BaseType);

                Assert.Same(lc, resolver.Context);
                Assert.Equal(1, resolver.CallCount);
                Assert.Equal("Hi!", e.Message);
            }
        }

        [Fact]
        public static void ResolverNoUnnecessaryCalls()
        {
            int resolveHandlerCallCount = 0;
            Assembly resolveEventHandlerResult = null;

            var resolver = new FuncMetadataAssemblyResolver(
                delegate (MetadataLoadContext context, AssemblyName name)
                {
                    if (name.Name == "Foo")
                    {
                        resolveHandlerCallCount++;
                        resolveEventHandlerResult = context.LoadFromByteArray(TestData.s_BaseClassesImage);
                        return resolveEventHandlerResult;
                    }

                    if (name.Name == "mscorlib")
                    {
                        return context.LoadFromByteArray(TestData.s_SimpleNameOnlyImage);
                    }

                    return null;
                });

            // In a single-threaded scenario at least, MetadataLoadContexts shouldn't ask the resolver to bind the same name twice.
            using (MetadataLoadContext lc = new MetadataLoadContext(resolver))
            {
                Assembly derived = lc.LoadFromByteArray(TestData.s_DerivedClassWithVariationsOnFooImage);
                Type t1 = derived.GetType("Derived1", throwOnError: true);
                Type bt1 = t1.BaseType;
                Type t2 = derived.GetType("Derived2", throwOnError: true);
                Type bt2 = t2.BaseType;
                Assert.Equal(1, resolveHandlerCallCount);
                Assert.Equal(resolveEventHandlerResult, bt1.Assembly);
                Assert.Equal(resolveEventHandlerResult, bt2.Assembly);
            }
        }

        [Fact]
        public static void ResolverMultipleCalls()
        {
            var resolver = new ResolverReturnsSomething();
            using (MetadataLoadContext lc = new MetadataLoadContext(resolver))
            {
                Assembly derived = lc.LoadFromByteArray(TestData.s_DerivedClassWithVariationsOnFooImage);

                int expectedCount = 2; // Includes the initial probe for mscorelib
                foreach (string typeName in new string[] { "Derived1", "Derived3", "Derived4", "Derived5", "Derived6" })
                {
                    Type t = derived.GetType(typeName, throwOnError: true);
                    Type bt = t.BaseType;
                    Assert.Equal(bt.Assembly, resolver.Assembly);
                    Assert.Equal(expectedCount++, resolver.CallCount);
                }
            }
        }

        [Fact]
        public static void ResolverFromReferencedAssembliesUsingFullPublicKeyReference()
        {
            // Ecma-335 allows an assembly reference to specify a full public key rather than the token. Ensure that those references
            // still hand out usable AssemblyNames to resolve handlers.

            AssemblyName assemblyNameReceivedByHandler = null;

            var resolver = new FuncMetadataAssemblyResolver(
                delegate (MetadataLoadContext context, AssemblyName name)
                {
                    if (name.Name == "RedirectCore")
                    {
                        return context.LoadFromByteArray(TestData.s_SimpleNameOnlyImage);
                    }

                    assemblyNameReceivedByHandler = name;
                    return null;
                });

            using (MetadataLoadContext lc = new MetadataLoadContext(resolver, "RedirectCore"))
            {
                Assembly a = lc.LoadFromByteArray(TestData.s_AssemblyRefUsingFullPublicKeyImage);
                Type t = a.GetType("C", throwOnError: true);

                // We expect this next to call to throw since it asks the MetadataLoadContext to resolve [mscorlib]System.Object and our
                // resolve handler doesn't return anything for that.
                Assert.Throws<FileNotFoundException> (() => t.BaseType);

                // But it did get called with a request to resolve "mscorlib" and we got the correct PKT calculated from the PK.
                // Note that the original PK is not made available (which follows prior precedent with these apis.) It's not like
                // anyone binds with the full PK...
                Assert.NotNull(assemblyNameReceivedByHandler);
                byte[] expectedPkt = "b77a5c561934e089".HexToByteArray();
                byte[] actualPkt = assemblyNameReceivedByHandler.GetPublicKeyToken();
                Assert.Equal<byte>(expectedPkt, actualPkt);
            }
        }
    }

    public class ResolverReturnsNull : MetadataAssemblyResolver
    {
        public override Assembly Resolve(System.Reflection.MetadataLoadContext context, AssemblyName assemblyName)
        {
            Context = context;
            AssemblyName = assemblyName;
            CallCount++;

            if (assemblyName.Name == "EmptyCore")
            {
                return context.LoadFromByteArray(TestData.s_SimpleNameOnlyImage);
            }

            return null;
        }

        public AssemblyName AssemblyName { get; private set; }
        public MetadataLoadContext Context { get; private set; }
        public int CallCount { get; private set; }
    }

    public class ResolverReturnsSomething : MetadataAssemblyResolver
    {
        public override Assembly Resolve(System.Reflection.MetadataLoadContext context, AssemblyName assemblyName)
        {
            Context = context;
            AssemblyName = assemblyName;
            CallCount++;

            Assembly = context.LoadFromByteArray(TestData.s_BaseClassesImage);
            return Assembly;
        }

        public Assembly Assembly { get; private set; }
        public AssemblyName AssemblyName { get; private set; }
        public MetadataLoadContext Context { get; private set; }
        public int CallCount { get; private set; }
    }

    public class ResolverThrows : MetadataAssemblyResolver
    {
        public override Assembly Resolve(System.Reflection.MetadataLoadContext context, AssemblyName assemblyName)
        {
            if (assemblyName.Name == "EmptyCore")
            {
                return context.LoadFromByteArray(TestData.s_SimpleNameOnlyImage);
            }

            Context = context;
            AssemblyName = assemblyName;
            CallCount++;

            throw new TargetParameterCountException("Hi!");
        }

        public AssemblyName AssemblyName { get; private set; }
        public MetadataLoadContext Context { get; private set; }
        public int CallCount { get; private set; }
    }
}
