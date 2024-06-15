// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    public static unsafe partial class Vector
    {
        /// <summary>Reinterprets a <see cref="Quaternion" /> as a new <see cref="Vector4" />.</summary>
        /// <param name="value">The quaternion to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Quaternion" />.</returns>
        [Intrinsic]
        public static Vector4 AsVector4(this Quaternion value)
        {
#if MONO
            return Unsafe.As<Quaternion, Vector4>(ref value);
#else
            return Unsafe.BitCast<Quaternion, Vector4>(value);
#endif
        }
    }
}
