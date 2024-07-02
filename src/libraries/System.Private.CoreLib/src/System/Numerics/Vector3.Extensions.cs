// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    public static unsafe partial class Vector
    {
        /// <summary>Converts a <see cref="Vector3" /> to a new <see cref="Vector4" /> with the new elements zeroed.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns><paramref name="value" /> converted to a new <see cref="Vector4" /> with the new elements zeroed.</returns>
        [Intrinsic]
        public static Vector4 AsVector4(this Vector3 value) => value.AsVector128().AsVector4();

        /// <summary>Converts a <see cref="Vector3" /> to a new <see cref="Vector4" /> with the new elements undefined.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns><paramref name="value" /> converted to a new <see cref="Vector4" /> with the new elements undefined.</returns>
        [Intrinsic]
        public static Vector4 AsVector4Unsafe(this Vector3 value) => value.AsVector128Unsafe().AsVector4();
    }
}
