// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public partial class BigIntegerConstructorTest
    {
        static partial void VerifyCtorByteSpan(byte[] value) =>
            Assert.Equal(new BigInteger(value), new BigInteger(new ReadOnlySpan<byte>(value)));
    }
}
