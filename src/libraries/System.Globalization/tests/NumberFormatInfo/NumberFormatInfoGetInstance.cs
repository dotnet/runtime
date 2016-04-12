// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoGetInstance
    {
        public static IEnumerable<object[]> GetInstance_TestData()
        {
            CultureInfo frFRCulture = new CultureInfo("fr-FR");
            yield return new object[] { frFRCulture, frFRCulture.NumberFormat };
            yield return new object[] { frFRCulture.NumberFormat, frFRCulture.NumberFormat };
            yield return new object[] { new CustomFormatProvider(), CustomFormatProvider.CustomFormat };

            yield return new object[] { new InvalidFormatProvider(), NumberFormatInfo.CurrentInfo };
            yield return new object[] { null, NumberFormatInfo.CurrentInfo };
        }

        [Theory]
        [MemberData(nameof(GetInstance_TestData))]
        public void GetInstance(IFormatProvider formatProvider, NumberFormatInfo expected)
        {
            Assert.Equal(expected, NumberFormatInfo.GetInstance(formatProvider));
        }

        private class CustomFormatProvider : IFormatProvider
        {
            public static NumberFormatInfo CustomFormat { get; } = new CultureInfo("fr-FR").NumberFormat;

            public object GetFormat(Type formatType) => CustomFormat;
        }

        private class InvalidFormatProvider : IFormatProvider
        {
            public object GetFormat(Type formatType) => "hello";
        }
    }
}
