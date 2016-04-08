// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlTypes;
using System.Globalization;

using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class SqlStringTest
    {
        private const SqlCompareOptions DefaultOptions =
            SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth;

        [Fact]
        public void Constructor_Value_Success()
        {
            const string value = "foo";
            ValidateProperties(value, CultureInfo.CurrentCulture, new SqlString(value));
        }

        [Theory]
        [InlineData(1033, "en-US")]
        [InlineData(1036, "fr-FR")]
        public void Constructor_ValueLcid_Success(int lcid, string name)
        {
            const string value = "foo";
            ValidateProperties(value, new CultureInfo(name), new SqlString(value, lcid));
        }

        private static void ValidateProperties(string value, CultureInfo culture, SqlString sqlString)
        {
            Assert.Same(value, sqlString.Value);
            Assert.False(sqlString.IsNull);
            Assert.Equal(DefaultOptions, sqlString.SqlCompareOptions);
            Assert.Equal(culture, sqlString.CultureInfo);
            Assert.Equal(culture.CompareInfo, sqlString.CompareInfo);
        }

        [Fact]
        public void CultureInfo_InvalidLcid_Throws()
        {
            const string value = "foo";

            Assert.Throws<ArgumentOutOfRangeException>(() => new SqlString(value, int.MinValue).CultureInfo);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SqlString(value, -1).CultureInfo);

            Assert.Throws<CultureNotFoundException>(() => new SqlString(value, int.MaxValue).CultureInfo);
        }
    }
}
