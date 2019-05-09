// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    
    /// <summary>
    /// Helpers that allows VM to call into ICastable methods without having to deal with RuntimeTypeHandle.
    /// RuntimeTypeHandle is a struct and is always passed in stack in x86, which our VM call helpers don't
    /// particularly like.
    /// </summary>
    internal class ICastableHelpers
    {
        internal static bool IsInstanceOfInterface(ICastable castable, RuntimeType type, out Exception? castError) // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
        {
            return castable.IsInstanceOfInterface(new RuntimeTypeHandle(type), out castError);
        }

        internal static RuntimeType GetImplType(ICastable castable, RuntimeType interfaceType)
        {
            return castable.GetImplType(new RuntimeTypeHandle(interfaceType)).GetRuntimeType();
        }
    }
}
