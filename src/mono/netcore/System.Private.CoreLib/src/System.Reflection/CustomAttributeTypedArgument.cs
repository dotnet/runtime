// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    partial struct CustomAttributeTypedArgument
    {
        static object CanonicalizeValue (object value)
        {
            if (value.GetType ().IsEnum)
                return ((Enum) value).GetValue ();

            return value;
        }
    }
}
