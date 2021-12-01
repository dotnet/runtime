// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    }
}
