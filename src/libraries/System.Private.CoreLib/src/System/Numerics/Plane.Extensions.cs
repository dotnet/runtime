// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Numerics
{
    public static partial class Vector
    {
        /// <summary>Reinterprets a <see cref="Plane" /> as a new <see cref="Vector4" />.</summary>
        /// <param name="value">The plane to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector4" />.</returns>
        [Intrinsic]
        public static Vector4 AsVector4(this Plane value) => Unsafe.BitCast<Plane, Vector4>(value);
    }
}
