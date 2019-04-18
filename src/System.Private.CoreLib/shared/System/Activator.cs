// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Remoting;

namespace System
{
    /// <summary>
    /// Activator contains the Activation (CreateInstance/New) methods for late bound support.
    /// </summary>
    public static partial class Activator
    {
        private const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

        [DebuggerHidden]
        [DebuggerStepThrough]
        public static object? CreateInstance(Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture) =>
            CreateInstance(type, bindingAttr, binder, args, culture, null);

        [DebuggerHidden]
        [DebuggerStepThrough]
        public static object? CreateInstance(Type type, params object?[]? args) =>
            CreateInstance(type, ConstructorDefault, null, args, null, null);

        [DebuggerHidden]
        [DebuggerStepThrough]
        public static object? CreateInstance(Type type, object?[]? args, object?[]? activationAttributes) =>
            CreateInstance(type, ConstructorDefault, null, args, null, activationAttributes);

        [DebuggerHidden]
        [DebuggerStepThrough]
        public static object? CreateInstance(Type type) =>
            CreateInstance(type, nonPublic: false);

        public static ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName) =>
            CreateInstanceFrom(assemblyFile, typeName, false, ConstructorDefault, null, null, null, null);

        public static ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName, object?[]? activationAttributes) =>
            CreateInstanceFrom(assemblyFile, typeName, false, ConstructorDefault, null, null, null, activationAttributes);

        public static ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
        {
            Assembly assembly = Assembly.LoadFrom(assemblyFile);
            Type t = assembly.GetType(typeName, true, ignoreCase);

            object? o = CreateInstance(t, bindingAttr, binder, args, culture, activationAttributes);

            return o != null ? new ObjectHandle(o) : null;
        }
    }
}
