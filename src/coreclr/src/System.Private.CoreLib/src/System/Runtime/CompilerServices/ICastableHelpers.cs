// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Helpers that allows VM to call into ICastable methods without having to deal with RuntimeTypeHandle.
    /// RuntimeTypeHandle is a struct and is always passed in stack in x86, which our VM call helpers don't
    /// particularly like.
    /// </summary>
    internal static class ICastableHelpers
    {
        internal static bool IsInstanceOfInterface(ICastable castable, RuntimeType type, [NotNullWhen(true)] out Exception? castError)
        {
            return castable.IsInstanceOfInterface(new RuntimeTypeHandle(type), out castError);
        }

        internal static RuntimeType GetImplType(ICastable castable, RuntimeType interfaceType)
        {
            return castable.GetImplType(new RuntimeTypeHandle(interfaceType)).GetRuntimeType();
        }

        [Diagnostics.StackTraceHidden]
        internal static RuntimeType? GetInterfaceImplementation(ICastableObject castableObject, RuntimeType interfaceType, bool throwIfNotFound)
        {
            RuntimeTypeHandle handle = castableObject.GetInterfaceImplementation(new RuntimeTypeHandle(interfaceType), throwIfNotFound);
            if (handle.Equals(default))
            {
                if (throwIfNotFound)
                    throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, castableObject.GetType(), interfaceType));

                return null;
            }

            RuntimeType implType = handle.GetRuntimeType();
            if (!implType.IsInterface)
                throw new InvalidOperationException(SR.Format(SR.ICastableObject_NotInterface, implType.ToString()));

            if (!implType.IsDefined(typeof(CastableObjectImplementationAttribute), inherit: false))
                throw new InvalidOperationException(SR.Format(SR.ICastableObject_MissingImplementationAttribute, implType, nameof(CastableObjectImplementationAttribute)));

            if (!implType.ImplementInterface(interfaceType))
                throw new InvalidOperationException(SR.Format(SR.ICastableObject_DoesNotImplementRequested, implType, interfaceType));

            return implType;
        }
    }
}
