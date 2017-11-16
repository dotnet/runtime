// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class SpanHelpers
    {
        /// <summary>
        /// Implements the copy functionality used by Span and ReadOnlySpan.
        ///
        /// NOTE: Fast span implements TryCopyTo in corelib and therefore this implementation
        ///       is only used by portable span. The code must live in code that only compiles
        ///       for portable span which means either each individual span implementation
        ///       of this shared code file. Other shared SpanHelper.X.cs files are compiled
        ///       for both portable and fast span implementations.
        /// </summary>
        public static unsafe void CopyTo<T>(ref T dst, int dstLength, ref T src, int srcLength)
        {
            Debug.Assert(dstLength != 0);

            IntPtr srcByteCount = Unsafe.ByteOffset(ref src, ref Unsafe.Add(ref src, srcLength));
            IntPtr dstByteCount = Unsafe.ByteOffset(ref dst, ref Unsafe.Add(ref dst, dstLength));

            IntPtr diff = Unsafe.ByteOffset(ref src, ref dst);

            bool isOverlapped = (sizeof(IntPtr) == sizeof(int))
                ? (uint)diff < (uint)srcByteCount || (uint)diff > (uint)-(int)dstByteCount
                : (ulong)diff < (ulong)srcByteCount || (ulong)diff > (ulong)-(long)dstByteCount;

            if (!isOverlapped && !SpanHelpers.IsReferenceOrContainsReferences<T>())
            {
                ref byte dstBytes = ref Unsafe.As<T, byte>(ref dst);
                ref byte srcBytes = ref Unsafe.As<T, byte>(ref src);
                ulong byteCount = (ulong)srcByteCount;
                ulong index = 0;

                while (index < byteCount)
                {
                    uint blockSize = (byteCount - index) > uint.MaxValue ? uint.MaxValue : (uint)(byteCount - index);
                    Unsafe.CopyBlock(
                        ref Unsafe.Add(ref dstBytes, (IntPtr)index),
                        ref Unsafe.Add(ref srcBytes, (IntPtr)index),
                        blockSize);
                    index += blockSize;
                }
            }
            else
            {
                bool srcGreaterThanDst = (sizeof(IntPtr) == sizeof(int))
                    ? (uint)diff > (uint)-(int)dstByteCount
                    : (ulong)diff > (ulong)-(long)dstByteCount;

                int direction = srcGreaterThanDst ? 1 : -1;
                int runCount = srcGreaterThanDst ? 0 : srcLength - 1;

                int loopCount = 0;
                for (; loopCount < (srcLength & ~7); loopCount += 8)
                {
                    Unsafe.Add<T>(ref dst, runCount + direction * 0) = Unsafe.Add<T>(ref src, runCount + direction * 0);
                    Unsafe.Add<T>(ref dst, runCount + direction * 1) = Unsafe.Add<T>(ref src, runCount + direction * 1);
                    Unsafe.Add<T>(ref dst, runCount + direction * 2) = Unsafe.Add<T>(ref src, runCount + direction * 2);
                    Unsafe.Add<T>(ref dst, runCount + direction * 3) = Unsafe.Add<T>(ref src, runCount + direction * 3);
                    Unsafe.Add<T>(ref dst, runCount + direction * 4) = Unsafe.Add<T>(ref src, runCount + direction * 4);
                    Unsafe.Add<T>(ref dst, runCount + direction * 5) = Unsafe.Add<T>(ref src, runCount + direction * 5);
                    Unsafe.Add<T>(ref dst, runCount + direction * 6) = Unsafe.Add<T>(ref src, runCount + direction * 6);
                    Unsafe.Add<T>(ref dst, runCount + direction * 7) = Unsafe.Add<T>(ref src, runCount + direction * 7);
                    runCount += direction * 8;
                }
                if (loopCount < (srcLength & ~3))
                {
                    Unsafe.Add<T>(ref dst, runCount + direction * 0) = Unsafe.Add<T>(ref src, runCount + direction * 0);
                    Unsafe.Add<T>(ref dst, runCount + direction * 1) = Unsafe.Add<T>(ref src, runCount + direction * 1);
                    Unsafe.Add<T>(ref dst, runCount + direction * 2) = Unsafe.Add<T>(ref src, runCount + direction * 2);
                    Unsafe.Add<T>(ref dst, runCount + direction * 3) = Unsafe.Add<T>(ref src, runCount + direction * 3);
                    runCount += direction * 4;
                    loopCount += 4;
                }
                for (; loopCount < srcLength; ++loopCount)
                {
                    Unsafe.Add<T>(ref dst, runCount) = Unsafe.Add<T>(ref src, runCount);
                    runCount += direction;
                }
            }
        }

        /// <summary>
        /// Computes "start + index * sizeof(T)", using the unsigned IntPtr-sized multiplication for 32 and 64 bits.
        ///
        /// Assumptions:
        ///     Start and index are non-negative, and already pre-validated to be within the valid range of their containing Span.
        ///
        ///     If the byte length (Span.Length * sizeof(T)) does an unsigned overflow (i.e. the buffer wraps or is too big to fit within the address space),
        ///     the behavior is undefined.
        ///
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr Add<T>(this IntPtr start, int index)
        {
            Debug.Assert(start.ToInt64() >= 0);
            Debug.Assert(index >= 0);

            unsafe
            {
                if (sizeof(IntPtr) == sizeof(int))
                {
                    // 32-bit path.
                    uint byteLength = (uint)index * (uint)Unsafe.SizeOf<T>();
                    return (IntPtr)(((byte*)start) + byteLength);
                }
                else
                {
                    // 64-bit path.
                    ulong byteLength = (ulong)index * (ulong)Unsafe.SizeOf<T>();
                    return (IntPtr)(((byte*)start) + byteLength);
                }
            }
        }

        /// <summary>
        /// Determine if a type is eligible for storage in unmanaged memory.
        /// Portable equivalent of RuntimeHelpers.IsReferenceOrContainsReferences{T}()
        /// </summary>
        public static bool IsReferenceOrContainsReferences<T>() => PerTypeValues<T>.IsReferenceOrContainsReferences;

        private static bool IsReferenceOrContainsReferencesCore(Type type)
        {
            if (type.GetTypeInfo().IsPrimitive) // This is hopefully the common case. All types that return true for this are value types w/out embedded references.
                return false;

            if (!type.GetTypeInfo().IsValueType)
                return true;

            // If type is a Nullable<> of something, unwrap it first.
            Type underlyingNullable = Nullable.GetUnderlyingType(type);
            if (underlyingNullable != null)
                type = underlyingNullable;

            if (type.GetTypeInfo().IsEnum)
                return false;

            foreach (FieldInfo field in type.GetTypeInfo().DeclaredFields)
            {
                if (field.IsStatic)
                    continue;
                if (IsReferenceOrContainsReferencesCore(field.FieldType))
                    return true;
            }
            return false;
        }

        public static class PerTypeValues<T>
        {
            //
            // Latch to ensure that excruciatingly expensive validation check for constructing a Span around a raw pointer is done
            // only once per type.
            //
            public static readonly bool IsReferenceOrContainsReferences = IsReferenceOrContainsReferencesCore(typeof(T));

            public static readonly T[] EmptyArray = new T[0];

            public static readonly IntPtr ArrayAdjustment = MeasureArrayAdjustment();

            // Array header sizes are a runtime implementation detail and aren't the same across all runtimes. (The CLR made a tweak after 4.5, and Mono has an extra Bounds pointer.)
            private static IntPtr MeasureArrayAdjustment()
            {
                T[] sampleArray = new T[1];
                return Unsafe.ByteOffset<T>(ref Unsafe.As<Pinnable<T>>(sampleArray).Data, ref sampleArray[0]);
            }
        }
    }
}
