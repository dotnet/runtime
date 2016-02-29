// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class TextInfoCultureName
    {
        [Theory]
        public static IEnumerable<object[]> CultureName_TestData()
        {
            yield return new object[] { CultureInfo.InvariantCulture.TextInfo, "" };
            yield return new object[] { new CultureInfo("").TextInfo, "" };
            yield return new object[] { new CultureInfo("en-US").TextInfo, "en-US" };
            yield return new object[] { new CultureInfo("fr-FR").TextInfo, "fr-FR" };
            yield return new object[] { new CultureInfo("EN-us").TextInfo, "en-US" };
            yield return new object[] { new CultureInfo("FR-fr").TextInfo, "fr-FR" };
        }

        [Theory]
        [MemberData(nameof(CultureName_TestData))]
        public void CultureName(TextInfo textInfo, string expected)
        {
            Assert.Equal(expected, textInfo.CultureName);
        }
    }
}
