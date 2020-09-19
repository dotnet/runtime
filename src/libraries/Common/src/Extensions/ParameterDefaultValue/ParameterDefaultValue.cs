// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Reflection;

namespace Microsoft.Extensions.Internal
{
    internal class ParameterDefaultValue
    {
        public static bool TryGetDefaultValue(ParameterInfo parameter, out object? defaultValue)
        {
            defaultValue = null;

            if (!parameter.HasDefaultValue)
            {
                return false;
            }

            defaultValue = parameter.DefaultValue;

            // Workaround for https://github.com/dotnet/runtime/issues/18599
            if (defaultValue == null && parameter.ParameterType.IsValueType)
            {
                defaultValue = Activator.CreateInstance(parameter.ParameterType);
            }

            // Handle nullable enums
            if (defaultValue != null &&
                parameter.ParameterType.IsGenericType &&
                parameter.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                Type? underlyingType = Nullable.GetUnderlyingType(parameter.ParameterType);
                if (underlyingType != null && underlyingType.IsEnum)
                {
                    defaultValue = Enum.ToObject(underlyingType, defaultValue);
                }
            }

            return true;
        }
    }
}
