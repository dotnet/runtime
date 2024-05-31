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
        /// Creates a new span over a portion of a regular managed object. This can be useful
        /// if part of a managed object represents a "fixed array." This is dangerous because the
        /// <paramref name="length"/> is not checked.
        /// </summary>
        /// <param name="reference">A reference to data.</param>
        /// <param name="length">The number of <typeparamref name="T"/> elements the memory contains.</param>
        /// <returns>A span representing the specified reference and length.</returns>
        /// <remarks>
        /// This method should be used with caution. It is dangerous because the length argument is not checked.
        /// Even though the ref is annotated as scoped, it will be stored into the returned span, and the lifetime
        /// of the returned span will not be validated for safety, even by span-aware languages.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Span<T> CreateSpan<T>(scoped ref T reference, int length) =>
            new Span<T>(ref Unsafe.AsRef(in reference), length);

        /// <summary>
        /// Creates a new read-only span over a portion of a regular managed object. This can be useful
        /// if part of a managed object represents a "fixed array." This is dangerous because the
        /// <paramref name="length"/> is not checked.
        /// </summary>
        /// <param name="reference">A reference to data.</param>
        /// <param name="length">The number of <typeparamref name="T"/> elements the memory contains.</param>
        /// <returns>A read-only span representing the specified reference and length.</returns>
        /// <remarks>
        /// This method should be used with caution. It is dangerous because the length argument is not checked.
        /// Even though the ref is annotated as scoped, it will be stored into the returned span, and the lifetime
        /// of the returned span will not be validated for safety, even by span-aware languages.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<T> CreateReadOnlySpan<T>(scoped ref readonly T reference, int length) =>
            new ReadOnlySpan<T>(ref Unsafe.AsRef(in reference), length);

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
        [Intrinsic]
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
