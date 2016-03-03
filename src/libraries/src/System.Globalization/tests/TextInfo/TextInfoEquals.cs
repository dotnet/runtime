// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoEquals
    {
        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { CultureInfo.InvariantCulture.TextInfo, CultureInfo.InvariantCulture.TextInfo, true };
            yield return new object[] { CultureInfo.InvariantCulture.TextInfo, new CultureInfo("").TextInfo, true };
            yield return new object[] { CultureInfo.InvariantCulture.TextInfo, new CultureInfo("en-US"), false };

            yield return new object[] { new CultureInfo("en-US").TextInfo, new CultureInfo("en-US").TextInfo, true };
            yield return new object[] { new CultureInfo("en-US").TextInfo, new CultureInfo("fr-FR").TextInfo, false };

            yield return new object[] { new CultureInfo("en-US").TextInfo, null, false };
            yield return new object[] { new CultureInfo("en-US").TextInfo, new object(), false };
            yield return new object[] { new CultureInfo("en-US").TextInfo, 123, false };
            yield return new object[] { new CultureInfo("en-US").TextInfo, "en-US", false };
           
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(TextInfo textInfo, object obj, bool expected)
        {
            Assert.Equal(expected, textInfo.Equals(obj));
            if (obj is TextInfo)
            {
                Assert.Equal(expected, textInfo.GetHashCode().Equals(obj.GetHashCode()));
            }
        }
    }
}
