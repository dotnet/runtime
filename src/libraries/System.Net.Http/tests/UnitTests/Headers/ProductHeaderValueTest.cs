// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class ProductHeaderValueTest
    {
        [Fact]
        public void Ctor_SetValidHeaderValues_InstanceCreatedCorrectly()
        {
            ProductHeaderValue product = new ProductHeaderValue("HTTP", "2.0");
            Assert.Equal("HTTP", product.Name);
            Assert.Equal("2.0", product.Version);

            product = new ProductHeaderValue("HTTP");
            Assert.Equal("HTTP", product.Name);
            Assert.Null(product.Version);

            product = new ProductHeaderValue("HTTP", ""); // null and string.Empty are equivalent
            Assert.Equal("HTTP", product.Name);
            Assert.Null(product.Version);
        }

        [Fact]
        public void Ctor_UseInvalidValues_Throw()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => { new ProductHeaderValue(null); });
            AssertExtensions.Throws<ArgumentException>("name", () => { new ProductHeaderValue(string.Empty); });
            Assert.Throws<FormatException>(() => { new ProductHeaderValue(" x"); });
            Assert.Throws<FormatException>(() => { new ProductHeaderValue("x "); });
            Assert.Throws<FormatException>(() => { new ProductHeaderValue("x y"); });

            Assert.Throws<FormatException>(() => { new ProductHeaderValue("x", " y"); });
            Assert.Throws<FormatException>(() => { new ProductHeaderValue("x", "y "); });
            Assert.Throws<FormatException>(() => { new ProductHeaderValue("x", "y z"); });
        }

        [Fact]
        public void ToString_UseDifferentRanges_AllSerializedCorrectly()
        {
            ProductHeaderValue product = new ProductHeaderValue("IRC", "6.9");
            Assert.Equal("IRC/6.9", product.ToString());

            product = new ProductHeaderValue("product");
            Assert.Equal("product", product.ToString());
        }

        [Fact]
        public void GetHashCode_UseSameAndDifferentRanges_SameOrDifferentHashCodes()
        {
            ProductHeaderValue product1 = new ProductHeaderValue("custom", "1.0");
            ProductHeaderValue product2 = new ProductHeaderValue("custom");
            ProductHeaderValue product3 = new ProductHeaderValue("CUSTOM", "1.0");
            ProductHeaderValue product4 = new ProductHeaderValue("RTA", "x11");
            ProductHeaderValue product5 = new ProductHeaderValue("rta", "X11");

            Assert.NotEqual(product1.GetHashCode(), product2.GetHashCode());
            Assert.Equal(product1.GetHashCode(), product3.GetHashCode());
            Assert.NotEqual(product1.GetHashCode(), product4.GetHashCode());
            Assert.Equal(product4.GetHashCode(), product5.GetHashCode());
        }

        [Fact]
        public void Equals_UseSameAndDifferentRanges_EqualOrNotEqualNoExceptions()
        {
            ProductHeaderValue product1 = new ProductHeaderValue("custom", "1.0");
            ProductHeaderValue product2 = new ProductHeaderValue("custom");
            ProductHeaderValue product3 = new ProductHeaderValue("CUSTOM", "1.0");
            ProductHeaderValue product4 = new ProductHeaderValue("RTA", "x11");
            ProductHeaderValue product5 = new ProductHeaderValue("rta", "X11");

            Assert.False(product1.Equals(null), "custom/1.0 vs. <null>");
            Assert.False(product1.Equals(product2), "custom/1.0 vs. custom");
            Assert.False(product2.Equals(product1), "custom/1.0 vs. custom");
            Assert.True(product1.Equals(product3), "custom/1.0 vs. CUSTOM/1.0");
            Assert.False(product1.Equals(product4), "custom/1.0 vs. rta/X11");
            Assert.True(product4.Equals(product5), "RTA/x11 vs. rta/X11");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            ProductHeaderValue source = new ProductHeaderValue("SHTTP", "1.3");
            ProductHeaderValue clone = (ProductHeaderValue)((ICloneable)source).Clone();

            Assert.Equal(source.Name, clone.Name);
            Assert.Equal(source.Version, clone.Version);

            source = new ProductHeaderValue("SHTTP", null);
            clone = (ProductHeaderValue)((ICloneable)source).Clone();

            Assert.Equal(source.Name, clone.Name);
            Assert.Null(clone.Version);
        }

        [Fact]
        public void GetProductLength_DifferentValidScenarios_AllReturnNonZero()
        {
            ProductHeaderValue result = null;

            CallGetProductLength(" custom", 1, 6, out result);
            Assert.Equal("custom", result.Name);
            Assert.Null(result.Version);

            CallGetProductLength(" custom, ", 1, 6, out result);
            Assert.Equal("custom", result.Name);
            Assert.Null(result.Version);

            CallGetProductLength(" custom / 5.6 ", 1, 13, out result);
            Assert.Equal("custom", result.Name);
            Assert.Equal("5.6", result.Version);

            CallGetProductLength("RTA / x58 ,", 0, 10, out result);
            Assert.Equal("RTA", result.Name);
            Assert.Equal("x58", result.Version);

            CallGetProductLength("RTA / x58", 0, 9, out result);
            Assert.Equal("RTA", result.Name);
            Assert.Equal("x58", result.Version);

            CallGetProductLength("RTA / x58 XXX", 0, 10, out result);
            Assert.Equal("RTA", result.Name);
            Assert.Equal("x58", result.Version);
        }

        [Fact]
        public void GetProductLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetProductLength(" custom", 0); // no leading whitespace allowed
            CheckInvalidGetProductLength("custom/", 0);
            CheckInvalidGetProductLength("custom/[", 0);
            CheckInvalidGetProductLength("=", 0);

            CheckInvalidGetProductLength("", 0);
            CheckInvalidGetProductLength(null, 0);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParse(" y/1 ", new ProductHeaderValue("y", "1"));
            CheckValidParse(" custom / 1.0 ", new ProductHeaderValue("custom", "1.0"));
            CheckValidParse("custom / 1.0 ", new ProductHeaderValue("custom", "1.0"));
            CheckValidParse("custom / 1.0", new ProductHeaderValue("custom", "1.0"));
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("product/version="); // only delimiter ',' allowed after last product
            CheckInvalidParse("product otherproduct");
            CheckInvalidParse("product[");
            CheckInvalidParse("=");

            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
            CheckInvalidParse("  ");
            CheckInvalidParse("  ,,");
        }

        #region Helper methods

        private void CheckValidParse(string input, ProductHeaderValue expectedResult)
        {
            ProductHeaderValue result = ProductHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            Assert.True(ProductHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { ProductHeaderValue.Parse(input); });

            Assert.False(ProductHeaderValue.TryParse(input, out ProductHeaderValue result));
            Assert.Null(result);
        }

        private static void CallGetProductLength(string input, int startIndex, int expectedLength,
            out ProductHeaderValue result)
        {
            Assert.Equal(expectedLength, ProductHeaderValue.GetProductLength(input, startIndex, out result));
        }

        private static void CheckInvalidGetProductLength(string input, int startIndex)
        {
            ProductHeaderValue result = null;
            Assert.Equal(0, ProductHeaderValue.GetProductLength(input, startIndex, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
