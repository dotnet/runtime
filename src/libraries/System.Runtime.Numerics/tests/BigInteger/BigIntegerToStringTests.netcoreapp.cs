// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public partial class ToStringTest
    {
        static partial void VerifyTryFormat(string test, string format, IFormatProvider provider, bool expectError, string expectedResult)
        {
            try
            {
                BigInteger bi = BigInteger.Parse(test, provider);

                char[] destination = expectedResult != null ? new char[expectedResult.Length] : Array.Empty<char>();
                Assert.True(bi.TryFormat(destination, out int charsWritten, format, provider));
                Assert.False(expectError);

                VerifyExpectedStringResult(expectedResult, new string(destination, 0, charsWritten));

                if (expectedResult.Length > 0)
                {
                    Assert.False(bi.TryFormat(new char[expectedResult.Length - 1], out charsWritten, format, provider));
                    Assert.Equal(0, charsWritten);
                }
            }
            catch (FormatException)
            {
                Assert.True(expectError);
            }
        }
    }
}
