// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

#if NETFRAMEWORK || NETSTANDARD2_0
using System.Runtime.Serialization;
#else
using System.Runtime.CompilerServices;
#endif

namespace Microsoft.Extensions.Internal
{
    internal class ParameterDefaultValue
    {
        public static bool TryGetDefaultValue(ParameterInfo parameter, out object? defaultValue)
        {
            bool hasDefaultValue;
            bool tryToGetDefaultValue = true;
            defaultValue = null;

            try
            {
                hasDefaultValue = parameter.HasDefaultValue;
            }
            catch (FormatException) when (parameter.ParameterType == typeof(DateTime))
            {
                // Workaround for https://github.com/dotnet/runtime/issues/18844
                // If HasDefaultValue throws FormatException for DateTime
                // we expect it to have default value
                hasDefaultValue = true;
                tryToGetDefaultValue = false;
            }

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

                [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
                    Justification = "CreateValueType is only called on a ValueType. You can always create an instance of a ValueType.")]
                static object? CreateValueType(Type t) =>
#if NETFRAMEWORK || NETSTANDARD2_0
                    FormatterServices.GetUninitializedObject(t);
#else
                    RuntimeHelpers.GetUninitializedObject(t);
#endif

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
