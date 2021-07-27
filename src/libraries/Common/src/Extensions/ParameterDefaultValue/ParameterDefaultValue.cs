// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Microsoft.Extensions.Internal
{
    internal static partial class ParameterDefaultValue
    {
        public static bool TryGetDefaultValue(ParameterInfo parameter, out object? defaultValue)
        {
            bool hasDefaultValue = CheckHasDefaultValue(parameter);
            bool tryToGetDefaultValue = true;
            defaultValue = null;

            if (hasDefaultValue)
            {
                if (tryToGetDefaultValue)
                {
                    defaultValue = parameter.DefaultValue;
                }

                bool isNullableParameterType = parameter.ParameterType.IsGenericType &&
                    parameter.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>);

                // Workaround for https://github.com/dotnet/runtime/issues/18599
                if (defaultValue == null && parameter.ParameterType.IsValueType
                    && !isNullableParameterType) // Nullable types should be left null
                {
                    defaultValue = CreateValueType(parameter.ParameterType);
                }

                // Handle nullable enums
                if (defaultValue != null && isNullableParameterType)
                {
                    Type? underlyingType = Nullable.GetUnderlyingType(parameter.ParameterType);
                    if (underlyingType != null && underlyingType.IsEnum)
                    {
                        defaultValue = Enum.ToObject(underlyingType, defaultValue);
                    }
                }
            }

            return hasDefaultValue;
        }
    }
}
