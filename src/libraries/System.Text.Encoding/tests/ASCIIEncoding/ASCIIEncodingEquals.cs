// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Tests
{
    public class ASCIIEncodingEquals
    {
        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new ASCIIEncoding(), new ASCIIEncoding(), true };
            yield return new object[] { Encoding.ASCII, Encoding.ASCII, true };
            yield return new object[] { Encoding.ASCII, new ASCIIEncoding(), true };
            yield return new object[] { Encoding.ASCII, Encoding.GetEncoding("ascii"), true };
            yield return new object[] { Encoding.ASCII, Encoding.GetEncoding("us-ascii"), true };

            yield return new object[] { new ASCIIEncoding(), new object(), false };
            yield return new object[] { new ASCIIEncoding(), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(ASCIIEncoding encoding, object value, bool expected)
        {
            Assert.Equal(expected, encoding.Equals(value));
            if (value is ASCIIEncoding)
            {
                Assert.Equal(expected, encoding.GetHashCode().Equals(value.GetHashCode()));
            }
        }
    }
}
