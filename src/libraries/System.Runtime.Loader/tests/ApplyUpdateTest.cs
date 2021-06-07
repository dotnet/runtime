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
    [ConditionalClass(typeof(ApplyUpdateUtil), nameof (ApplyUpdateUtil.IsSupported))]
    public class ApplyUpdateTest
    {
        [Fact]
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

        [Fact]
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
    }
}
