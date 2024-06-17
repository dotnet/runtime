// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    public static unsafe partial class Vector
    {
        /// <summary>Reinterprets a <see cref="Plane" /> as a new <see cref="Vector4" />.</summary>
        /// <param name="value">The plane to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector4" />.</returns>
        [Intrinsic]
        public static Vector4 AsVector4(this Plane value)
        {
#if MONO
            return Unsafe.As<Plane, Vector4>(ref value);
#else
            return Unsafe.BitCast<Plane, Vector4>(value);
#endif
        }
    }
}
