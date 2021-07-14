// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Reflection
{
    internal static partial class ReflectionExtensions
    {
        public static bool IsVirtual(this PropertyInfo? propertyInfo)
        {
            Debug.Assert(propertyInfo != null);
            return propertyInfo != null && (propertyInfo.GetMethod?.IsVirtual == true || propertyInfo.SetMethod?.IsVirtual == true);
        }
    }
}
