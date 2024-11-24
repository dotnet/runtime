// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    public static unsafe partial class Vector
    {
        /// <summary>Reinterprets a <see cref="Vector2" /> to a new <see cref="Vector4" /> with the new elements zeroed.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted to a new <see cref="Vector4" /> with the new elements zeroed.</returns>
        [Intrinsic]
        public static Vector4 AsVector4(this Vector2 value) => value.AsVector128().AsVector4();

        /// <summary>Reinterprets a <see cref="Vector2" /> to a new <see cref="Vector4" /> with the new elements undefined.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted to a new <see cref="Vector4" /> with the new elements undefined.</returns>
        [Intrinsic]
        public static Vector4 AsVector4Unsafe(this Vector2 value) => value.AsVector128Unsafe().AsVector4();
    }
}
