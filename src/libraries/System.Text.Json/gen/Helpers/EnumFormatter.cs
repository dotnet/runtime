// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Text.Json.SourceGeneration
{
    public static class EnumFormatter<TEnum> where TEnum : struct, Enum
    {
        private static readonly bool s_IsFlagsEnum = typeof(TEnum).GetCustomAttribute<FlagsAttribute>() != null;
        private static readonly TEnum[] s_definedValues = (TEnum[])Enum.GetValues(typeof(TEnum));

        public static string FormatEnumLiteral(string enumTypeName, TEnum value)
        {
            if (TryGetEnumComponents(value, out TEnum? singleValue, out List<TEnum>? flagComponents))
            {
                if (singleValue != null)
                {
                    return FormatDefinedValue(value);
                }
                else
                {
                    Debug.Assert(flagComponents?.Count > 1);
                    return string.Join(" | ", flagComponents.Select(FormatDefinedValue));
                }

                string FormatDefinedValue(TEnum value)
                {
                    return $"{enumTypeName}.{value}";
                }
            }

            // Does not correspond to an enum value, format as numeric value.
            int numericValue = GetNumericValue(value);
            return numericValue >= 0
                ? $"({enumTypeName}){numericValue}"
                : $"({enumTypeName})({numericValue})";
        }

        private static bool TryGetEnumComponents(TEnum value, out TEnum? singleValue, out List<TEnum>? flagComponents)
        {
            singleValue = null;
            flagComponents = null;

            if (!s_IsFlagsEnum)
            {
                int idx = Array.IndexOf(s_definedValues, value);
                if (idx < 0)
                {
                    return false;
                }

                singleValue = s_definedValues[idx];
                return true;
            }

            int numericValue = GetNumericValue(value);

            foreach (TEnum definedValue in s_definedValues)
            {
                int definedNumericValue = GetNumericValue(definedValue);
                bool isContainedInValue = definedNumericValue != 0
                    ? (numericValue & definedNumericValue) == definedNumericValue
                    : numericValue == 0;

                if (isContainedInValue)
                {
                    if (singleValue is null && flagComponents is null)
                    {
                        singleValue = definedValue;
                    }
                    else
                    {
                        if (flagComponents is null)
                        {
                            Debug.Assert(singleValue.HasValue);
                            flagComponents = new() { singleValue.Value };
                            singleValue = null;
                        }

                        flagComponents.Add(definedValue);
                    }

                    numericValue &= ~definedNumericValue;
                }
            }

            // The enum contains bits that do not correspond to defined values.
            // Discard accumulated state and return false.
            if (numericValue != 0)
            {
                flagComponents = null;
                singleValue = null;
            }

            return singleValue != null || flagComponents != null;
        }

        private static int GetNumericValue(TEnum value)
        {
            Debug.Assert(Type.GetTypeCode(typeof(TEnum)) is TypeCode.Int32, "only int-backed enums supported for now.");
            return Unsafe.As<TEnum, int>(ref value);
        }
    }
}
