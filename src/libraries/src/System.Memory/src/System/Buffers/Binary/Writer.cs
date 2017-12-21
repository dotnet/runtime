// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

namespace System.Buffers.Binary
{
    public static partial class BinaryPrimitives
    {
        /// <summary>
        /// Writes a structure of type T into a span of bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteMachineEndian<T>(Span<byte> buffer, ref T value)
            where T : struct
        {
#if netstandard
            if (SpanHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
            }
#else
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
            }
#endif
            if ((uint)Unsafe.SizeOf<T>() > (uint)buffer.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length);
            }
            Unsafe.WriteUnaligned<T>(ref MemoryMarshal.GetReference(buffer), value);
        }

        /// <summary>
        /// Writes a structure of type T into a span of bytes.
        /// <returns>If the span is too small to contain the type T, return false.</returns>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteMachineEndian<T>(Span<byte> buffer, ref T value)
            where T : struct
        {
#if netstandard
            if (SpanHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
            }
#else
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                ThrowHelper.ThrowArgumentException_InvalidTypeWithPointersNotSupported(typeof(T));
            }
#endif
            if (Unsafe.SizeOf<T>() > (uint)buffer.Length)
            {
                return false;
            }
            Unsafe.WriteUnaligned<T>(ref MemoryMarshal.GetReference(buffer), value);
            return true;
        }
    }
}

