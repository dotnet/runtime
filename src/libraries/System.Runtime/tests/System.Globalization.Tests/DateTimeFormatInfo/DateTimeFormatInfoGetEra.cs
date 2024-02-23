// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace System.Globalization.Tests
{
    public class DateTimeFormatInfoGetEra
    {
        public static IEnumerable<object[]> GetEra_TestData()
        {
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "A.D.", 1 };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "AD", 1 };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "a.d.", 1 };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "ad", 1 };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "C.E", -1 };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "CE", -1 };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "B.C", -1 };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "BC", -1 };
            yield return new object[] { DateTimeFormatInfo.InvariantInfo, "", -1 };

            var enUSFormat = new CultureInfo("en-US").DateTimeFormat;
            yield return new object[] { enUSFormat, DateTimeFormatInfoData.EnUSEraName(), 1 };
            yield return new object[] { enUSFormat, DateTimeFormatInfoData.EnUSEraName().ToLower(), 1 };
            yield return new object[] { enUSFormat, DateTimeFormatInfoData.EnUSAbbreviatedEraName(), 1 };
            yield return new object[] { enUSFormat, DateTimeFormatInfoData.EnUSAbbreviatedEraName().ToLower(), 1 };
            yield return new object[] { enUSFormat, "C.E", -1 };
            yield return new object[] { enUSFormat, "CE", -1 };
            yield return new object[] { enUSFormat, "B.C", -1 };
            yield return new object[] { enUSFormat, "BC", -1 };
            yield return new object[] { enUSFormat, "", -1 };

            // For Win7, "fr-FR" is "ap J.-C".
            // For windows<Win7 & MAC, every culture is "A.D."
            var frFRFormat = new CultureInfo("fr-FR").DateTimeFormat;
            yield return new object[] { frFRFormat, "A.D.", -1 };
            yield return new object[] { frFRFormat, "AD", -1 };
            yield return new object[] { frFRFormat, "a.d.", -1 };
            yield return new object[] { frFRFormat, "ad", -1 };
            yield return new object[] { frFRFormat, "C.E", -1 };
            yield return new object[] { frFRFormat, "CE", -1 };
            yield return new object[] { frFRFormat, "B.C", -1 };
            yield return new object[] { frFRFormat, "BC", -1 };
            yield return new object[] { frFRFormat, "ap. J.-C.", 1 };
            yield return new object[] { frFRFormat, "AP. J.-C.", 1 };
            yield return new object[] { frFRFormat, "ap. j.-c.", 1 };
            yield return new object[] { frFRFormat, "ap J-C", -1 };
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotHybridGlobalizationOnApplePlatform))]
        [MemberData(nameof(GetEra_TestData))]
        public void GetEra_Invoke_ReturnsExpected(DateTimeFormatInfo format, string eraName, int expected)
        {
            Assert.Equal(expected, format.GetEra(eraName));
        }

        [Fact]
        public void GetEra_NullEraName_ThrowsArgumentNullException()
        {
            var format = new DateTimeFormatInfo();
            AssertExtensions.Throws<ArgumentNullException>("eraName", () => format.GetEra(null));
        }
    }
}
