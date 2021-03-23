// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public static partial class MemoryMarshal
    {
        /// <summary>
        /// Returns a reference to the 0th element of <paramref name="array"/>. If the array is empty, returns a reference to where the 0th element
        /// would have been stored. Such a reference may be used for pinning but must never be dereferenced.
        /// </summary>
        /// <exception cref="NullReferenceException"><paramref name="array"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// This method does not perform array variance checks. The caller must manually perform any array variance checks
        /// if the caller wishes to write to the returned reference.
        /// </remarks>
        [Intrinsic]
        [NonVersionable]
        public static ref T GetArrayDataReference<T>(T[] array) =>
            ref Unsafe.As<byte, T>(ref Unsafe.As<RawArrayData>(array).Data);
    }
}
