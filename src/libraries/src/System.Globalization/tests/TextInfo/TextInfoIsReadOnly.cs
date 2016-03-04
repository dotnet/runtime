// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoIsReadOnly
    {
        public static IEnumerable<object[]> IsReadOnly_TestData()
        {
            yield return new object[] { CultureInfo.ReadOnly(new CultureInfo("en-US")).TextInfo, true };
            yield return new object[] { CultureInfo.InvariantCulture.TextInfo, true };
            yield return new object[] { new CultureInfo("").TextInfo, false };
            yield return new object[] { new CultureInfo("en-US").TextInfo, false };
            yield return new object[] { new CultureInfo("fr-FR").TextInfo, false };
        }

        [Theory]
        [MemberData(nameof(IsReadOnly_TestData))]
        public void IsReadOnly(TextInfo textInfo, bool expected)
        {
            Assert.Equal(expected, textInfo.IsReadOnly);
        }
    }
}
