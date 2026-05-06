// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Microsoft.Extensions.Internal
{
    internal static partial class ParameterDefaultValue
    {
        public static bool CheckHasDefaultValue(ParameterInfo parameter, out bool tryToGetDefaultValue)
        {
            tryToGetDefaultValue = true;
            try
            {
                return parameter.HasDefaultValue;
            }
            catch (FormatException) when (parameter.ParameterType == typeof(DateTime))
            {
                // Workaround for https://github.com/dotnet/runtime/issues/18844
                // If HasDefaultValue throws FormatException for DateTime
                // we expect it to have default value
                tryToGetDefaultValue = false;
                return true;
            }
        }
    }
}
