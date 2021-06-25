// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Metadata
{
    ///
    /// The general setup for ApplyUpdate tests is:
    ///
    /// Each test Foo has a corresponding assembly under
    /// System.Reflection.Metadata.ApplyUpate.Test.Foo The Foo.csproj has a delta
    /// script that applies one or more updates to Foo.dll The ApplyUpdateTest
    /// testsuite runs each test in sequence, loading the corresponding
    /// assembly, applying an update to it and observing the results.
    [Collection(nameof(ApplyUpdateUtil.NoParallelTests))]
    public class ApplyUpdateTest
    {
        [ConditionalFact(typeof(ApplyUpdateUtil), nameof (ApplyUpdateUtil.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/54617", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        void StaticMethodBodyUpdate()
        {
            ApplyUpdateUtil.TestCase(static () =>
            {
                var assm = typeof (ApplyUpdate.Test.MethodBody1).Assembly;

                var r = ApplyUpdate.Test.MethodBody1.StaticMethod1();
                Assert.Equal("OLD STRING", r);

                ApplyUpdateUtil.ApplyUpdate(assm);

                r = ApplyUpdate.Test.MethodBody1.StaticMethod1();
                Assert.Equal("NEW STRING", r);

                ApplyUpdateUtil.ApplyUpdate(assm);

                r = ApplyUpdate.Test.MethodBody1.StaticMethod1 ();
                Assert.Equal ("NEWEST STRING", r);
            });
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof (ApplyUpdateUtil.IsSupported))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/52993", TestRuntimes.Mono)]
        void ClassWithCustomAttributes()
        {
            ApplyUpdateUtil.TestCase(static () =>
            {
                // Get the custom attribtues from a newly-added type and method
                // and check that they are the expected ones.
                var assm = typeof(ApplyUpdate.Test.ClassWithCustomAttributesHelper).Assembly;

                // returns ClassWithCustomAttributes
                var ty = ApplyUpdate.Test.ClassWithCustomAttributesHelper.GetAttributedClass();
                Assert.NotNull (ty);

                ApplyUpdateUtil.ApplyUpdate(assm);
                ApplyUpdateUtil.ClearAllReflectionCaches();

                // returns ClassWithCustomAttributes2
                ty = ApplyUpdate.Test.ClassWithCustomAttributesHelper.GetAttributedClass();
                Assert.NotNull (ty);

                var attrType = typeof(ObsoleteAttribute);

                var cattrs = Attribute.GetCustomAttributes(ty, attrType);

                Assert.NotNull(cattrs);
                Assert.Equal(1, cattrs.Length);
                Assert.NotNull(cattrs[0]);
                Assert.Equal(attrType, cattrs[0].GetType());

                var methodName = "Method2";
                var mi = ty.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);

                Assert.NotNull (mi);

                cattrs = Attribute.GetCustomAttributes(mi, attrType);

                Assert.NotNull(cattrs);
                Assert.Equal(1, cattrs.Length);
                Assert.NotNull(cattrs[0]);
                Assert.Equal(attrType, cattrs[0].GetType());
            });
        }

        class NonRuntimeAssembly : Assembly
        {
        }

        [Fact]
        public static void ApplyUpdateInvalidParameters()
        {
            // Dummy delta arrays
            var metadataDelta = new byte[20];
            var ilDelta = new byte[20];

            // Assembly can't be null
            Assert.Throws<ArgumentNullException>("assembly", () =>
                MetadataUpdater.ApplyUpdate(null, new ReadOnlySpan<byte>(metadataDelta), new ReadOnlySpan<byte>(ilDelta), ReadOnlySpan<byte>.Empty));

            // Tests fail on non-runtime assemblies
            Assert.Throws<ArgumentException>(() =>
                MetadataUpdater.ApplyUpdate(new NonRuntimeAssembly(), new ReadOnlySpan<byte>(metadataDelta), new ReadOnlySpan<byte>(ilDelta), ReadOnlySpan<byte>.Empty));

            // Tests that this assembly isn't not editable
            Assert.Throws<InvalidOperationException>(() =>
                MetadataUpdater.ApplyUpdate(typeof(AssemblyExtensions).Assembly, new ReadOnlySpan<byte>(metadataDelta), new ReadOnlySpan<byte>(ilDelta), ReadOnlySpan<byte>.Empty));
        }

        [Fact]
        public static void GetCapabilities()
        {
            var ty = typeof(System.Reflection.Metadata.MetadataUpdater);
            var mi = ty.GetMethod("GetCapabilities", BindingFlags.NonPublic | BindingFlags.Static, Array.Empty<Type>());

            Assert.NotNull(mi);

            var result = mi.Invoke(null, null);

            Assert.NotNull(result);
            Assert.Equal(typeof(string), result.GetType());
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof(ApplyUpdateUtil.TestUsingRemoteExecutor))]
        public static void IsSupported()
        {
            bool result = MetadataUpdater.IsSupported;
            Assert.False(result);
        }

        [ConditionalFact(typeof(ApplyUpdateUtil), nameof(ApplyUpdateUtil.TestUsingLaunchEnvironment))]
        public static void IsSupported2()
        {
            bool result = MetadataUpdater.IsSupported;
            Assert.True(result);
        }
    }
}
