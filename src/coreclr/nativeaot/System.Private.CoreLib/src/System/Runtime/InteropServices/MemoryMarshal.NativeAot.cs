// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public static unsafe partial class MemoryMarshal
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
            ref GetArrayDataReference(array);

        /// <summary>
        /// Returns a reference to the 0th element of <paramref name="array"/>. If the array is empty, returns a reference to where the 0th element
        /// would have been stored. Such a reference may be used for pinning but must never be dereferenced.
        /// </summary>
        /// <exception cref="NullReferenceException"><paramref name="array"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// The caller must manually reinterpret the returned <em>ref byte</em> as a ref to the array's underlying elemental type,
        /// perhaps utilizing an API such as <em>System.Runtime.CompilerServices.Unsafe.As</em> to assist with the reinterpretation.
        /// This technique does not perform array variance checks. The caller must manually perform any array variance checks
        /// if the caller wishes to write to the returned reference.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte GetArrayDataReference(Array array)
        {
            // If needed, we can save one or two instructions per call by marking this method as intrinsic and asking the JIT
            // to special-case arrays of known type and dimension.

            // See comment on RawArrayData (in RuntimeHelpers.CoreCLR.cs) for details
            return ref Unsafe.AddByteOffset(ref Unsafe.As<RawData>(array).Data, (nuint)array.GetMethodTable()->BaseSize - (nuint)(2 * sizeof(IntPtr)));
        }
    }
}
