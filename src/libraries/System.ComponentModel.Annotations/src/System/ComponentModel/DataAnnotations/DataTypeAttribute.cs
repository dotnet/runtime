// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     Allows for clarification of the <see cref="DataType" /> represented by a given
    ///     property (such as <see cref="System.ComponentModel.DataAnnotations.DataType.PhoneNumber" />
    ///     or <see cref="System.ComponentModel.DataAnnotations.DataType.Url" />)
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public class DataTypeAttribute : ValidationAttribute
    {
        private static readonly string[] _dataTypeStrings = Enum.GetNames(typeof(DataType));

        /// <summary>
        ///     Constructor that accepts a data type enumeration
        /// </summary>
        /// <param name="dataType">The <see cref="DataType" /> enum value indicating the type to apply.</param>
        public DataTypeAttribute(DataType dataType)
        {
            DataType = dataType;

            // Set some DisplayFormat for a few specific data types
            switch (dataType)
            {
                case DataType.Date:
                    DisplayFormat = new DisplayFormatAttribute();
                    DisplayFormat.DataFormatString = "{0:d}";
                    DisplayFormat.ApplyFormatInEditMode = true;
                    break;
                case DataType.Time:
                    DisplayFormat = new DisplayFormatAttribute();
                    DisplayFormat.DataFormatString = "{0:t}";
                    DisplayFormat.ApplyFormatInEditMode = true;
                    break;
                case DataType.Currency:
                    DisplayFormat = new DisplayFormatAttribute();
                    DisplayFormat.DataFormatString = "{0:C}";

                    // Don't set ApplyFormatInEditMode for currencies because the currency
                    // symbol can't be parsed
                    break;
            }
        }

        /// <summary>
        ///     Constructor that accepts the string name of a custom data type
        /// </summary>
        /// <param name="customDataType">The string name of the custom data type.</param>
        public DataTypeAttribute(string customDataType)
            : this(DataType.Custom)
        {
            CustomDataType = customDataType;
        }

        /// <summary>
        ///     Gets the DataType. If it equals DataType.Custom, <see cref="CustomDataType" /> should also be retrieved.
        /// </summary>
        public DataType DataType { get; }

        /// <summary>
        ///     Gets the string representing a custom data type. Returns a non-null value only if <see cref="DataType" /> is
        ///     DataType.Custom.
        /// </summary>
        public string? CustomDataType { get; }

        /// <summary>
        ///     Gets the default display format that gets used along with this DataType.
        /// </summary>
        public DisplayFormatAttribute? DisplayFormat { get; protected set; }

        /// <summary>
        ///     Return the name of the data type, either using the <see cref="DataType" /> enum or <see cref="CustomDataType" />
        ///     string
        /// </summary>
        /// <returns>The name of the data type enum</returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
        public virtual string GetDataTypeName()
        {
            EnsureValidDataType();

            if (DataType == DataType.Custom)
            {
                // If it's a custom type string, use it as the template name
                return CustomDataType!;
            }
            // If it's an enum, turn it into a string
            // Use the cached array with enum string values instead of ToString() as the latter is too slow
            return _dataTypeStrings[(int)DataType];
        }

        /// <summary>
        ///     Override of <see cref="ValidationAttribute.IsValid(object)" />
        /// </summary>
        /// <remarks>This override always returns <c>true</c>.  Subclasses should override this to provide the correct result.</remarks>
        /// <param name="value">The value to validate</param>
        /// <returns>Unconditionally returns <c>true</c></returns>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
        public override bool IsValid(object? value)
        {
            EnsureValidDataType();

            return true;
        }

        /// <summary>
        ///     Throws an exception if this attribute is not correctly formed
        /// </summary>
        /// <exception cref="InvalidOperationException"> is thrown if the current attribute is ill-formed.</exception>
        private void EnsureValidDataType()
        {
            if (DataType == DataType.Custom && string.IsNullOrWhiteSpace(CustomDataType))
            {
                throw new InvalidOperationException(SR.DataTypeAttribute_EmptyDataTypeString);
            }
        }
    }
}
