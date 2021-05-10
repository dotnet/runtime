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
    }
}
