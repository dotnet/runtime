// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Data.Common;
using System.Globalization;

using Xunit;

namespace System.Data.Tests.Common
{
    public class DbCommandBuilderTest
    {
        [Fact]
        public void CatalogLocationTest()
        {
            MyCommandBuilder cb = new MyCommandBuilder();
            Assert.Equal(CatalogLocation.Start, cb.CatalogLocation);
            cb.CatalogLocation = CatalogLocation.End;
            Assert.Equal(CatalogLocation.End, cb.CatalogLocation);
        }

        [Fact]
        public void CatalogLocation_Value_Invalid()
        {
            MyCommandBuilder cb = new MyCommandBuilder();
            cb.CatalogLocation = CatalogLocation.End;

            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => cb.CatalogLocation = (CatalogLocation)666);
            // The CatalogLocation enumeration value, 666, is invalid
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Contains("CatalogLocation", ex.Message);
            Assert.Contains("666", ex.Message);
            Assert.Equal("CatalogLocation", ex.ParamName);

            Assert.Equal(CatalogLocation.End, cb.CatalogLocation);
        }

        [Fact]
        public void CatalogSeparator()
        {
            MyCommandBuilder cb = new MyCommandBuilder();
            Assert.Equal(".", cb.CatalogSeparator);
            cb.CatalogSeparator = "a";
            Assert.Equal("a", cb.CatalogSeparator);
            cb.CatalogSeparator = null;
            Assert.Equal(".", cb.CatalogSeparator);
            cb.CatalogSeparator = "b";
            Assert.Equal("b", cb.CatalogSeparator);
            cb.CatalogSeparator = string.Empty;
            Assert.Equal(".", cb.CatalogSeparator);
            cb.CatalogSeparator = " ";
            Assert.Equal(" ", cb.CatalogSeparator);
        }

        [Fact]
        public void ConflictOptionTest()
        {
            MyCommandBuilder cb = new MyCommandBuilder();
            Assert.Equal(ConflictOption.CompareAllSearchableValues, cb.ConflictOption);
            cb.ConflictOption = ConflictOption.CompareRowVersion;
            Assert.Equal(ConflictOption.CompareRowVersion, cb.ConflictOption);
        }

        [Fact]
        public void ConflictOption_Value_Invalid()
        {
            MyCommandBuilder cb = new MyCommandBuilder();
            cb.ConflictOption = ConflictOption.CompareRowVersion;
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => cb.ConflictOption = (ConflictOption)666);
            // The ConflictOption enumeration value, 666, is invalid
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Contains("ConflictOption", ex.Message);
            Assert.Contains("666", ex.Message);
            Assert.Equal("ConflictOption", ex.ParamName);

            Assert.Equal(ConflictOption.CompareRowVersion, cb.ConflictOption);
        }

        [Fact] // QuoteIdentifier (String)
        public void QuoteIdentifier()
        {
            MyCommandBuilder cb = new MyCommandBuilder();

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => cb.QuoteIdentifier(null));
            Assert.Null(ex.InnerException);
            Assert.Equal((new NotSupportedException()).Message, ex.Message);

            NotSupportedException ex2 = Assert.Throws<NotSupportedException>(() => cb.QuoteIdentifier("mono"));
            Assert.Null(ex.InnerException);
            Assert.Equal((new NotSupportedException()).Message, ex.Message);
        }

        [Fact]
        public void QuotePrefix()
        {
            MyCommandBuilder cb = new MyCommandBuilder();
            Assert.Equal(string.Empty, cb.QuotePrefix);
            cb.QuotePrefix = "mono";
            Assert.Equal("mono", cb.QuotePrefix);
            cb.QuotePrefix = null;
            Assert.Equal(string.Empty, cb.QuotePrefix);
            cb.QuotePrefix = "'\"";
            Assert.Equal("'\"", cb.QuotePrefix);
            cb.QuotePrefix = string.Empty;
            Assert.Equal(string.Empty, cb.QuotePrefix);
            cb.QuotePrefix = " ";
            Assert.Equal(" ", cb.QuotePrefix);
        }

        [Fact]
        public void QuoteSuffix()
        {
            MyCommandBuilder cb = new MyCommandBuilder();
            Assert.Equal(string.Empty, cb.QuoteSuffix);
            cb.QuoteSuffix = "mono";
            Assert.Equal("mono", cb.QuoteSuffix);
            cb.QuoteSuffix = null;
            Assert.Equal(string.Empty, cb.QuoteSuffix);
            cb.QuoteSuffix = "'\"";
            Assert.Equal("'\"", cb.QuoteSuffix);
            cb.QuoteSuffix = string.Empty;
            Assert.Equal(string.Empty, cb.QuoteSuffix);
            cb.QuoteSuffix = " ";
            Assert.Equal(" ", cb.QuoteSuffix);
        }

        [Fact]
        public void SchemaSeparator()
        {
            MyCommandBuilder cb = new MyCommandBuilder();
            Assert.Equal(".", cb.SchemaSeparator);
            cb.SchemaSeparator = "a";
            Assert.Equal("a", cb.SchemaSeparator);
            cb.SchemaSeparator = null;
            Assert.Equal(".", cb.SchemaSeparator);
            cb.SchemaSeparator = "b";
            Assert.Equal("b", cb.SchemaSeparator);
            cb.SchemaSeparator = string.Empty;
            Assert.Equal(".", cb.SchemaSeparator);
            cb.SchemaSeparator = " ";
            Assert.Equal(" ", cb.SchemaSeparator);
        }

        private class MyCommandBuilder : DbCommandBuilder
        {
            protected override string GetParameterPlaceholder(int parameterOrdinal)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "@PH:{0}@", parameterOrdinal);
            }

            protected override string GetParameterName(string parameterName)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "@NAME:{0}@", parameterName);
            }

            protected override string GetParameterName(int parameterOrdinal)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "@NAME:{0}@", parameterOrdinal);
            }

            protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
            {
            }

            protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
            {
            }
        }
    }
}
