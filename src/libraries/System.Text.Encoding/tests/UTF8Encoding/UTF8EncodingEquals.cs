// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF8EncodingEquals
    {
        public static IEnumerable<object[]> Equals_TestData()
        {
            UTF8Encoding encoding = new UTF8Encoding();
            yield return new object[] { encoding, encoding, true };

            yield return new object[] { new UTF8Encoding(), new UTF8Encoding(), true };

            yield return new object[] { new UTF8Encoding(), new UTF8Encoding(false), true };
            yield return new object[] { new UTF8Encoding(), new UTF8Encoding(true), false };

            yield return new object[] { new UTF8Encoding(false), new UTF8Encoding(false), true };
            yield return new object[] { new UTF8Encoding(true), new UTF8Encoding(true), true };
            yield return new object[] { new UTF8Encoding(true), new UTF8Encoding(false), false };

            yield return new object[] { new UTF8Encoding(), new UTF8Encoding(false, false), true };
            yield return new object[] { new UTF8Encoding(), new UTF8Encoding(false, true), false };

            yield return new object[] { new UTF8Encoding(false), new UTF8Encoding(false, false), true };
            yield return new object[] { new UTF8Encoding(true), new UTF8Encoding(true, false), true };
            yield return new object[] { new UTF8Encoding(false), new UTF8Encoding(false, true), false };

            yield return new object[] { new UTF8Encoding(true, true), new UTF8Encoding(true, true), true };
            yield return new object[] { new UTF8Encoding(false, false), new UTF8Encoding(false, false), true };
            yield return new object[] { new UTF8Encoding(true, false), new UTF8Encoding(true, false), true };
            yield return new object[] { new UTF8Encoding(true, false), new UTF8Encoding(false, true), false };

            yield return new object[] { new UTF8Encoding(), new TimeSpan(), false };
            yield return new object[] { new UTF8Encoding(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(UTF8Encoding encoding, object value, bool expected)
        {
            Assert.Equal(expected, encoding.Equals(value));
            if (value is UTF8Encoding)
            {
                Assert.Equal(expected, encoding.GetHashCode().Equals(value.GetHashCode()));
            }
        }
    }
}
