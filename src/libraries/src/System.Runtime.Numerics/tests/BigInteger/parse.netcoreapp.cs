// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit;

namespace System.Numerics.Tests
{
    public partial class parseTest
    {
        static partial void VerifyParseSpanToString(string num1, NumberStyles ns, bool failureNotExpected, string expected)
        {
            if (failureNotExpected)
            {
                Eval(BigInteger.Parse(num1.AsReadOnlySpan(), ns), expected);
                Assert.True(BigInteger.TryParse(num1.AsReadOnlySpan(), out BigInteger test, ns));
                Eval(test, expected);
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1.AsReadOnlySpan(), ns); });
                Assert.False(BigInteger.TryParse(num1.AsReadOnlySpan(), out BigInteger test, ns), String.Format("Expected TryParse to fail on {0}", num1));
            }
        }

        static partial void VerifySimpleFormatParseSpan(string num1, NumberFormatInfo nfi, BigInteger expected, bool failureExpected)
        {
            if (!failureExpected)
            {
                Assert.Equal(expected, BigInteger.Parse(num1.AsReadOnlySpan(), provider: nfi));
                Assert.True(BigInteger.TryParse(num1.AsReadOnlySpan(), out BigInteger test, NumberStyles.Any, nfi));
                Assert.Equal(expected, test);
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1.AsReadOnlySpan(), provider: nfi); });
                Assert.False(BigInteger.TryParse(num1.AsReadOnlySpan(), out BigInteger test, NumberStyles.Any, nfi), String.Format("Expected TryParse to fail on {0}", num1));
            }
        }

        static partial void VerifyFormatParseSpan(string num1, NumberStyles ns, NumberFormatInfo nfi, BigInteger expected, bool failureExpected)
        {
            if (!failureExpected)
            {
                Assert.Equal(expected, BigInteger.Parse(num1.AsReadOnlySpan(), ns, nfi));
                Assert.True(BigInteger.TryParse(num1.AsReadOnlySpan(), out BigInteger test, NumberStyles.Any, nfi));
                Assert.Equal(expected, test);
            }
            else
            {
                Assert.Throws<FormatException>(() => { BigInteger.Parse(num1.AsReadOnlySpan(), ns, nfi); });
                Assert.False(BigInteger.TryParse(num1.AsReadOnlySpan(), out BigInteger test, ns, nfi), String.Format("Expected TryParse to fail on {0}", num1));
            }
        }
    }
}
