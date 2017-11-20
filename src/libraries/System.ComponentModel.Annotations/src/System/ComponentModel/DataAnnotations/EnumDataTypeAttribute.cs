// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace System.ComponentModel.DataAnnotations
{
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Parameter,
        AllowMultiple = false)]
    public sealed class EnumDataTypeAttribute : DataTypeAttribute
    {
        public EnumDataTypeAttribute(Type enumType)
            : base("Enumeration")
        {
            EnumType = enumType;
        }

        public Type EnumType { get; }

        public override bool IsValid(object value)
        {
            if (EnumType == null)
            {
                throw new InvalidOperationException(SR.EnumDataTypeAttribute_TypeCannotBeNull);
            }
            if (!EnumType.IsEnum)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    SR.EnumDataTypeAttribute_TypeNeedsToBeAnEnum, EnumType.FullName));
            }

            if (value == null)
            {
                return true;
            }
            var stringValue = value as string;
            if (stringValue?.Length == 0)
            {
                return true;
            }

            Type valueType = value.GetType();
            if (valueType.IsEnum && EnumType != valueType)
            {
                // don't match a different enum that might map to the same underlying integer
                return false;
            }

            if (!valueType.IsValueType && valueType != typeof(string))
            {
                // non-value types cannot be converted
                return false;
            }

            if (valueType == typeof(bool) ||
                valueType == typeof(float) ||
                valueType == typeof(double) ||
                valueType == typeof(decimal) ||
                valueType == typeof(char))
            {
                // non-integral types cannot be converted
                return false;
            }

            object convertedValue;
            if (valueType.IsEnum)
            {
                Debug.Assert(valueType == value.GetType(), "The valueType should equal the Type of the value");
                convertedValue = value;
            }
            else
            {
                try
                {
                    convertedValue = stringValue != null
                        ? Enum.Parse(EnumType, stringValue, false)
                        : Enum.ToObject(EnumType, value);
                }
                catch (ArgumentException)
                {
                    // REVIEW: is there a better way to detect this
                    return false;
                }
            }

            if (IsEnumTypeInFlagsMode(EnumType))
            {
                // REVIEW: this seems to be the easiest way to ensure that the value is a valid flag combination
                // If it is, the string representation of the enum value will be something like "A, B", while
                // the string representation of the underlying value will be "3". If the enum value does not
                // match a valid flag combination, then it would also be something like "3".
                string underlying = GetUnderlyingTypeValueString(EnumType, convertedValue);
                string converted = convertedValue.ToString();
                return !underlying.Equals(converted);
            }

            return Enum.IsDefined(EnumType, convertedValue);
        }

        private static bool IsEnumTypeInFlagsMode(Type enumType) =>
            enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Any();

        private static string GetUnderlyingTypeValueString(Type enumType, object enumValue) =>
            Convert.ChangeType(enumValue, Enum.GetUnderlyingType(enumType), CultureInfo.InvariantCulture).ToString();
    }
}
