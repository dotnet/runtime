// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel
{
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "type is annotated as preserve All members, so we can call GetConstructor.")]
    internal static class TrimSafeReflectionHelper
    {
        public static ConstructorInfo? GetConstructor(Type type, Type[] types) => type.GetConstructor(types);
        public static PropertyInfo[] GetProperties(Type type, BindingFlags bindingAttr) => type.GetProperties(bindingAttr);
        public static PropertyInfo? GetProperty(Type type, string name, BindingFlags bindingAttr) => type.GetProperty(name, bindingAttr);
        public static EventInfo[] GetEvents(Type type, BindingFlags bindingAttr) => type.GetEvents(bindingAttr);
        public static Type[] GetInterfaces(Type type) => type.GetInterfaces();
    }
}
