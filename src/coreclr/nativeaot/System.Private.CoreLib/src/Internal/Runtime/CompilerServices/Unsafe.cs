// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Runtime.CompilerServices
{
    [ReflectionBlocked]
    public static partial class Unsafe
    {
        /// <summary>
        /// Reads a value of type <typeparamref name="T"/> from the given location.
        /// </summary>
        [Intrinsic]
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Read<T>(ref byte source)
        {
            return Unsafe.As<byte, T>(ref source);
        }

        /// <summary>
        /// Writes a value of type <typeparamref name="T"/> to the given location.
        /// </summary>
        [Intrinsic]
        [NonVersionable]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write<T>(ref byte destination, T value)
        {
            Unsafe.As<byte, T>(ref destination) = value;
        }
    }
}
