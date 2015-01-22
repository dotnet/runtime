// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    public class DataTypeAttributeTests
    {
        private static readonly ValidationContext s_testValidationContext = new ValidationContext(new object());

        [Fact]
        public static void DataType_and_CustomDataType_assigned_correctly_for_all_non_custom_DataTypes()
        {
            foreach (var enumValue in Enum.GetValues(typeof(DataType)))
            {
                var dataType = (DataType)enumValue;
                if (DataType.Custom != dataType)
                {
                    var attribute = new DataTypeAttribute(dataType);
                    Assert.Equal(dataType, attribute.DataType);
                    Assert.Null(attribute.CustomDataType);
                }
            }
        }

        [Fact]
        public static void GetDataTypeName_and_Validate_successful_for_all_non_custom_DataTypes()
        {
            foreach (var enumValue in Enum.GetValues(typeof(DataType)))
            {
                var dataType = (DataType)enumValue;
                if (DataType.Custom != dataType)
                {
                    var attribute = new DataTypeAttribute(dataType);
                    Assert.Equal(Enum.GetName(typeof(DataType), enumValue), attribute.GetDataTypeName());
                    AssertEx.DoesNotThrow(() => attribute.Validate(new object(), s_testValidationContext));
                }
            }
        }

        [Fact]
        public static void DataType_and_CustomDataType_assigned_correctly_for_custom_DataType()
        {
            var attribute = new DataTypeAttribute("CustomValue");
            Assert.Equal(DataType.Custom, attribute.DataType);
            Assert.Equal("CustomValue", attribute.CustomDataType);
        }

        [Fact]
        public static void GetDataTypeName_and_IsValid_on_null_custom_DataTypeAttribute_throws_exception()
        {
            var attribute = new DataTypeAttribute((string)null);
            Assert.Equal(DataType.Custom, attribute.DataType); // Only throw when call GetDataTypeName() or Validate()
            Assert.Null(attribute.CustomDataType); // Only throw when call GetDataTypeName() or Validate()
            Assert.Throws<InvalidOperationException>(() => attribute.GetDataTypeName());
            Assert.Throws<InvalidOperationException>(() => attribute.Validate(new object(), s_testValidationContext));
        }

        [Fact]
        public static void GetDataTypeName_and_IsValid_on_empty_custom_DataTypeAttribute_throws_exception()
        {
            var attribute = new DataTypeAttribute(string.Empty);
            Assert.Equal(DataType.Custom, attribute.DataType); // Only throw when call GetDataTypeName() or Validate()
            AssertEx.Empty(attribute.CustomDataType); // Only throw when call GetDataTypeName() or Validate()
            Assert.Throws<InvalidOperationException>(() => attribute.GetDataTypeName());
            Assert.Throws<InvalidOperationException>(() => attribute.Validate(new object(), s_testValidationContext));
        }

        [Fact]
        public static void GetDataTypeName_and_IsValid_on_non_null_custom_DataTypeAttribute_is_successful()
        {
            var attribute = new DataTypeAttribute("TestCustomDataType");
            Assert.Equal("TestCustomDataType", attribute.GetDataTypeName());
            AssertEx.DoesNotThrow(() => attribute.Validate(new object(), s_testValidationContext));
        }

        [Fact]
        public static void DisplayFormat_set_correctly_for_date_time_and_currency()
        {
            var dateAttribute = new DataTypeAttribute(DataType.Date);
            Assert.NotNull(dateAttribute.DisplayFormat);
            Assert.Equal("{0:d}", dateAttribute.DisplayFormat.DataFormatString);
            Assert.True(dateAttribute.DisplayFormat.ApplyFormatInEditMode);

            var timeAttribute = new DataTypeAttribute(DataType.Time);
            Assert.NotNull(timeAttribute.DisplayFormat);
            Assert.Equal("{0:t}", timeAttribute.DisplayFormat.DataFormatString);
            Assert.True(timeAttribute.DisplayFormat.ApplyFormatInEditMode);

            var currencyAttribute = new DataTypeAttribute(DataType.Currency);
            Assert.NotNull(currencyAttribute.DisplayFormat);
            Assert.Equal("{0:C}", currencyAttribute.DisplayFormat.DataFormatString);
            Assert.False(currencyAttribute.DisplayFormat.ApplyFormatInEditMode);
        }

        [Fact]
        public static void DisplayFormat_null_for_non_date_time_and_currency()
        {
            foreach (var enumValue in Enum.GetValues(typeof(DataType)))
            {
                var dataType = (DataType)enumValue;
                if (DataType.Date != dataType
                    && DataType.Time != dataType
                    && DataType.Currency != dataType)
                {
                    var attribute = new DataTypeAttribute(dataType);
                    Assert.Null(attribute.DisplayFormat);
                }
            }
        }
    }
}
