// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class UTF7EncodingEquals
    {
        public static IEnumerable<object[]> Equals_TestData()
        {
            UTF7Encoding encoding = new UTF7Encoding();
            yield return new object[] { encoding, encoding, true };
            yield return new object[] { new UTF7Encoding(), new UTF7Encoding(), true };

            yield return new object[] { new UTF7Encoding(), new TimeSpan(), false };
            yield return new object[] { new UTF7Encoding(), null, false };

            yield return new object[] { new UTF7Encoding(), new UTF7Encoding(true), false };
            yield return new object[] { new UTF7Encoding(), new UTF7Encoding(false), true };

            yield return new object[] { new UTF7Encoding(true), new UTF7Encoding(true), true };
            yield return new object[] { new UTF7Encoding(true), new UTF7Encoding(false), false };

            yield return new object[] { new UTF7Encoding(false), new UTF7Encoding(false), true };
            yield return new object[] { new UTF7Encoding(false), new UTF7Encoding(true), false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(UTF7Encoding encoding, object value, bool expected)
        {
            Assert.Equal(expected, encoding.Equals(value));
        }
    }
}
