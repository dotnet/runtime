// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class DataTypeAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        private static readonly DataType[] s_dataTypes = (DataType[])Enum.GetValues(typeof(DataType));
        public static IEnumerable<object[]> DataTypes_TestData => s_dataTypes.Select(type => new object[] { type });

        [Theory]
        [MemberData(nameof(DataTypes_TestData))]
        [InlineData((DataType)(-1))]
        [InlineData(DataType.Upload + 1)]
        public static void Ctor_DataType(DataType dataType)
        {
            DataTypeAttribute attribute = new DataTypeAttribute(dataType);
            Assert.Equal(dataType, attribute.DataType);
            Assert.Null(attribute.CustomDataType);

            bool expectedNull = dataType != DataType.Date && dataType != DataType.Time && dataType != DataType.Currency;
            Assert.Equal(expectedNull, attribute.DisplayFormat == null);
        }

        [Theory]
        [MemberData(nameof(DataTypes_TestData))]
        public static void GetDataTypeName_ReturnsExpectedName(DataType dataType)
        {
            if (dataType != DataType.Custom)
            {
                DataTypeAttribute attribute = new DataTypeAttribute(dataType);
                Assert.Equal(Enum.GetName(typeof(DataType), dataType), attribute.GetDataTypeName());
            }
        }

        [Theory]
        [InlineData((DataType)(-1))]
        [InlineData(DataType.Upload + 1)]
        public static void GetDataTypeName_InvalidDataType_ThrowsIndexOutOfRangeException(DataType dataType)
        {
            DataTypeAttribute attribute = new DataTypeAttribute(dataType);
            Assert.Throws<IndexOutOfRangeException>(() => attribute.GetDataTypeName());
        }

        [Theory]
        [InlineData((DataType)(-1))]
        [InlineData(DataType.Upload + 1)]
        public static void Validate_InvalidDataType_DoesNotThrow(DataType dataType)
        {
            DataTypeAttribute attribute = new DataTypeAttribute(dataType);
            attribute.Validate(new object(), s_testValidationContext);
        }

        [Theory]
        [MemberData(nameof(DataTypes_TestData))]
        public static void Validate_DoesNotThrow(DataType dataType)
        {
            if (dataType != DataType.Custom)
            {
                DataTypeAttribute attribute = new DataTypeAttribute(dataType);
                attribute.Validate(new object(), s_testValidationContext);
            }
        }

        [Theory]
        [InlineData("CustomValue")]
        [InlineData("")]
        [InlineData(null)]
        public static void Ctor_String(string customDataType)
        {
            DataTypeAttribute attribute = new DataTypeAttribute(customDataType);
            Assert.Equal(DataType.Custom, attribute.DataType);
            Assert.Equal(customDataType, attribute.CustomDataType);

            if (string.IsNullOrEmpty(customDataType))
            {
                Assert.Throws<InvalidOperationException>(() => attribute.GetDataTypeName());
                Assert.Throws<InvalidOperationException>(() => attribute.Validate(new object(), s_testValidationContext));
            }
            else
            {
                Assert.Equal(customDataType, attribute.GetDataTypeName());
                attribute.Validate(new object(), s_testValidationContext);
            }
        }

        [Theory]
        [InlineData(DataType.Date, "{0:d}", true)]
        [InlineData(DataType.Time, "{0:t}", true)]
        [InlineData(DataType.Currency, "{0:C}", false)]
        public static void DisplayFormat_ReturnsExpected(DataType dataType, string dataFormatString, bool applyFormatInEditMode)
        {
            DataTypeAttribute attribute = new DataTypeAttribute(dataType);
            Assert.Equal(dataFormatString, attribute.DisplayFormat.DataFormatString);
            Assert.Equal(applyFormatInEditMode, attribute.DisplayFormat.ApplyFormatInEditMode);
        }
    }
}
