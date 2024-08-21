// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeHelpers
    {
        // The special dll name to be used for DllImport of QCalls
#if NATIVEAOT
        internal const string QCall = "*";
#else
        internal const string QCall = "QCall";
#endif

        public delegate void TryCode(object? userData);

        public delegate void CleanupCode(object? userData, bool exceptionThrown);

        /// <summary>
        /// Slices the specified array using the specified range.
        /// </summary>
        public static T[] GetSubArray<T>(T[] array, Range range)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            (int offset, int length) = range.GetOffsetAndLength(array.Length);

            T[] dest;

            if (typeof(T[]) == array.GetType())
            {
                // We know the type of the array to be exactly T[].

                if (length == 0)
                {
                    return Array.Empty<T>();
                }

                dest = new T[length];
            }
            else
            {
                // The array is actually a U[] where U:T. We'll make sure to create
                // an array of the exact same backing type. The cast to T[] will
                // never fail.

                dest = Unsafe.As<T[]>(Array.CreateInstanceFromArrayType(array.GetType(), length));
            }

            // In either case, the newly-allocated array is the exact same type as the
            // original incoming array. It's safe for us to SpanHelpers.Memmove the contents
            // from the source array to the destination array, otherwise the contents
            // wouldn't have been valid for the source array in the first place.

            Buffer.Memmove(
                ref MemoryMarshal.GetArrayDataReference(dest),
                ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), offset),
                (uint)length);

            return dest;
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void ExecuteCodeWithGuaranteedCleanup(TryCode code, CleanupCode backoutCode, object? userData)
        {
            ArgumentNullException.ThrowIfNull(code);
            ArgumentNullException.ThrowIfNull(backoutCode);

            bool exceptionThrown = true;

            try
            {
                code(userData);
                exceptionThrown = false;
            }
            finally
            {
                backoutCode(userData, exceptionThrown);
            }
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void PrepareContractedDelegate(Delegate d)
        {
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void ProbeForSufficientStack()
        {
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void PrepareConstrainedRegions()
        {
        }

        [Obsolete(Obsoletions.ConstrainedExecutionRegionMessage, DiagnosticId = Obsoletions.ConstrainedExecutionRegionDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void PrepareConstrainedRegionsNoOP()
        {
        }

        internal static bool IsPrimitiveType(this CorElementType et)
            // COR_ELEMENT_TYPE_I1,I2,I4,I8,U1,U2,U4,U8,R4,R8,I,U,CHAR,BOOLEAN
            => ((1 << (int)et) & 0b_0011_0000_0000_0011_1111_1111_1100) != 0;

        private static ReadOnlySpan<short> PrimitiveWidenTable =>
            [
                0x00,      // ELEMENT_TYPE_END
                0x00,      // ELEMENT_TYPE_VOID
                0x0004,    // ELEMENT_TYPE_BOOLEAN
                0x3F88,    // ELEMENT_TYPE_CHAR (W = U2, CHAR, I4, U4, I8, U8, R4, R8) (U2 == Char)
                0x3550,    // ELEMENT_TYPE_I1   (W = I1, I2, I4, I8, R4, R8)
                0x3FE8,    // ELEMENT_TYPE_U1   (W = CHAR, U1, I2, U2, I4, U4, I8, U8, R4, R8)
                0x3540,    // ELEMENT_TYPE_I2   (W = I2, I4, I8, R4, R8)
                0x3F88,    // ELEMENT_TYPE_U2   (W = U2, CHAR, I4, U4, I8, U8, R4, R8)
                0x3500,    // ELEMENT_TYPE_I4   (W = I4, I8, R4, R8)
                0x3E00,    // ELEMENT_TYPE_U4   (W = U4, I8, R4, R8)
                0x3400,    // ELEMENT_TYPE_I8   (W = I8, R4, R8)
                0x3800,    // ELEMENT_TYPE_U8   (W = U8, R4, R8)
                0x3000,    // ELEMENT_TYPE_R4   (W = R4, R8)
                0x2000,    // ELEMENT_TYPE_R8   (W = R8)
            ];

        internal static bool CanPrimitiveWiden(CorElementType srcET, CorElementType dstET)
        {
            Debug.Assert(srcET.IsPrimitiveType() && dstET.IsPrimitiveType());
            if ((int)srcET >= PrimitiveWidenTable.Length)
            {
                // I or U
                return srcET == dstET;
            }
            return (PrimitiveWidenTable[(int)srcET] & (1 << (int)dstET)) != 0;
        }

        /// <summary>Provide a fast way to access constant data stored in a module as a ReadOnlySpan{T}</summary>
        /// <param name="fldHandle">A field handle that specifies the location of the data to be referred to by the ReadOnlySpan{T}. The Rva of the field must be aligned on a natural boundary of type T</param>
        /// <returns>A ReadOnlySpan{T} of the data stored in the field</returns>
        /// <exception cref="ArgumentException"><paramref name="fldHandle"/> does not refer to a field which is an Rva, is misaligned, or T is of an invalid type.</exception>
        /// <remarks>This method is intended for compiler use rather than use directly in code. T must be one of byte, sbyte, bool, char, short, ushort, int, uint, long, ulong, float, or double.</remarks>
        [Intrinsic]
        public static unsafe ReadOnlySpan<T> CreateSpan<T>(RuntimeFieldHandle fldHandle)
            => new ReadOnlySpan<T>(ref Unsafe.As<byte, T>(ref GetSpanDataFrom(fldHandle, typeof(T).TypeHandle, out int length)), length);


        // The following intrinsics return true if input is a compile-time constant
        // Feel free to add more overloads on demand
#pragma warning disable IDE0060
        [Intrinsic]
        internal static bool IsKnownConstant(Type? t) => false;

        [Intrinsic]
        internal static bool IsKnownConstant(string? t) => false;

        [Intrinsic]
        internal static bool IsKnownConstant(char t) => false;

        [Intrinsic]
        internal static bool IsKnownConstant<T>(T t) where T : struct => false;
#pragma warning restore IDE0060

        /// <returns>true if the given type is a reference type or a value type that contains references or by-refs; otherwise, false.</returns>
        [Intrinsic]
        public static bool IsReferenceOrContainsReferences<T>() where T: allows ref struct => IsReferenceOrContainsReferences<T>();
    }
}
