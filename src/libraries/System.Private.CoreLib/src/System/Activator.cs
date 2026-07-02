// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;

namespace System
{
    /// <summary>
    /// Activator contains the Activation (CreateInstance/New) methods for late bound support.
    /// </summary>
    public static partial class Activator
    {
        private const BindingFlags ConstructorDefault = BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

        //
        // Note: CreateInstance returns null for Nullable<T>, e.g. CreateInstance(typeof(int?)) returns null.
        //

        [DebuggerHidden]
        [DebuggerStepThrough]
        public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture) =>
            CreateInstance(type, bindingAttr, binder, args, culture, null);

        [DebuggerHidden]
        [DebuggerStepThrough]
        public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, params object?[]? args) =>
            CreateInstance(type, ConstructorDefault, null, args, null, null);

        [DebuggerHidden]
        [DebuggerStepThrough]
        public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, object?[]? args, object?[]? activationAttributes) =>
            CreateInstance(type, ConstructorDefault, null, args, null, activationAttributes);

        [Intrinsic]
        [DebuggerHidden]
        [DebuggerStepThrough]
        public static object? CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type) =>
            CreateInstance(type, nonPublic: false);

        [RequiresUnreferencedCode("Type and its constructor could be removed")]
        public static ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName) =>
            CreateInstanceFrom(assemblyFile, typeName, false, ConstructorDefault, null, null, null, null);

        [RequiresUnreferencedCode("Type and its constructor could be removed")]
        public static ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName, object?[]? activationAttributes) =>
            CreateInstanceFrom(assemblyFile, typeName, false, ConstructorDefault, null, null, null, activationAttributes);

        [RequiresUnreferencedCode("Type and its constructor could be removed")]
        public static ObjectHandle? CreateInstanceFrom(string assemblyFile, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
        {
            Assembly assembly = Assembly.LoadFrom(assemblyFile);
            Type t = assembly.GetType(typeName, throwOnError: true, ignoreCase)!;

            object? o = CreateInstance(t, bindingAttr, binder, args, culture, activationAttributes);

            return o != null ? new ObjectHandle(o) : null;
        }

#if !MONO
        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        private static unsafe void CallConstructorStruct(void* constructor, ref byte instance)
        {
            try
            {
#if NATIVEAOT
                RawCalliHelper.CallDefaultStructConstructor((nint)constructor, ref instance);
#else
                InstanceCalliHelper.Call((delegate*<ref byte, void>)constructor, ref instance);
#endif
            }
            catch (Exception exception)
            {
                throw new TargetInvocationException(exception);
            }
        }

        [DebuggerHidden]
        [DebuggerStepThrough]
        [StackTraceHidden]
        private static unsafe void CallConstructor(void* constructor, object instance)
        {
            try
            {
#if NATIVEAOT
                RawCalliHelper.Call((nint)constructor, instance);
#else
                InstanceCalliHelper.Call((delegate*<object, void>)constructor, instance);
#endif
            }
            catch (Exception exception)
            {
                throw new TargetInvocationException(exception);
            }
        }
#endif
    }
}
