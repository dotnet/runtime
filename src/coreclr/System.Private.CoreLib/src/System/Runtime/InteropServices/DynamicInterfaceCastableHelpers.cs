// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Helpers that allows VM to call into IDynamicInterfaceCastable methods without having to deal with RuntimeTypeHandle.
    /// RuntimeTypeHandle is a struct and is always passed in stack in x86, which our VM call helpers don't
    /// particularly like.
    /// </summary>
    internal static class DynamicInterfaceCastableHelpers
    {
        [Diagnostics.StackTraceHidden]
        internal static bool IsInterfaceImplemented(IDynamicInterfaceCastable castable, RuntimeType interfaceType, bool throwIfNotImplemented)
        {
            bool isImplemented= castable.IsInterfaceImplemented(new RuntimeTypeHandle(interfaceType), throwIfNotImplemented);
            if (!isImplemented && throwIfNotImplemented)
                throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, castable.GetType(), interfaceType));

            return isImplemented;
        }

        [Diagnostics.StackTraceHidden]
        internal static RuntimeType? GetInterfaceImplementation(IDynamicInterfaceCastable castable, RuntimeType interfaceType)
        {
            RuntimeTypeHandle handle = castable.GetInterfaceImplementation(new RuntimeTypeHandle(interfaceType));
            if (handle.Equals(default))
                throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, castable.GetType(), interfaceType));

            RuntimeType implType = handle.GetRuntimeType();
            if (!implType.IsInterface)
                throw new InvalidOperationException(SR.Format(SR.IDynamicInterfaceCastable_NotInterface, implType.ToString()));

            if (!implType.IsDefined(typeof(DynamicInterfaceCastableImplementationAttribute), inherit: false))
                throw new InvalidOperationException(SR.Format(SR.IDynamicInterfaceCastable_MissingImplementationAttribute, implType, nameof(DynamicInterfaceCastableImplementationAttribute)));

            if (!implType.IsAssignableTo(interfaceType))
                throw new InvalidOperationException(SR.Format(SR.IDynamicInterfaceCastable_DoesNotImplementRequested, implType, interfaceType));

            return implType;
        }
    }
}
