// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Numerics
{
    public static unsafe partial class Vector
    {
        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Plane" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Plane" />.</returns>
        [Intrinsic]
        public static Plane AsPlane(this Vector4 value)
        {
#if MONO
            return Unsafe.As<Vector4, Plane>(ref value);
#else
            return Unsafe.BitCast<Vector4, Plane>(value);
#endif
        }

        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Quaternion" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Quaternion" />.</returns>
        [Intrinsic]
        public static Quaternion AsQuaternion(this Vector4 value)
        {
#if MONO
            return Unsafe.As<Vector4, Quaternion>(ref value);
#else
            return Unsafe.BitCast<Vector4, Quaternion>(value);
#endif
        }

        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Vector2" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector2" />.</returns>
        [Intrinsic]
        public static Vector2 AsVector2(this Vector4 value) => value.AsVector128().AsVector2();

        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Vector3" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector3" />.</returns>
        [Intrinsic]
        public static Vector3 AsVector3(this Vector4 value) => value.AsVector128().AsVector3();
    }
}
