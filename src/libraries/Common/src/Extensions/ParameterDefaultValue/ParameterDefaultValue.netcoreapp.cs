// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Internal
{
    internal static partial class ParameterDefaultValue
    {
        public static bool CheckHasDefaultValue(ParameterInfo parameter)
        {
            return parameter.HasDefaultValue;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "CreateValueType is only called on a ValueType. You can always create an instance of a ValueType.")]
        internal static object? CreateValueType(Type t) =>
            RuntimeHelpers.GetUninitializedObject(t);
    }
}
