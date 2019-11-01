// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Cryptography.OpenSsl.Tests
{
    public static class SafeEvpPKeyHandleTests
    {
        [Fact]
        public static void TestOpenSslVersion()
        {
            long version = SafeEvpPKeyHandle.OpenSslVersion;
            long version2 = SafeEvpPKeyHandle.OpenSslVersion;

            Assert.Equal(version, version2);

            // A value representing OpenSSL 1.0.0's development (pre-beta) build.
            const long MinValue = 0x10000000;

            // Until a platform+build is discovered which violates this constraint, assert that it
            // is between 1.0.0-devel and (signed) int.MaxValue as a sanity check on reading the
            // value.
            //
            // NOTE: The OpenSslVersion value should not be depended upon for anything other than
            // an equality check, to assert that a component outside of .NET Core which is utilizing
            // SafeEvpPKeyHandle is using the same version as .NET Core (to avoid sending the pointers
            // from one library into another).  The exception is this test, in asserting that we're
            // getting "sensible" values.
            Assert.InRange(version, MinValue, int.MaxValue);
        }
    }
}
