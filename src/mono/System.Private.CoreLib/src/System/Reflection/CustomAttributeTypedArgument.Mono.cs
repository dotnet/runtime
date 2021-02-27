// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    public partial struct CustomAttributeTypedArgument
    {
        private static object CanonicalizeValue(object value)
        {
            if (value.GetType().IsEnum)
                return ((Enum)value).GetValue();

            return value;
        }
    }
}
