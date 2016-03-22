// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UnicodeEncodingEquals
    {
        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new UnicodeEncoding(), new UnicodeEncoding(), true };
            yield return new object[] { new UnicodeEncoding(), new UnicodeEncoding(false, true), true };
            yield return new object[] { new UnicodeEncoding(), new UnicodeEncoding(false, false), false };
            yield return new object[] { new UnicodeEncoding(), new TimeSpan(), false };
            yield return new object[] { new UnicodeEncoding(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(UnicodeEncoding encoding, object value, bool expected)
        {
            Assert.Equal(expected, encoding.Equals(value));
            if (value is UnicodeEncoding)
            {
                Assert.Equal(expected, encoding.GetHashCode().Equals(value.GetHashCode()));
            }
        }
    }
}
