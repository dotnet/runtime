// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Numerics.Hashing;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Numerics
{
    /* Note: The following patterns are used throughout the code here and are described here
    *
    * PATTERN:
    *    if (typeof(T) == typeof(Int32)) { ... }
    *    else if (typeof(T) == typeof(Single)) { ... }
    * EXPLANATION:
    *    At runtime, each instantiation of Vector<T> will be type-specific, and each of these typeof blocks will be eliminated,
    *    as typeof(T) is a (JIT) compile-time constant for each instantiation. This design was chosen to eliminate any overhead from
    *    delegates and other patterns.
    *
    * PATTERN:
    *    if (Vector.IsHardwareAccelerated) { ... }
    *    else { ... }
    * EXPLANATION
    *    This pattern solves two problems:
    *        1. Allows us to unroll loops when we know the size (when no hardware acceleration is present)
    *        2. Allows reflection to work:
    *            - If a method is called via reflection, it will not be "intrinsified", which would cause issues if we did
    *              not provide an implementation for that case (i.e. if it only included a case which assumed 16-byte registers)
    *    (NOTE: It is assumed that Vector.IsHardwareAccelerated will be a compile-time constant, eliminating these checks
    *        from the JIT'd code.)
    *
    * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

    /// <summary>
    /// A structure that represents a single Vector. The count of this Vector is fixed but CPU register dependent.
    /// This struct only supports numerical types. This type is intended to be used as a building block for vectorizing
    /// large algorithms. This type is immutable, individual elements cannot be modified.
    /// </summary>
    public struct Vector<T> : IEquatable<Vector<T>>, IFormattable where T : struct
    {
        #region Fields
        private Register register;
        #endregion Fields

        #region Static Members
        /// <summary>
        /// Returns the number of elements stored in the vector. This value is hardware dependent.
        /// </summary>
        [JitIntrinsic]
        public static int Count
        {
            get
            {
                return s_count;
            }
        }
        private static readonly int s_count = InitializeCount();

        /// <summary>
        /// Returns a vector containing all zeroes.
        /// </summary>
        [JitIntrinsic]
        public static Vector<T> Zero { get { return zero; } }
        private static readonly Vector<T> zero = new Vector<T>(GetZeroValue());

        /// <summary>
        /// Returns a vector containing all ones.
        /// </summary>
        [JitIntrinsic]
        public static Vector<T> One { get { return one; } }
        private static readonly Vector<T> one = new Vector<T>(GetOneValue());

        internal static Vector<T> AllOnes { get { return allOnes; } }
        private static readonly Vector<T> allOnes = new Vector<T>(GetAllBitsSetValue());
        #endregion Static Members

        #region Static Initialization
        private struct VectorSizeHelper
        {
            internal Vector<T> _placeholder;
            internal byte _byte;
        }

		// Calculates the size of this struct in bytes, by computing the offset of a field in a structure
        private static unsafe int InitializeCount()
        {
            VectorSizeHelper vsh;
            byte* vectorBase = &vsh._placeholder.register.byte_0;
            byte* byteBase = &vsh._byte;
            int vectorSizeInBytes = (int)(byteBase - vectorBase);

            int typeSizeInBytes = -1;
            if (typeof(T) == typeof(Byte))
            {
                typeSizeInBytes = sizeof(Byte);
            }
            else if (typeof(T) == typeof(SByte))
            {
                typeSizeInBytes = sizeof(SByte);
            }
            else if (typeof(T) == typeof(UInt16))
            {
                typeSizeInBytes = sizeof(UInt16);
            }
            else if (typeof(T) == typeof(Int16))
            {
                typeSizeInBytes = sizeof(Int16);
            }
            else if (typeof(T) == typeof(UInt32))
            {
                typeSizeInBytes = sizeof(UInt32);
            }
            else if (typeof(T) == typeof(Int32))
            {
                typeSizeInBytes = sizeof(Int32);
            }
            else if (typeof(T) == typeof(UInt64))
            {
                typeSizeInBytes = sizeof(UInt64);
            }
            else if (typeof(T) == typeof(Int64))
            {
                typeSizeInBytes = sizeof(Int64);
            }
            else if (typeof(T) == typeof(Single))
            {
                typeSizeInBytes = sizeof(Single);
            }
            else if (typeof(T) == typeof(Double))
            {
                typeSizeInBytes = sizeof(Double);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }

            return vectorSizeInBytes / typeSizeInBytes;
        }
        #endregion Static Initialization

        #region Constructors
        /// <summary>
        /// Constructs a vector whose components are all <code>value</code>
        /// </summary>
        [JitIntrinsic]
        public unsafe Vector(T value)
            : this()
        {
            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(Byte))
                {
                    fixed (Byte* basePtr = &this.register.byte_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Byte)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(SByte))
                {
                    fixed (SByte* basePtr = &this.register.sbyte_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (SByte)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    fixed (UInt16* basePtr = &this.register.uint16_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt16)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(Int16))
                {
                    fixed (Int16* basePtr = &this.register.int16_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int16)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    fixed (UInt32* basePtr = &this.register.uint32_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt32)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(Int32))
                {
                    fixed (Int32* basePtr = &this.register.int32_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int32)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    fixed (UInt64* basePtr = &this.register.uint64_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt64)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(Int64))
                {
                    fixed (Int64* basePtr = &this.register.int64_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int64)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(Single))
                {
                    fixed (Single* basePtr = &this.register.single_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Single)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(Double))
                {
                    fixed (Double* basePtr = &this.register.double_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Double)(object)value;
                        }
                    }
                }
            }
            else
            {
                if (typeof(T) == typeof(Byte))
                {
                    register.byte_0 = (Byte)(object)value;
                    register.byte_1 = (Byte)(object)value;
                    register.byte_2 = (Byte)(object)value;
                    register.byte_3 = (Byte)(object)value;
                    register.byte_4 = (Byte)(object)value;
                    register.byte_5 = (Byte)(object)value;
                    register.byte_6 = (Byte)(object)value;
                    register.byte_7 = (Byte)(object)value;
                    register.byte_8 = (Byte)(object)value;
                    register.byte_9 = (Byte)(object)value;
                    register.byte_10 = (Byte)(object)value;
                    register.byte_11 = (Byte)(object)value;
                    register.byte_12 = (Byte)(object)value;
                    register.byte_13 = (Byte)(object)value;
                    register.byte_14 = (Byte)(object)value;
                    register.byte_15 = (Byte)(object)value;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    register.sbyte_0 = (SByte)(object)value;
                    register.sbyte_1 = (SByte)(object)value;
                    register.sbyte_2 = (SByte)(object)value;
                    register.sbyte_3 = (SByte)(object)value;
                    register.sbyte_4 = (SByte)(object)value;
                    register.sbyte_5 = (SByte)(object)value;
                    register.sbyte_6 = (SByte)(object)value;
                    register.sbyte_7 = (SByte)(object)value;
                    register.sbyte_8 = (SByte)(object)value;
                    register.sbyte_9 = (SByte)(object)value;
                    register.sbyte_10 = (SByte)(object)value;
                    register.sbyte_11 = (SByte)(object)value;
                    register.sbyte_12 = (SByte)(object)value;
                    register.sbyte_13 = (SByte)(object)value;
                    register.sbyte_14 = (SByte)(object)value;
                    register.sbyte_15 = (SByte)(object)value;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    register.uint16_0 = (UInt16)(object)value;
                    register.uint16_1 = (UInt16)(object)value;
                    register.uint16_2 = (UInt16)(object)value;
                    register.uint16_3 = (UInt16)(object)value;
                    register.uint16_4 = (UInt16)(object)value;
                    register.uint16_5 = (UInt16)(object)value;
                    register.uint16_6 = (UInt16)(object)value;
                    register.uint16_7 = (UInt16)(object)value;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    register.int16_0 = (Int16)(object)value;
                    register.int16_1 = (Int16)(object)value;
                    register.int16_2 = (Int16)(object)value;
                    register.int16_3 = (Int16)(object)value;
                    register.int16_4 = (Int16)(object)value;
                    register.int16_5 = (Int16)(object)value;
                    register.int16_6 = (Int16)(object)value;
                    register.int16_7 = (Int16)(object)value;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    register.uint32_0 = (UInt32)(object)value;
                    register.uint32_1 = (UInt32)(object)value;
                    register.uint32_2 = (UInt32)(object)value;
                    register.uint32_3 = (UInt32)(object)value;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    register.int32_0 = (Int32)(object)value;
                    register.int32_1 = (Int32)(object)value;
                    register.int32_2 = (Int32)(object)value;
                    register.int32_3 = (Int32)(object)value;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    register.uint64_0 = (UInt64)(object)value;
                    register.uint64_1 = (UInt64)(object)value;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    register.int64_0 = (Int64)(object)value;
                    register.int64_1 = (Int64)(object)value;
                }
                else if (typeof(T) == typeof(Single))
                {
                    register.single_0 = (Single)(object)value;
                    register.single_1 = (Single)(object)value;
                    register.single_2 = (Single)(object)value;
                    register.single_3 = (Single)(object)value;
                }
                else if (typeof(T) == typeof(Double))
                {
                    register.double_0 = (Double)(object)value;
                    register.double_1 = (Double)(object)value;
                }
            }
        }

        /// <summary>
        /// Constructs a vector from the given array. The size of the given array must be at least Vector'T.Count.
        /// </summary>
        [JitIntrinsic]
        public unsafe Vector(T[] values) : this(values, 0) { }

        /// <summary>
        /// Constructs a vector from the given array, starting from the given index.
        /// The array must contain at least Vector'T.Count from the given index.
        /// </summary>
        public unsafe Vector(T[] values, int index)
            : this()
        {
            if (values == null)
            {
                // Match the JIT's exception type here. For perf, a NullReference is thrown instead of an ArgumentNull.
                throw new NullReferenceException(SR.Arg_NullArgumentNullRef);
            }
            if (index < 0 || (values.Length - index) < Count)
            {
                throw new IndexOutOfRangeException();
            }

            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(Byte))
                {
                    fixed (Byte* basePtr = &this.register.byte_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Byte)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(SByte))
                {
                    fixed (SByte* basePtr = &this.register.sbyte_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (SByte)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    fixed (UInt16* basePtr = &this.register.uint16_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt16)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(Int16))
                {
                    fixed (Int16* basePtr = &this.register.int16_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int16)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    fixed (UInt32* basePtr = &this.register.uint32_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt32)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(Int32))
                {
                    fixed (Int32* basePtr = &this.register.int32_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int32)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    fixed (UInt64* basePtr = &this.register.uint64_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt64)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(Int64))
                {
                    fixed (Int64* basePtr = &this.register.int64_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int64)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(Single))
                {
                    fixed (Single* basePtr = &this.register.single_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Single)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(Double))
                {
                    fixed (Double* basePtr = &this.register.double_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Double)(object)values[g + index];
                        }
                    }
                }
            }
            else
            {
                if (typeof(T) == typeof(Byte))
                {
                    fixed (Byte* basePtr = &this.register.byte_0)
                    {
                        *(basePtr + 0) = (Byte)(object)values[0 + index];
                        *(basePtr + 1) = (Byte)(object)values[1 + index];
                        *(basePtr + 2) = (Byte)(object)values[2 + index];
                        *(basePtr + 3) = (Byte)(object)values[3 + index];
                        *(basePtr + 4) = (Byte)(object)values[4 + index];
                        *(basePtr + 5) = (Byte)(object)values[5 + index];
                        *(basePtr + 6) = (Byte)(object)values[6 + index];
                        *(basePtr + 7) = (Byte)(object)values[7 + index];
                        *(basePtr + 8) = (Byte)(object)values[8 + index];
                        *(basePtr + 9) = (Byte)(object)values[9 + index];
                        *(basePtr + 10) = (Byte)(object)values[10 + index];
                        *(basePtr + 11) = (Byte)(object)values[11 + index];
                        *(basePtr + 12) = (Byte)(object)values[12 + index];
                        *(basePtr + 13) = (Byte)(object)values[13 + index];
                        *(basePtr + 14) = (Byte)(object)values[14 + index];
                        *(basePtr + 15) = (Byte)(object)values[15 + index];
                    }
                }
                else if (typeof(T) == typeof(SByte))
                {
                    fixed (SByte* basePtr = &this.register.sbyte_0)
                    {
                        *(basePtr + 0) = (SByte)(object)values[0 + index];
                        *(basePtr + 1) = (SByte)(object)values[1 + index];
                        *(basePtr + 2) = (SByte)(object)values[2 + index];
                        *(basePtr + 3) = (SByte)(object)values[3 + index];
                        *(basePtr + 4) = (SByte)(object)values[4 + index];
                        *(basePtr + 5) = (SByte)(object)values[5 + index];
                        *(basePtr + 6) = (SByte)(object)values[6 + index];
                        *(basePtr + 7) = (SByte)(object)values[7 + index];
                        *(basePtr + 8) = (SByte)(object)values[8 + index];
                        *(basePtr + 9) = (SByte)(object)values[9 + index];
                        *(basePtr + 10) = (SByte)(object)values[10 + index];
                        *(basePtr + 11) = (SByte)(object)values[11 + index];
                        *(basePtr + 12) = (SByte)(object)values[12 + index];
                        *(basePtr + 13) = (SByte)(object)values[13 + index];
                        *(basePtr + 14) = (SByte)(object)values[14 + index];
                        *(basePtr + 15) = (SByte)(object)values[15 + index];
                    }
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    fixed (UInt16* basePtr = &this.register.uint16_0)
                    {
                        *(basePtr + 0) = (UInt16)(object)values[0 + index];
                        *(basePtr + 1) = (UInt16)(object)values[1 + index];
                        *(basePtr + 2) = (UInt16)(object)values[2 + index];
                        *(basePtr + 3) = (UInt16)(object)values[3 + index];
                        *(basePtr + 4) = (UInt16)(object)values[4 + index];
                        *(basePtr + 5) = (UInt16)(object)values[5 + index];
                        *(basePtr + 6) = (UInt16)(object)values[6 + index];
                        *(basePtr + 7) = (UInt16)(object)values[7 + index];
                    }
                }
                else if (typeof(T) == typeof(Int16))
                {
                    fixed (Int16* basePtr = &this.register.int16_0)
                    {
                        *(basePtr + 0) = (Int16)(object)values[0 + index];
                        *(basePtr + 1) = (Int16)(object)values[1 + index];
                        *(basePtr + 2) = (Int16)(object)values[2 + index];
                        *(basePtr + 3) = (Int16)(object)values[3 + index];
                        *(basePtr + 4) = (Int16)(object)values[4 + index];
                        *(basePtr + 5) = (Int16)(object)values[5 + index];
                        *(basePtr + 6) = (Int16)(object)values[6 + index];
                        *(basePtr + 7) = (Int16)(object)values[7 + index];
                    }
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    fixed (UInt32* basePtr = &this.register.uint32_0)
                    {
                        *(basePtr + 0) = (UInt32)(object)values[0 + index];
                        *(basePtr + 1) = (UInt32)(object)values[1 + index];
                        *(basePtr + 2) = (UInt32)(object)values[2 + index];
                        *(basePtr + 3) = (UInt32)(object)values[3 + index];
                    }
                }
                else if (typeof(T) == typeof(Int32))
                {
                    fixed (Int32* basePtr = &this.register.int32_0)
                    {
                        *(basePtr + 0) = (Int32)(object)values[0 + index];
                        *(basePtr + 1) = (Int32)(object)values[1 + index];
                        *(basePtr + 2) = (Int32)(object)values[2 + index];
                        *(basePtr + 3) = (Int32)(object)values[3 + index];
                    }
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    fixed (UInt64* basePtr = &this.register.uint64_0)
                    {
                        *(basePtr + 0) = (UInt64)(object)values[0 + index];
                        *(basePtr + 1) = (UInt64)(object)values[1 + index];
                    }
                }
                else if (typeof(T) == typeof(Int64))
                {
                    fixed (Int64* basePtr = &this.register.int64_0)
                    {
                        *(basePtr + 0) = (Int64)(object)values[0 + index];
                        *(basePtr + 1) = (Int64)(object)values[1 + index];
                    }
                }
                else if (typeof(T) == typeof(Single))
                {
                    fixed (Single* basePtr = &this.register.single_0)
                    {
                        *(basePtr + 0) = (Single)(object)values[0 + index];
                        *(basePtr + 1) = (Single)(object)values[1 + index];
                        *(basePtr + 2) = (Single)(object)values[2 + index];
                        *(basePtr + 3) = (Single)(object)values[3 + index];
                    }
                }
                else if (typeof(T) == typeof(Double))
                {
                    fixed (Double* basePtr = &this.register.double_0)
                    {
                        *(basePtr + 0) = (Double)(object)values[0 + index];
                        *(basePtr + 1) = (Double)(object)values[1 + index];
                    }
                }
            }
        }

#pragma warning disable 3001 // void* is not a CLS-Compliant argument type
        internal unsafe Vector(void* dataPointer) : this(dataPointer, 0) { }
#pragma warning restore 3001 // void* is not a CLS-Compliant argument type

#pragma warning disable 3001 // void* is not a CLS-Compliant argument type
        // Implemented with offset if this API ever becomes public; an offset of 0 is used internally.
        internal unsafe Vector(void* dataPointer, int offset)
            : this()
        {
            if (typeof(T) == typeof(Byte))
            {
                Byte* castedPtr = (Byte*)dataPointer;
                castedPtr += offset;
                fixed (Byte* registerBase = &this.register.byte_0)
                {
                    for (int g = 0; g < Count; g++)
                    {
                        registerBase[g] = castedPtr[g];
                    }
                }
            }
            else if (typeof(T) == typeof(SByte))
            {
                SByte* castedPtr = (SByte*)dataPointer;
                castedPtr += offset;
                fixed (SByte* registerBase = &this.register.sbyte_0)
                {
                    for (int g = 0; g < Count; g++)
                    {
                        registerBase[g] = castedPtr[g];
                    }
                }
            }
            else if (typeof(T) == typeof(UInt16))
            {
                UInt16* castedPtr = (UInt16*)dataPointer;
                castedPtr += offset;
                fixed (UInt16* registerBase = &this.register.uint16_0)
                {
                    for (int g = 0; g < Count; g++)
                    {
                        registerBase[g] = castedPtr[g];
                    }
                }
            }
            else if (typeof(T) == typeof(Int16))
            {
                Int16* castedPtr = (Int16*)dataPointer;
                castedPtr += offset;
                fixed (Int16* registerBase = &this.register.int16_0)
                {
                    for (int g = 0; g < Count; g++)
                    {
                        registerBase[g] = castedPtr[g];
                    }
                }
            }
            else if (typeof(T) == typeof(UInt32))
            {
                UInt32* castedPtr = (UInt32*)dataPointer;
                castedPtr += offset;
                fixed (UInt32* registerBase = &this.register.uint32_0)
                {
                    for (int g = 0; g < Count; g++)
                    {
                        registerBase[g] = castedPtr[g];
                    }
                }
            }
            else if (typeof(T) == typeof(Int32))
            {
                Int32* castedPtr = (Int32*)dataPointer;
                castedPtr += offset;
                fixed (Int32* registerBase = &this.register.int32_0)
                {
                    for (int g = 0; g < Count; g++)
                    {
                        registerBase[g] = castedPtr[g];
                    }
                }
            }
            else if (typeof(T) == typeof(UInt64))
            {
                UInt64* castedPtr = (UInt64*)dataPointer;
                castedPtr += offset;
                fixed (UInt64* registerBase = &this.register.uint64_0)
                {
                    for (int g = 0; g < Count; g++)
                    {
                        registerBase[g] = castedPtr[g];
                    }
                }
            }
            else if (typeof(T) == typeof(Int64))
            {
                Int64* castedPtr = (Int64*)dataPointer;
                castedPtr += offset;
                fixed (Int64* registerBase = &this.register.int64_0)
                {
                    for (int g = 0; g < Count; g++)
                    {
                        registerBase[g] = castedPtr[g];
                    }
                }
            }
            else if (typeof(T) == typeof(Single))
            {
                Single* castedPtr = (Single*)dataPointer;
                castedPtr += offset;
                fixed (Single* registerBase = &this.register.single_0)
                {
                    for (int g = 0; g < Count; g++)
                    {
                        registerBase[g] = castedPtr[g];
                    }
                }
            }
            else if (typeof(T) == typeof(Double))
            {
                Double* castedPtr = (Double*)dataPointer;
                castedPtr += offset;
                fixed (Double* registerBase = &this.register.double_0)
                {
                    for (int g = 0; g < Count; g++)
                    {
                        registerBase[g] = castedPtr[g];
                    }
                }
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }
#pragma warning restore 3001 // void* is not a CLS-Compliant argument type

        private Vector(ref Register existingRegister)
        {
            this.register = existingRegister;
        }
        #endregion Constructors

        #region Public Instance Methods
        /// <summary>
        /// Copies the vector to the given destination array. The destination array must be at least size Vector'T.Count.
        /// </summary>
        /// <param name="destination">The destination array which the values are copied into</param>
        /// <exception cref="ArgumentNullException">If the destination array is null</exception>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination array</exception>
        [JitIntrinsic]
        public unsafe void CopyTo(T[] destination)
        {
            CopyTo(destination, 0);
        }

        /// <summary>
        /// Copies the vector to the given destination array. The destination array must be at least size Vector'T.Count.
        /// </summary>
        /// <param name="destination">The destination array which the values are copied into</param>
        /// <param name="startIndex">The index to start copying to</param>
        /// <exception cref="ArgumentNullException">If the destination array is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If index is greater than end of the array or index is less than zero</exception>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination array</exception>
        [JitIntrinsic]
        public unsafe void CopyTo(T[] destination, int startIndex)
        {
            if (destination == null)
            {
                // Match the JIT's exception type here. For perf, a NullReference is thrown instead of an ArgumentNull.
                throw new NullReferenceException(SR.Arg_NullArgumentNullRef);
            }
            if (startIndex < 0 || startIndex >= destination.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.Format(SR.Arg_ArgumentOutOfRangeException, startIndex));
            }
            if ((destination.Length - startIndex) < Count)
            {
                throw new ArgumentException(SR.Format(SR.Arg_ElementsInSourceIsGreaterThanDestination, startIndex));
            }

            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(Byte))
                {
                    Byte[] byteArray = (Byte[])(object)destination;
                    fixed (Byte* destinationBase = byteArray)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            destinationBase[startIndex + g] = (Byte)(object)this[g];
                        }
                    }
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte[] sbyteArray = (SByte[])(object)destination;
                    fixed (SByte* destinationBase = sbyteArray)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            destinationBase[startIndex + g] = (SByte)(object)this[g];
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16[] uint16Array = (UInt16[])(object)destination;
                    fixed (UInt16* destinationBase = uint16Array)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            destinationBase[startIndex + g] = (UInt16)(object)this[g];
                        }
                    }
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16[] int16Array = (Int16[])(object)destination;
                    fixed (Int16* destinationBase = int16Array)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            destinationBase[startIndex + g] = (Int16)(object)this[g];
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32[] uint32Array = (UInt32[])(object)destination;
                    fixed (UInt32* destinationBase = uint32Array)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            destinationBase[startIndex + g] = (UInt32)(object)this[g];
                        }
                    }
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32[] int32Array = (Int32[])(object)destination;
                    fixed (Int32* destinationBase = int32Array)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            destinationBase[startIndex + g] = (Int32)(object)this[g];
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64[] uint64Array = (UInt64[])(object)destination;
                    fixed (UInt64* destinationBase = uint64Array)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            destinationBase[startIndex + g] = (UInt64)(object)this[g];
                        }
                    }
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64[] int64Array = (Int64[])(object)destination;
                    fixed (Int64* destinationBase = int64Array)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            destinationBase[startIndex + g] = (Int64)(object)this[g];
                        }
                    }
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single[] singleArray = (Single[])(object)destination;
                    fixed (Single* destinationBase = singleArray)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            destinationBase[startIndex + g] = (Single)(object)this[g];
                        }
                    }
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double[] doubleArray = (Double[])(object)destination;
                    fixed (Double* destinationBase = doubleArray)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            destinationBase[startIndex + g] = (Double)(object)this[g];
                        }
                    }
                }
            }
            else
            {
                if (typeof(T) == typeof(Byte))
                {
                    Byte[] byteArray = (Byte[])(object)destination;
                    fixed (Byte* destinationBase = byteArray)
                    {
                        destinationBase[startIndex + 0] = this.register.byte_0;
                        destinationBase[startIndex + 1] = this.register.byte_1;
                        destinationBase[startIndex + 2] = this.register.byte_2;
                        destinationBase[startIndex + 3] = this.register.byte_3;
                        destinationBase[startIndex + 4] = this.register.byte_4;
                        destinationBase[startIndex + 5] = this.register.byte_5;
                        destinationBase[startIndex + 6] = this.register.byte_6;
                        destinationBase[startIndex + 7] = this.register.byte_7;
                        destinationBase[startIndex + 8] = this.register.byte_8;
                        destinationBase[startIndex + 9] = this.register.byte_9;
                        destinationBase[startIndex + 10] = this.register.byte_10;
                        destinationBase[startIndex + 11] = this.register.byte_11;
                        destinationBase[startIndex + 12] = this.register.byte_12;
                        destinationBase[startIndex + 13] = this.register.byte_13;
                        destinationBase[startIndex + 14] = this.register.byte_14;
                        destinationBase[startIndex + 15] = this.register.byte_15;
                    }
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte[] sbyteArray = (SByte[])(object)destination;
                    fixed (SByte* destinationBase = sbyteArray)
                    {
                        destinationBase[startIndex + 0] = this.register.sbyte_0;
                        destinationBase[startIndex + 1] = this.register.sbyte_1;
                        destinationBase[startIndex + 2] = this.register.sbyte_2;
                        destinationBase[startIndex + 3] = this.register.sbyte_3;
                        destinationBase[startIndex + 4] = this.register.sbyte_4;
                        destinationBase[startIndex + 5] = this.register.sbyte_5;
                        destinationBase[startIndex + 6] = this.register.sbyte_6;
                        destinationBase[startIndex + 7] = this.register.sbyte_7;
                        destinationBase[startIndex + 8] = this.register.sbyte_8;
                        destinationBase[startIndex + 9] = this.register.sbyte_9;
                        destinationBase[startIndex + 10] = this.register.sbyte_10;
                        destinationBase[startIndex + 11] = this.register.sbyte_11;
                        destinationBase[startIndex + 12] = this.register.sbyte_12;
                        destinationBase[startIndex + 13] = this.register.sbyte_13;
                        destinationBase[startIndex + 14] = this.register.sbyte_14;
                        destinationBase[startIndex + 15] = this.register.sbyte_15;
                    }
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16[] uint16Array = (UInt16[])(object)destination;
                    fixed (UInt16* destinationBase = uint16Array)
                    {
                        destinationBase[startIndex + 0] = this.register.uint16_0;
                        destinationBase[startIndex + 1] = this.register.uint16_1;
                        destinationBase[startIndex + 2] = this.register.uint16_2;
                        destinationBase[startIndex + 3] = this.register.uint16_3;
                        destinationBase[startIndex + 4] = this.register.uint16_4;
                        destinationBase[startIndex + 5] = this.register.uint16_5;
                        destinationBase[startIndex + 6] = this.register.uint16_6;
                        destinationBase[startIndex + 7] = this.register.uint16_7;
                    }
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16[] int16Array = (Int16[])(object)destination;
                    fixed (Int16* destinationBase = int16Array)
                    {
                        destinationBase[startIndex + 0] = this.register.int16_0;
                        destinationBase[startIndex + 1] = this.register.int16_1;
                        destinationBase[startIndex + 2] = this.register.int16_2;
                        destinationBase[startIndex + 3] = this.register.int16_3;
                        destinationBase[startIndex + 4] = this.register.int16_4;
                        destinationBase[startIndex + 5] = this.register.int16_5;
                        destinationBase[startIndex + 6] = this.register.int16_6;
                        destinationBase[startIndex + 7] = this.register.int16_7;
                    }
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32[] uint32Array = (UInt32[])(object)destination;
                    fixed (UInt32* destinationBase = uint32Array)
                    {
                        destinationBase[startIndex + 0] = this.register.uint32_0;
                        destinationBase[startIndex + 1] = this.register.uint32_1;
                        destinationBase[startIndex + 2] = this.register.uint32_2;
                        destinationBase[startIndex + 3] = this.register.uint32_3;
                    }
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32[] int32Array = (Int32[])(object)destination;
                    fixed (Int32* destinationBase = int32Array)
                    {
                        destinationBase[startIndex + 0] = this.register.int32_0;
                        destinationBase[startIndex + 1] = this.register.int32_1;
                        destinationBase[startIndex + 2] = this.register.int32_2;
                        destinationBase[startIndex + 3] = this.register.int32_3;
                    }
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64[] uint64Array = (UInt64[])(object)destination;
                    fixed (UInt64* destinationBase = uint64Array)
                    {
                        destinationBase[startIndex + 0] = this.register.uint64_0;
                        destinationBase[startIndex + 1] = this.register.uint64_1;
                    }
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64[] int64Array = (Int64[])(object)destination;
                    fixed (Int64* destinationBase = int64Array)
                    {
                        destinationBase[startIndex + 0] = this.register.int64_0;
                        destinationBase[startIndex + 1] = this.register.int64_1;
                    }
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single[] singleArray = (Single[])(object)destination;
                    fixed (Single* destinationBase = singleArray)
                    {
                        destinationBase[startIndex + 0] = this.register.single_0;
                        destinationBase[startIndex + 1] = this.register.single_1;
                        destinationBase[startIndex + 2] = this.register.single_2;
                        destinationBase[startIndex + 3] = this.register.single_3;
                    }
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double[] doubleArray = (Double[])(object)destination;
                    fixed (Double* destinationBase = doubleArray)
                    {
                        destinationBase[startIndex + 0] = this.register.double_0;
                        destinationBase[startIndex + 1] = this.register.double_1;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the element at the given index.
        /// </summary>
        [JitIntrinsic]
        public unsafe T this[int index]
        {
            get
            {
                if (index >= Count || index < 0)
                {
                    throw new IndexOutOfRangeException(SR.Format(SR.Arg_ArgumentOutOfRangeException, index));
                }
                if (typeof(T) == typeof(Byte))
                {
                    fixed (Byte* basePtr = &this.register.byte_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(SByte))
                {
                    fixed (SByte* basePtr = &this.register.sbyte_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    fixed (UInt16* basePtr = &this.register.uint16_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(Int16))
                {
                    fixed (Int16* basePtr = &this.register.int16_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    fixed (UInt32* basePtr = &this.register.uint32_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(Int32))
                {
                    fixed (Int32* basePtr = &this.register.int32_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    fixed (UInt64* basePtr = &this.register.uint64_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(Int64))
                {
                    fixed (Int64* basePtr = &this.register.int64_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(Single))
                {
                    fixed (Single* basePtr = &this.register.single_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(Double))
                {
                    fixed (Double* basePtr = &this.register.double_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this vector instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this vector; False otherwise.</returns>
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (!(obj is Vector<T>))
            {
                return false;
            }
            return Equals((Vector<T>)obj);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given vector is equal to this vector instance.
        /// </summary>
        /// <param name="other">The vector to compare this instance to.</param>
        /// <returns>True if the other vector is equal to this instance; False otherwise.</returns>
        [JitIntrinsic]
        public bool Equals(Vector<T> other)
        {
            if (Vector.IsHardwareAccelerated)
            {
                for (int g = 0; g < Count; g++)
                {
                    if (!ScalarEquals(this[g], other[g]))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                if (typeof(T) == typeof(Byte))
                {
                    return
                        this.register.byte_0 == other.register.byte_0
                        && this.register.byte_1 == other.register.byte_1
                        && this.register.byte_2 == other.register.byte_2
                        && this.register.byte_3 == other.register.byte_3
                        && this.register.byte_4 == other.register.byte_4
                        && this.register.byte_5 == other.register.byte_5
                        && this.register.byte_6 == other.register.byte_6
                        && this.register.byte_7 == other.register.byte_7
                        && this.register.byte_8 == other.register.byte_8
                        && this.register.byte_9 == other.register.byte_9
                        && this.register.byte_10 == other.register.byte_10
                        && this.register.byte_11 == other.register.byte_11
                        && this.register.byte_12 == other.register.byte_12
                        && this.register.byte_13 == other.register.byte_13
                        && this.register.byte_14 == other.register.byte_14
                        && this.register.byte_15 == other.register.byte_15;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    return
                        this.register.sbyte_0 == other.register.sbyte_0
                        && this.register.sbyte_1 == other.register.sbyte_1
                        && this.register.sbyte_2 == other.register.sbyte_2
                        && this.register.sbyte_3 == other.register.sbyte_3
                        && this.register.sbyte_4 == other.register.sbyte_4
                        && this.register.sbyte_5 == other.register.sbyte_5
                        && this.register.sbyte_6 == other.register.sbyte_6
                        && this.register.sbyte_7 == other.register.sbyte_7
                        && this.register.sbyte_8 == other.register.sbyte_8
                        && this.register.sbyte_9 == other.register.sbyte_9
                        && this.register.sbyte_10 == other.register.sbyte_10
                        && this.register.sbyte_11 == other.register.sbyte_11
                        && this.register.sbyte_12 == other.register.sbyte_12
                        && this.register.sbyte_13 == other.register.sbyte_13
                        && this.register.sbyte_14 == other.register.sbyte_14
                        && this.register.sbyte_15 == other.register.sbyte_15;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    return
                        this.register.uint16_0 == other.register.uint16_0
                        && this.register.uint16_1 == other.register.uint16_1
                        && this.register.uint16_2 == other.register.uint16_2
                        && this.register.uint16_3 == other.register.uint16_3
                        && this.register.uint16_4 == other.register.uint16_4
                        && this.register.uint16_5 == other.register.uint16_5
                        && this.register.uint16_6 == other.register.uint16_6
                        && this.register.uint16_7 == other.register.uint16_7;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    return
                        this.register.int16_0 == other.register.int16_0
                        && this.register.int16_1 == other.register.int16_1
                        && this.register.int16_2 == other.register.int16_2
                        && this.register.int16_3 == other.register.int16_3
                        && this.register.int16_4 == other.register.int16_4
                        && this.register.int16_5 == other.register.int16_5
                        && this.register.int16_6 == other.register.int16_6
                        && this.register.int16_7 == other.register.int16_7;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    return
                        this.register.uint32_0 == other.register.uint32_0
                        && this.register.uint32_1 == other.register.uint32_1
                        && this.register.uint32_2 == other.register.uint32_2
                        && this.register.uint32_3 == other.register.uint32_3;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    return
                        this.register.int32_0 == other.register.int32_0
                        && this.register.int32_1 == other.register.int32_1
                        && this.register.int32_2 == other.register.int32_2
                        && this.register.int32_3 == other.register.int32_3;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    return
                        this.register.uint64_0 == other.register.uint64_0
                        && this.register.uint64_1 == other.register.uint64_1;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    return
                        this.register.int64_0 == other.register.int64_0
                        && this.register.int64_1 == other.register.int64_1;
                }
                else if (typeof(T) == typeof(Single))
                {
                    return
                        this.register.single_0 == other.register.single_0
                        && this.register.single_1 == other.register.single_1
                        && this.register.single_2 == other.register.single_2
                        && this.register.single_3 == other.register.single_3;
                }
                else if (typeof(T) == typeof(Double))
                {
                    return
                        this.register.double_0 == other.register.double_0
                        && this.register.double_1 == other.register.double_1;
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            int hash = 0;

            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(Byte))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashHelpers.Combine(hash, ((Byte)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashHelpers.Combine(hash, ((SByte)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashHelpers.Combine(hash, ((UInt16)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashHelpers.Combine(hash, ((Int16)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashHelpers.Combine(hash, ((UInt32)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashHelpers.Combine(hash, ((Int32)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashHelpers.Combine(hash, ((UInt64)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashHelpers.Combine(hash, ((Int64)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(Single))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashHelpers.Combine(hash, ((Single)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(Double))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashHelpers.Combine(hash, ((Double)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
            else
            {
                if (typeof(T) == typeof(Byte))
                {
                    hash = HashHelpers.Combine(hash, this.register.byte_0.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_1.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_2.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_3.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_4.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_5.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_6.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_7.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_8.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_9.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_10.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_11.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_12.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_13.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_14.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.byte_15.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    hash = HashHelpers.Combine(hash, this.register.sbyte_0.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_1.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_2.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_3.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_4.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_5.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_6.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_7.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_8.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_9.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_10.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_11.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_12.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_13.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_14.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.sbyte_15.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    hash = HashHelpers.Combine(hash, this.register.uint16_0.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.uint16_1.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.uint16_2.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.uint16_3.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.uint16_4.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.uint16_5.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.uint16_6.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.uint16_7.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    hash = HashHelpers.Combine(hash, this.register.int16_0.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.int16_1.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.int16_2.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.int16_3.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.int16_4.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.int16_5.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.int16_6.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.int16_7.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    hash = HashHelpers.Combine(hash, this.register.uint32_0.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.uint32_1.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.uint32_2.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.uint32_3.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    hash = HashHelpers.Combine(hash, this.register.int32_0.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.int32_1.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.int32_2.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.int32_3.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    hash = HashHelpers.Combine(hash, this.register.uint64_0.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.uint64_1.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    hash = HashHelpers.Combine(hash, this.register.int64_0.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.int64_1.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(Single))
                {
                    hash = HashHelpers.Combine(hash, this.register.single_0.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.single_1.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.single_2.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.single_3.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(Double))
                {
                    hash = HashHelpers.Combine(hash, this.register.double_0.GetHashCode());
                    hash = HashHelpers.Combine(hash, this.register.double_1.GetHashCode());
                    return hash;
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
        }

        /// <summary>
        /// Returns a String representing this vector.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return ToString("G", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns a String representing this vector, using the specified format string to format individual elements.
        /// </summary>
        /// <param name="format">The format of individual elements.</param>
        /// <returns>The string representation.</returns>
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns a String representing this vector, using the specified format string to format individual elements
        /// and the given IFormatProvider.
        /// </summary>
        /// <param name="format">The format of individual elements.</param>
        /// <param name="formatProvider">The format provider to use when formatting elements.</param>
        /// <returns>The string representation.</returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            sb.Append('<');
            for (int g = 0; g < Count - 1; g++)
            {
                sb.Append(((IFormattable)this[g]).ToString(format, formatProvider));
                sb.Append(separator);
                sb.Append(' ');
            }
            // Append last element w/out separator
            sb.Append(((IFormattable)this[Count - 1]).ToString(format, formatProvider));
            sb.Append('>');
            return sb.ToString();
        }
        #endregion Public Instance Methods

        #region Arithmetic Operators
        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        public static unsafe Vector<T> operator +(Vector<T> left, Vector<T> right)
        {
            unchecked
            {
                if (Vector.IsHardwareAccelerated)
                {
                    if (typeof(T) == typeof(Byte))
                    {
                        Byte* dataPtr = stackalloc Byte[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Byte)(object)ScalarAdd(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        SByte* dataPtr = stackalloc SByte[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (SByte)(object)ScalarAdd(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        UInt16* dataPtr = stackalloc UInt16[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt16)(object)ScalarAdd(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        Int16* dataPtr = stackalloc Int16[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int16)(object)ScalarAdd(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        UInt32* dataPtr = stackalloc UInt32[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt32)(object)ScalarAdd(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        Int32* dataPtr = stackalloc Int32[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int32)(object)ScalarAdd(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        UInt64* dataPtr = stackalloc UInt64[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt64)(object)ScalarAdd(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        Int64* dataPtr = stackalloc Int64[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int64)(object)ScalarAdd(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        Single* dataPtr = stackalloc Single[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Single)(object)ScalarAdd(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        Double* dataPtr = stackalloc Double[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Double)(object)ScalarAdd(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else
                    {
                        throw new NotSupportedException(SR.Arg_TypeNotSupported);
                    }
                }
                else
                {
                    Vector<T> sum = new Vector<T>();
                    if (typeof(T) == typeof(Byte))
                    {
                        sum.register.byte_0 = (Byte)(left.register.byte_0 + right.register.byte_0);
                        sum.register.byte_1 = (Byte)(left.register.byte_1 + right.register.byte_1);
                        sum.register.byte_2 = (Byte)(left.register.byte_2 + right.register.byte_2);
                        sum.register.byte_3 = (Byte)(left.register.byte_3 + right.register.byte_3);
                        sum.register.byte_4 = (Byte)(left.register.byte_4 + right.register.byte_4);
                        sum.register.byte_5 = (Byte)(left.register.byte_5 + right.register.byte_5);
                        sum.register.byte_6 = (Byte)(left.register.byte_6 + right.register.byte_6);
                        sum.register.byte_7 = (Byte)(left.register.byte_7 + right.register.byte_7);
                        sum.register.byte_8 = (Byte)(left.register.byte_8 + right.register.byte_8);
                        sum.register.byte_9 = (Byte)(left.register.byte_9 + right.register.byte_9);
                        sum.register.byte_10 = (Byte)(left.register.byte_10 + right.register.byte_10);
                        sum.register.byte_11 = (Byte)(left.register.byte_11 + right.register.byte_11);
                        sum.register.byte_12 = (Byte)(left.register.byte_12 + right.register.byte_12);
                        sum.register.byte_13 = (Byte)(left.register.byte_13 + right.register.byte_13);
                        sum.register.byte_14 = (Byte)(left.register.byte_14 + right.register.byte_14);
                        sum.register.byte_15 = (Byte)(left.register.byte_15 + right.register.byte_15);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        sum.register.sbyte_0 = (SByte)(left.register.sbyte_0 + right.register.sbyte_0);
                        sum.register.sbyte_1 = (SByte)(left.register.sbyte_1 + right.register.sbyte_1);
                        sum.register.sbyte_2 = (SByte)(left.register.sbyte_2 + right.register.sbyte_2);
                        sum.register.sbyte_3 = (SByte)(left.register.sbyte_3 + right.register.sbyte_3);
                        sum.register.sbyte_4 = (SByte)(left.register.sbyte_4 + right.register.sbyte_4);
                        sum.register.sbyte_5 = (SByte)(left.register.sbyte_5 + right.register.sbyte_5);
                        sum.register.sbyte_6 = (SByte)(left.register.sbyte_6 + right.register.sbyte_6);
                        sum.register.sbyte_7 = (SByte)(left.register.sbyte_7 + right.register.sbyte_7);
                        sum.register.sbyte_8 = (SByte)(left.register.sbyte_8 + right.register.sbyte_8);
                        sum.register.sbyte_9 = (SByte)(left.register.sbyte_9 + right.register.sbyte_9);
                        sum.register.sbyte_10 = (SByte)(left.register.sbyte_10 + right.register.sbyte_10);
                        sum.register.sbyte_11 = (SByte)(left.register.sbyte_11 + right.register.sbyte_11);
                        sum.register.sbyte_12 = (SByte)(left.register.sbyte_12 + right.register.sbyte_12);
                        sum.register.sbyte_13 = (SByte)(left.register.sbyte_13 + right.register.sbyte_13);
                        sum.register.sbyte_14 = (SByte)(left.register.sbyte_14 + right.register.sbyte_14);
                        sum.register.sbyte_15 = (SByte)(left.register.sbyte_15 + right.register.sbyte_15);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        sum.register.uint16_0 = (UInt16)(left.register.uint16_0 + right.register.uint16_0);
                        sum.register.uint16_1 = (UInt16)(left.register.uint16_1 + right.register.uint16_1);
                        sum.register.uint16_2 = (UInt16)(left.register.uint16_2 + right.register.uint16_2);
                        sum.register.uint16_3 = (UInt16)(left.register.uint16_3 + right.register.uint16_3);
                        sum.register.uint16_4 = (UInt16)(left.register.uint16_4 + right.register.uint16_4);
                        sum.register.uint16_5 = (UInt16)(left.register.uint16_5 + right.register.uint16_5);
                        sum.register.uint16_6 = (UInt16)(left.register.uint16_6 + right.register.uint16_6);
                        sum.register.uint16_7 = (UInt16)(left.register.uint16_7 + right.register.uint16_7);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        sum.register.int16_0 = (Int16)(left.register.int16_0 + right.register.int16_0);
                        sum.register.int16_1 = (Int16)(left.register.int16_1 + right.register.int16_1);
                        sum.register.int16_2 = (Int16)(left.register.int16_2 + right.register.int16_2);
                        sum.register.int16_3 = (Int16)(left.register.int16_3 + right.register.int16_3);
                        sum.register.int16_4 = (Int16)(left.register.int16_4 + right.register.int16_4);
                        sum.register.int16_5 = (Int16)(left.register.int16_5 + right.register.int16_5);
                        sum.register.int16_6 = (Int16)(left.register.int16_6 + right.register.int16_6);
                        sum.register.int16_7 = (Int16)(left.register.int16_7 + right.register.int16_7);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        sum.register.uint32_0 = (UInt32)(left.register.uint32_0 + right.register.uint32_0);
                        sum.register.uint32_1 = (UInt32)(left.register.uint32_1 + right.register.uint32_1);
                        sum.register.uint32_2 = (UInt32)(left.register.uint32_2 + right.register.uint32_2);
                        sum.register.uint32_3 = (UInt32)(left.register.uint32_3 + right.register.uint32_3);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        sum.register.int32_0 = (Int32)(left.register.int32_0 + right.register.int32_0);
                        sum.register.int32_1 = (Int32)(left.register.int32_1 + right.register.int32_1);
                        sum.register.int32_2 = (Int32)(left.register.int32_2 + right.register.int32_2);
                        sum.register.int32_3 = (Int32)(left.register.int32_3 + right.register.int32_3);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        sum.register.uint64_0 = (UInt64)(left.register.uint64_0 + right.register.uint64_0);
                        sum.register.uint64_1 = (UInt64)(left.register.uint64_1 + right.register.uint64_1);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        sum.register.int64_0 = (Int64)(left.register.int64_0 + right.register.int64_0);
                        sum.register.int64_1 = (Int64)(left.register.int64_1 + right.register.int64_1);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        sum.register.single_0 = (Single)(left.register.single_0 + right.register.single_0);
                        sum.register.single_1 = (Single)(left.register.single_1 + right.register.single_1);
                        sum.register.single_2 = (Single)(left.register.single_2 + right.register.single_2);
                        sum.register.single_3 = (Single)(left.register.single_3 + right.register.single_3);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        sum.register.double_0 = (Double)(left.register.double_0 + right.register.double_0);
                        sum.register.double_1 = (Double)(left.register.double_1 + right.register.double_1);
                    }
                    return sum;
                }
            }
        }

        /// <summary>
        /// Subtracts the second vector from the first.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        public static unsafe Vector<T> operator -(Vector<T> left, Vector<T> right)
        {
            unchecked
            {
                if (Vector.IsHardwareAccelerated)
                {
                    if (typeof(T) == typeof(Byte))
                    {
                        Byte* dataPtr = stackalloc Byte[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Byte)(object)ScalarSubtract(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        SByte* dataPtr = stackalloc SByte[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (SByte)(object)ScalarSubtract(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        UInt16* dataPtr = stackalloc UInt16[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt16)(object)ScalarSubtract(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        Int16* dataPtr = stackalloc Int16[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int16)(object)ScalarSubtract(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        UInt32* dataPtr = stackalloc UInt32[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt32)(object)ScalarSubtract(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        Int32* dataPtr = stackalloc Int32[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int32)(object)ScalarSubtract(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        UInt64* dataPtr = stackalloc UInt64[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt64)(object)ScalarSubtract(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        Int64* dataPtr = stackalloc Int64[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int64)(object)ScalarSubtract(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        Single* dataPtr = stackalloc Single[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Single)(object)ScalarSubtract(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        Double* dataPtr = stackalloc Double[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Double)(object)ScalarSubtract(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else
                    {
                        throw new NotSupportedException(SR.Arg_TypeNotSupported);
                    }
                }
                else
                {
                    Vector<T> difference = new Vector<T>();
                    if (typeof(T) == typeof(Byte))
                    {
                        difference.register.byte_0 = (Byte)(left.register.byte_0 - right.register.byte_0);
                        difference.register.byte_1 = (Byte)(left.register.byte_1 - right.register.byte_1);
                        difference.register.byte_2 = (Byte)(left.register.byte_2 - right.register.byte_2);
                        difference.register.byte_3 = (Byte)(left.register.byte_3 - right.register.byte_3);
                        difference.register.byte_4 = (Byte)(left.register.byte_4 - right.register.byte_4);
                        difference.register.byte_5 = (Byte)(left.register.byte_5 - right.register.byte_5);
                        difference.register.byte_6 = (Byte)(left.register.byte_6 - right.register.byte_6);
                        difference.register.byte_7 = (Byte)(left.register.byte_7 - right.register.byte_7);
                        difference.register.byte_8 = (Byte)(left.register.byte_8 - right.register.byte_8);
                        difference.register.byte_9 = (Byte)(left.register.byte_9 - right.register.byte_9);
                        difference.register.byte_10 = (Byte)(left.register.byte_10 - right.register.byte_10);
                        difference.register.byte_11 = (Byte)(left.register.byte_11 - right.register.byte_11);
                        difference.register.byte_12 = (Byte)(left.register.byte_12 - right.register.byte_12);
                        difference.register.byte_13 = (Byte)(left.register.byte_13 - right.register.byte_13);
                        difference.register.byte_14 = (Byte)(left.register.byte_14 - right.register.byte_14);
                        difference.register.byte_15 = (Byte)(left.register.byte_15 - right.register.byte_15);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        difference.register.sbyte_0 = (SByte)(left.register.sbyte_0 - right.register.sbyte_0);
                        difference.register.sbyte_1 = (SByte)(left.register.sbyte_1 - right.register.sbyte_1);
                        difference.register.sbyte_2 = (SByte)(left.register.sbyte_2 - right.register.sbyte_2);
                        difference.register.sbyte_3 = (SByte)(left.register.sbyte_3 - right.register.sbyte_3);
                        difference.register.sbyte_4 = (SByte)(left.register.sbyte_4 - right.register.sbyte_4);
                        difference.register.sbyte_5 = (SByte)(left.register.sbyte_5 - right.register.sbyte_5);
                        difference.register.sbyte_6 = (SByte)(left.register.sbyte_6 - right.register.sbyte_6);
                        difference.register.sbyte_7 = (SByte)(left.register.sbyte_7 - right.register.sbyte_7);
                        difference.register.sbyte_8 = (SByte)(left.register.sbyte_8 - right.register.sbyte_8);
                        difference.register.sbyte_9 = (SByte)(left.register.sbyte_9 - right.register.sbyte_9);
                        difference.register.sbyte_10 = (SByte)(left.register.sbyte_10 - right.register.sbyte_10);
                        difference.register.sbyte_11 = (SByte)(left.register.sbyte_11 - right.register.sbyte_11);
                        difference.register.sbyte_12 = (SByte)(left.register.sbyte_12 - right.register.sbyte_12);
                        difference.register.sbyte_13 = (SByte)(left.register.sbyte_13 - right.register.sbyte_13);
                        difference.register.sbyte_14 = (SByte)(left.register.sbyte_14 - right.register.sbyte_14);
                        difference.register.sbyte_15 = (SByte)(left.register.sbyte_15 - right.register.sbyte_15);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        difference.register.uint16_0 = (UInt16)(left.register.uint16_0 - right.register.uint16_0);
                        difference.register.uint16_1 = (UInt16)(left.register.uint16_1 - right.register.uint16_1);
                        difference.register.uint16_2 = (UInt16)(left.register.uint16_2 - right.register.uint16_2);
                        difference.register.uint16_3 = (UInt16)(left.register.uint16_3 - right.register.uint16_3);
                        difference.register.uint16_4 = (UInt16)(left.register.uint16_4 - right.register.uint16_4);
                        difference.register.uint16_5 = (UInt16)(left.register.uint16_5 - right.register.uint16_5);
                        difference.register.uint16_6 = (UInt16)(left.register.uint16_6 - right.register.uint16_6);
                        difference.register.uint16_7 = (UInt16)(left.register.uint16_7 - right.register.uint16_7);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        difference.register.int16_0 = (Int16)(left.register.int16_0 - right.register.int16_0);
                        difference.register.int16_1 = (Int16)(left.register.int16_1 - right.register.int16_1);
                        difference.register.int16_2 = (Int16)(left.register.int16_2 - right.register.int16_2);
                        difference.register.int16_3 = (Int16)(left.register.int16_3 - right.register.int16_3);
                        difference.register.int16_4 = (Int16)(left.register.int16_4 - right.register.int16_4);
                        difference.register.int16_5 = (Int16)(left.register.int16_5 - right.register.int16_5);
                        difference.register.int16_6 = (Int16)(left.register.int16_6 - right.register.int16_6);
                        difference.register.int16_7 = (Int16)(left.register.int16_7 - right.register.int16_7);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        difference.register.uint32_0 = (UInt32)(left.register.uint32_0 - right.register.uint32_0);
                        difference.register.uint32_1 = (UInt32)(left.register.uint32_1 - right.register.uint32_1);
                        difference.register.uint32_2 = (UInt32)(left.register.uint32_2 - right.register.uint32_2);
                        difference.register.uint32_3 = (UInt32)(left.register.uint32_3 - right.register.uint32_3);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        difference.register.int32_0 = (Int32)(left.register.int32_0 - right.register.int32_0);
                        difference.register.int32_1 = (Int32)(left.register.int32_1 - right.register.int32_1);
                        difference.register.int32_2 = (Int32)(left.register.int32_2 - right.register.int32_2);
                        difference.register.int32_3 = (Int32)(left.register.int32_3 - right.register.int32_3);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        difference.register.uint64_0 = (UInt64)(left.register.uint64_0 - right.register.uint64_0);
                        difference.register.uint64_1 = (UInt64)(left.register.uint64_1 - right.register.uint64_1);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        difference.register.int64_0 = (Int64)(left.register.int64_0 - right.register.int64_0);
                        difference.register.int64_1 = (Int64)(left.register.int64_1 - right.register.int64_1);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        difference.register.single_0 = (Single)(left.register.single_0 - right.register.single_0);
                        difference.register.single_1 = (Single)(left.register.single_1 - right.register.single_1);
                        difference.register.single_2 = (Single)(left.register.single_2 - right.register.single_2);
                        difference.register.single_3 = (Single)(left.register.single_3 - right.register.single_3);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        difference.register.double_0 = (Double)(left.register.double_0 - right.register.double_0);
                        difference.register.double_1 = (Double)(left.register.double_1 - right.register.double_1);
                    }
                    return difference;
                }
            }
        }

        // This method is intrinsic only for certain types. It cannot access fields directly unless we are sure the context is unaccelerated.
        /// <summary>
        /// Multiplies two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The product vector.</returns>
        public static unsafe Vector<T> operator *(Vector<T> left, Vector<T> right)
        {
            unchecked
            {
                if (Vector.IsHardwareAccelerated)
                {
                    if (typeof(T) == typeof(Byte))
                    {
                        Byte* dataPtr = stackalloc Byte[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Byte)(object)ScalarMultiply(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        SByte* dataPtr = stackalloc SByte[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (SByte)(object)ScalarMultiply(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        UInt16* dataPtr = stackalloc UInt16[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt16)(object)ScalarMultiply(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        Int16* dataPtr = stackalloc Int16[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int16)(object)ScalarMultiply(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        UInt32* dataPtr = stackalloc UInt32[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt32)(object)ScalarMultiply(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        Int32* dataPtr = stackalloc Int32[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int32)(object)ScalarMultiply(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        UInt64* dataPtr = stackalloc UInt64[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt64)(object)ScalarMultiply(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        Int64* dataPtr = stackalloc Int64[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int64)(object)ScalarMultiply(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        Single* dataPtr = stackalloc Single[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Single)(object)ScalarMultiply(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        Double* dataPtr = stackalloc Double[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Double)(object)ScalarMultiply(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else
                    {
                        throw new NotSupportedException(SR.Arg_TypeNotSupported);
                    }
                }
                else
                {
                    Vector<T> product = new Vector<T>();
                    if (typeof(T) == typeof(Byte))
                    {
                        product.register.byte_0 = (Byte)(left.register.byte_0 * right.register.byte_0);
                        product.register.byte_1 = (Byte)(left.register.byte_1 * right.register.byte_1);
                        product.register.byte_2 = (Byte)(left.register.byte_2 * right.register.byte_2);
                        product.register.byte_3 = (Byte)(left.register.byte_3 * right.register.byte_3);
                        product.register.byte_4 = (Byte)(left.register.byte_4 * right.register.byte_4);
                        product.register.byte_5 = (Byte)(left.register.byte_5 * right.register.byte_5);
                        product.register.byte_6 = (Byte)(left.register.byte_6 * right.register.byte_6);
                        product.register.byte_7 = (Byte)(left.register.byte_7 * right.register.byte_7);
                        product.register.byte_8 = (Byte)(left.register.byte_8 * right.register.byte_8);
                        product.register.byte_9 = (Byte)(left.register.byte_9 * right.register.byte_9);
                        product.register.byte_10 = (Byte)(left.register.byte_10 * right.register.byte_10);
                        product.register.byte_11 = (Byte)(left.register.byte_11 * right.register.byte_11);
                        product.register.byte_12 = (Byte)(left.register.byte_12 * right.register.byte_12);
                        product.register.byte_13 = (Byte)(left.register.byte_13 * right.register.byte_13);
                        product.register.byte_14 = (Byte)(left.register.byte_14 * right.register.byte_14);
                        product.register.byte_15 = (Byte)(left.register.byte_15 * right.register.byte_15);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        product.register.sbyte_0 = (SByte)(left.register.sbyte_0 * right.register.sbyte_0);
                        product.register.sbyte_1 = (SByte)(left.register.sbyte_1 * right.register.sbyte_1);
                        product.register.sbyte_2 = (SByte)(left.register.sbyte_2 * right.register.sbyte_2);
                        product.register.sbyte_3 = (SByte)(left.register.sbyte_3 * right.register.sbyte_3);
                        product.register.sbyte_4 = (SByte)(left.register.sbyte_4 * right.register.sbyte_4);
                        product.register.sbyte_5 = (SByte)(left.register.sbyte_5 * right.register.sbyte_5);
                        product.register.sbyte_6 = (SByte)(left.register.sbyte_6 * right.register.sbyte_6);
                        product.register.sbyte_7 = (SByte)(left.register.sbyte_7 * right.register.sbyte_7);
                        product.register.sbyte_8 = (SByte)(left.register.sbyte_8 * right.register.sbyte_8);
                        product.register.sbyte_9 = (SByte)(left.register.sbyte_9 * right.register.sbyte_9);
                        product.register.sbyte_10 = (SByte)(left.register.sbyte_10 * right.register.sbyte_10);
                        product.register.sbyte_11 = (SByte)(left.register.sbyte_11 * right.register.sbyte_11);
                        product.register.sbyte_12 = (SByte)(left.register.sbyte_12 * right.register.sbyte_12);
                        product.register.sbyte_13 = (SByte)(left.register.sbyte_13 * right.register.sbyte_13);
                        product.register.sbyte_14 = (SByte)(left.register.sbyte_14 * right.register.sbyte_14);
                        product.register.sbyte_15 = (SByte)(left.register.sbyte_15 * right.register.sbyte_15);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        product.register.uint16_0 = (UInt16)(left.register.uint16_0 * right.register.uint16_0);
                        product.register.uint16_1 = (UInt16)(left.register.uint16_1 * right.register.uint16_1);
                        product.register.uint16_2 = (UInt16)(left.register.uint16_2 * right.register.uint16_2);
                        product.register.uint16_3 = (UInt16)(left.register.uint16_3 * right.register.uint16_3);
                        product.register.uint16_4 = (UInt16)(left.register.uint16_4 * right.register.uint16_4);
                        product.register.uint16_5 = (UInt16)(left.register.uint16_5 * right.register.uint16_5);
                        product.register.uint16_6 = (UInt16)(left.register.uint16_6 * right.register.uint16_6);
                        product.register.uint16_7 = (UInt16)(left.register.uint16_7 * right.register.uint16_7);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        product.register.int16_0 = (Int16)(left.register.int16_0 * right.register.int16_0);
                        product.register.int16_1 = (Int16)(left.register.int16_1 * right.register.int16_1);
                        product.register.int16_2 = (Int16)(left.register.int16_2 * right.register.int16_2);
                        product.register.int16_3 = (Int16)(left.register.int16_3 * right.register.int16_3);
                        product.register.int16_4 = (Int16)(left.register.int16_4 * right.register.int16_4);
                        product.register.int16_5 = (Int16)(left.register.int16_5 * right.register.int16_5);
                        product.register.int16_6 = (Int16)(left.register.int16_6 * right.register.int16_6);
                        product.register.int16_7 = (Int16)(left.register.int16_7 * right.register.int16_7);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        product.register.uint32_0 = (UInt32)(left.register.uint32_0 * right.register.uint32_0);
                        product.register.uint32_1 = (UInt32)(left.register.uint32_1 * right.register.uint32_1);
                        product.register.uint32_2 = (UInt32)(left.register.uint32_2 * right.register.uint32_2);
                        product.register.uint32_3 = (UInt32)(left.register.uint32_3 * right.register.uint32_3);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        product.register.int32_0 = (Int32)(left.register.int32_0 * right.register.int32_0);
                        product.register.int32_1 = (Int32)(left.register.int32_1 * right.register.int32_1);
                        product.register.int32_2 = (Int32)(left.register.int32_2 * right.register.int32_2);
                        product.register.int32_3 = (Int32)(left.register.int32_3 * right.register.int32_3);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        product.register.uint64_0 = (UInt64)(left.register.uint64_0 * right.register.uint64_0);
                        product.register.uint64_1 = (UInt64)(left.register.uint64_1 * right.register.uint64_1);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        product.register.int64_0 = (Int64)(left.register.int64_0 * right.register.int64_0);
                        product.register.int64_1 = (Int64)(left.register.int64_1 * right.register.int64_1);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        product.register.single_0 = (Single)(left.register.single_0 * right.register.single_0);
                        product.register.single_1 = (Single)(left.register.single_1 * right.register.single_1);
                        product.register.single_2 = (Single)(left.register.single_2 * right.register.single_2);
                        product.register.single_3 = (Single)(left.register.single_3 * right.register.single_3);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        product.register.double_0 = (Double)(left.register.double_0 * right.register.double_0);
                        product.register.double_1 = (Double)(left.register.double_1 * right.register.double_1);
                    }
                    return product;
                }
            }
        }

        // This method is intrinsic only for certain types. It cannot access fields directly unless we are sure the context is unaccelerated.
        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <param name="factor">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector<T> operator *(Vector<T> value, T factor)
        {
            unchecked
            {
                if (Vector.IsHardwareAccelerated)
                {
                    return new Vector<T>(factor) * value;
                }
                else
                {
                    Vector<T> product = new Vector<T>();
                    if (typeof(T) == typeof(Byte))
                    {
                        product.register.byte_0 = (Byte)(value.register.byte_0 * (Byte)(object)factor);
                        product.register.byte_1 = (Byte)(value.register.byte_1 * (Byte)(object)factor);
                        product.register.byte_2 = (Byte)(value.register.byte_2 * (Byte)(object)factor);
                        product.register.byte_3 = (Byte)(value.register.byte_3 * (Byte)(object)factor);
                        product.register.byte_4 = (Byte)(value.register.byte_4 * (Byte)(object)factor);
                        product.register.byte_5 = (Byte)(value.register.byte_5 * (Byte)(object)factor);
                        product.register.byte_6 = (Byte)(value.register.byte_6 * (Byte)(object)factor);
                        product.register.byte_7 = (Byte)(value.register.byte_7 * (Byte)(object)factor);
                        product.register.byte_8 = (Byte)(value.register.byte_8 * (Byte)(object)factor);
                        product.register.byte_9 = (Byte)(value.register.byte_9 * (Byte)(object)factor);
                        product.register.byte_10 = (Byte)(value.register.byte_10 * (Byte)(object)factor);
                        product.register.byte_11 = (Byte)(value.register.byte_11 * (Byte)(object)factor);
                        product.register.byte_12 = (Byte)(value.register.byte_12 * (Byte)(object)factor);
                        product.register.byte_13 = (Byte)(value.register.byte_13 * (Byte)(object)factor);
                        product.register.byte_14 = (Byte)(value.register.byte_14 * (Byte)(object)factor);
                        product.register.byte_15 = (Byte)(value.register.byte_15 * (Byte)(object)factor);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        product.register.sbyte_0 = (SByte)(value.register.sbyte_0 * (SByte)(object)factor);
                        product.register.sbyte_1 = (SByte)(value.register.sbyte_1 * (SByte)(object)factor);
                        product.register.sbyte_2 = (SByte)(value.register.sbyte_2 * (SByte)(object)factor);
                        product.register.sbyte_3 = (SByte)(value.register.sbyte_3 * (SByte)(object)factor);
                        product.register.sbyte_4 = (SByte)(value.register.sbyte_4 * (SByte)(object)factor);
                        product.register.sbyte_5 = (SByte)(value.register.sbyte_5 * (SByte)(object)factor);
                        product.register.sbyte_6 = (SByte)(value.register.sbyte_6 * (SByte)(object)factor);
                        product.register.sbyte_7 = (SByte)(value.register.sbyte_7 * (SByte)(object)factor);
                        product.register.sbyte_8 = (SByte)(value.register.sbyte_8 * (SByte)(object)factor);
                        product.register.sbyte_9 = (SByte)(value.register.sbyte_9 * (SByte)(object)factor);
                        product.register.sbyte_10 = (SByte)(value.register.sbyte_10 * (SByte)(object)factor);
                        product.register.sbyte_11 = (SByte)(value.register.sbyte_11 * (SByte)(object)factor);
                        product.register.sbyte_12 = (SByte)(value.register.sbyte_12 * (SByte)(object)factor);
                        product.register.sbyte_13 = (SByte)(value.register.sbyte_13 * (SByte)(object)factor);
                        product.register.sbyte_14 = (SByte)(value.register.sbyte_14 * (SByte)(object)factor);
                        product.register.sbyte_15 = (SByte)(value.register.sbyte_15 * (SByte)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        product.register.uint16_0 = (UInt16)(value.register.uint16_0 * (UInt16)(object)factor);
                        product.register.uint16_1 = (UInt16)(value.register.uint16_1 * (UInt16)(object)factor);
                        product.register.uint16_2 = (UInt16)(value.register.uint16_2 * (UInt16)(object)factor);
                        product.register.uint16_3 = (UInt16)(value.register.uint16_3 * (UInt16)(object)factor);
                        product.register.uint16_4 = (UInt16)(value.register.uint16_4 * (UInt16)(object)factor);
                        product.register.uint16_5 = (UInt16)(value.register.uint16_5 * (UInt16)(object)factor);
                        product.register.uint16_6 = (UInt16)(value.register.uint16_6 * (UInt16)(object)factor);
                        product.register.uint16_7 = (UInt16)(value.register.uint16_7 * (UInt16)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        product.register.int16_0 = (Int16)(value.register.int16_0 * (Int16)(object)factor);
                        product.register.int16_1 = (Int16)(value.register.int16_1 * (Int16)(object)factor);
                        product.register.int16_2 = (Int16)(value.register.int16_2 * (Int16)(object)factor);
                        product.register.int16_3 = (Int16)(value.register.int16_3 * (Int16)(object)factor);
                        product.register.int16_4 = (Int16)(value.register.int16_4 * (Int16)(object)factor);
                        product.register.int16_5 = (Int16)(value.register.int16_5 * (Int16)(object)factor);
                        product.register.int16_6 = (Int16)(value.register.int16_6 * (Int16)(object)factor);
                        product.register.int16_7 = (Int16)(value.register.int16_7 * (Int16)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        product.register.uint32_0 = (UInt32)(value.register.uint32_0 * (UInt32)(object)factor);
                        product.register.uint32_1 = (UInt32)(value.register.uint32_1 * (UInt32)(object)factor);
                        product.register.uint32_2 = (UInt32)(value.register.uint32_2 * (UInt32)(object)factor);
                        product.register.uint32_3 = (UInt32)(value.register.uint32_3 * (UInt32)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        product.register.int32_0 = (Int32)(value.register.int32_0 * (Int32)(object)factor);
                        product.register.int32_1 = (Int32)(value.register.int32_1 * (Int32)(object)factor);
                        product.register.int32_2 = (Int32)(value.register.int32_2 * (Int32)(object)factor);
                        product.register.int32_3 = (Int32)(value.register.int32_3 * (Int32)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        product.register.uint64_0 = (UInt64)(value.register.uint64_0 * (UInt64)(object)factor);
                        product.register.uint64_1 = (UInt64)(value.register.uint64_1 * (UInt64)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        product.register.int64_0 = (Int64)(value.register.int64_0 * (Int64)(object)factor);
                        product.register.int64_1 = (Int64)(value.register.int64_1 * (Int64)(object)factor);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        product.register.single_0 = (Single)(value.register.single_0 * (Single)(object)factor);
                        product.register.single_1 = (Single)(value.register.single_1 * (Single)(object)factor);
                        product.register.single_2 = (Single)(value.register.single_2 * (Single)(object)factor);
                        product.register.single_3 = (Single)(value.register.single_3 * (Single)(object)factor);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        product.register.double_0 = (Double)(value.register.double_0 * (Double)(object)factor);
                        product.register.double_1 = (Double)(value.register.double_1 * (Double)(object)factor);
                    }
                    return product;
                }
            }
        }

        // This method is intrinsic only for certain types. It cannot access fields directly unless we are sure the context is unaccelerated.
        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="factor">The scalar value.</param>
        /// <param name="value">The source vector.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector<T> operator *(T factor, Vector<T> value)
        {
            unchecked
            {
                if (Vector.IsHardwareAccelerated)
                {
                    return new Vector<T>(factor) * value;
                }
                else
                {
                    Vector<T> product = new Vector<T>();
                    if (typeof(T) == typeof(Byte))
                    {
                        product.register.byte_0 = (Byte)(value.register.byte_0 * (Byte)(object)factor);
                        product.register.byte_1 = (Byte)(value.register.byte_1 * (Byte)(object)factor);
                        product.register.byte_2 = (Byte)(value.register.byte_2 * (Byte)(object)factor);
                        product.register.byte_3 = (Byte)(value.register.byte_3 * (Byte)(object)factor);
                        product.register.byte_4 = (Byte)(value.register.byte_4 * (Byte)(object)factor);
                        product.register.byte_5 = (Byte)(value.register.byte_5 * (Byte)(object)factor);
                        product.register.byte_6 = (Byte)(value.register.byte_6 * (Byte)(object)factor);
                        product.register.byte_7 = (Byte)(value.register.byte_7 * (Byte)(object)factor);
                        product.register.byte_8 = (Byte)(value.register.byte_8 * (Byte)(object)factor);
                        product.register.byte_9 = (Byte)(value.register.byte_9 * (Byte)(object)factor);
                        product.register.byte_10 = (Byte)(value.register.byte_10 * (Byte)(object)factor);
                        product.register.byte_11 = (Byte)(value.register.byte_11 * (Byte)(object)factor);
                        product.register.byte_12 = (Byte)(value.register.byte_12 * (Byte)(object)factor);
                        product.register.byte_13 = (Byte)(value.register.byte_13 * (Byte)(object)factor);
                        product.register.byte_14 = (Byte)(value.register.byte_14 * (Byte)(object)factor);
                        product.register.byte_15 = (Byte)(value.register.byte_15 * (Byte)(object)factor);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        product.register.sbyte_0 = (SByte)(value.register.sbyte_0 * (SByte)(object)factor);
                        product.register.sbyte_1 = (SByte)(value.register.sbyte_1 * (SByte)(object)factor);
                        product.register.sbyte_2 = (SByte)(value.register.sbyte_2 * (SByte)(object)factor);
                        product.register.sbyte_3 = (SByte)(value.register.sbyte_3 * (SByte)(object)factor);
                        product.register.sbyte_4 = (SByte)(value.register.sbyte_4 * (SByte)(object)factor);
                        product.register.sbyte_5 = (SByte)(value.register.sbyte_5 * (SByte)(object)factor);
                        product.register.sbyte_6 = (SByte)(value.register.sbyte_6 * (SByte)(object)factor);
                        product.register.sbyte_7 = (SByte)(value.register.sbyte_7 * (SByte)(object)factor);
                        product.register.sbyte_8 = (SByte)(value.register.sbyte_8 * (SByte)(object)factor);
                        product.register.sbyte_9 = (SByte)(value.register.sbyte_9 * (SByte)(object)factor);
                        product.register.sbyte_10 = (SByte)(value.register.sbyte_10 * (SByte)(object)factor);
                        product.register.sbyte_11 = (SByte)(value.register.sbyte_11 * (SByte)(object)factor);
                        product.register.sbyte_12 = (SByte)(value.register.sbyte_12 * (SByte)(object)factor);
                        product.register.sbyte_13 = (SByte)(value.register.sbyte_13 * (SByte)(object)factor);
                        product.register.sbyte_14 = (SByte)(value.register.sbyte_14 * (SByte)(object)factor);
                        product.register.sbyte_15 = (SByte)(value.register.sbyte_15 * (SByte)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        product.register.uint16_0 = (UInt16)(value.register.uint16_0 * (UInt16)(object)factor);
                        product.register.uint16_1 = (UInt16)(value.register.uint16_1 * (UInt16)(object)factor);
                        product.register.uint16_2 = (UInt16)(value.register.uint16_2 * (UInt16)(object)factor);
                        product.register.uint16_3 = (UInt16)(value.register.uint16_3 * (UInt16)(object)factor);
                        product.register.uint16_4 = (UInt16)(value.register.uint16_4 * (UInt16)(object)factor);
                        product.register.uint16_5 = (UInt16)(value.register.uint16_5 * (UInt16)(object)factor);
                        product.register.uint16_6 = (UInt16)(value.register.uint16_6 * (UInt16)(object)factor);
                        product.register.uint16_7 = (UInt16)(value.register.uint16_7 * (UInt16)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        product.register.int16_0 = (Int16)(value.register.int16_0 * (Int16)(object)factor);
                        product.register.int16_1 = (Int16)(value.register.int16_1 * (Int16)(object)factor);
                        product.register.int16_2 = (Int16)(value.register.int16_2 * (Int16)(object)factor);
                        product.register.int16_3 = (Int16)(value.register.int16_3 * (Int16)(object)factor);
                        product.register.int16_4 = (Int16)(value.register.int16_4 * (Int16)(object)factor);
                        product.register.int16_5 = (Int16)(value.register.int16_5 * (Int16)(object)factor);
                        product.register.int16_6 = (Int16)(value.register.int16_6 * (Int16)(object)factor);
                        product.register.int16_7 = (Int16)(value.register.int16_7 * (Int16)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        product.register.uint32_0 = (UInt32)(value.register.uint32_0 * (UInt32)(object)factor);
                        product.register.uint32_1 = (UInt32)(value.register.uint32_1 * (UInt32)(object)factor);
                        product.register.uint32_2 = (UInt32)(value.register.uint32_2 * (UInt32)(object)factor);
                        product.register.uint32_3 = (UInt32)(value.register.uint32_3 * (UInt32)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        product.register.int32_0 = (Int32)(value.register.int32_0 * (Int32)(object)factor);
                        product.register.int32_1 = (Int32)(value.register.int32_1 * (Int32)(object)factor);
                        product.register.int32_2 = (Int32)(value.register.int32_2 * (Int32)(object)factor);
                        product.register.int32_3 = (Int32)(value.register.int32_3 * (Int32)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        product.register.uint64_0 = (UInt64)(value.register.uint64_0 * (UInt64)(object)factor);
                        product.register.uint64_1 = (UInt64)(value.register.uint64_1 * (UInt64)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        product.register.int64_0 = (Int64)(value.register.int64_0 * (Int64)(object)factor);
                        product.register.int64_1 = (Int64)(value.register.int64_1 * (Int64)(object)factor);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        product.register.single_0 = (Single)(value.register.single_0 * (Single)(object)factor);
                        product.register.single_1 = (Single)(value.register.single_1 * (Single)(object)factor);
                        product.register.single_2 = (Single)(value.register.single_2 * (Single)(object)factor);
                        product.register.single_3 = (Single)(value.register.single_3 * (Single)(object)factor);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        product.register.double_0 = (Double)(value.register.double_0 * (Double)(object)factor);
                        product.register.double_1 = (Double)(value.register.double_1 * (Double)(object)factor);
                    }
                    return product;
                }
            }
        }

        // This method is intrinsic only for certain types. It cannot access fields directly unless we are sure the context is unaccelerated.
        /// <summary>
        /// Divides the first vector by the second.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        public static unsafe Vector<T> operator /(Vector<T> left, Vector<T> right)
        {
            unchecked
            {
                if (Vector.IsHardwareAccelerated)
                {
                    if (typeof(T) == typeof(Byte))
                    {
                        Byte* dataPtr = stackalloc Byte[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Byte)(object)ScalarDivide(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        SByte* dataPtr = stackalloc SByte[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (SByte)(object)ScalarDivide(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        UInt16* dataPtr = stackalloc UInt16[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt16)(object)ScalarDivide(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        Int16* dataPtr = stackalloc Int16[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int16)(object)ScalarDivide(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        UInt32* dataPtr = stackalloc UInt32[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt32)(object)ScalarDivide(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        Int32* dataPtr = stackalloc Int32[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int32)(object)ScalarDivide(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        UInt64* dataPtr = stackalloc UInt64[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (UInt64)(object)ScalarDivide(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        Int64* dataPtr = stackalloc Int64[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Int64)(object)ScalarDivide(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        Single* dataPtr = stackalloc Single[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Single)(object)ScalarDivide(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        Double* dataPtr = stackalloc Double[Count];
                        for (int g = 0; g < Count; g++)
                        {
                            dataPtr[g] = (Double)(object)ScalarDivide(left[g], right[g]);
                        }
                        return new Vector<T>(dataPtr);
                    }
                    else
                    {
                        throw new NotSupportedException(SR.Arg_TypeNotSupported);
                    }
                }
                else
                {
                    Vector<T> quotient = new Vector<T>();
                    if (typeof(T) == typeof(Byte))
                    {
                        quotient.register.byte_0 = (Byte)(left.register.byte_0 / right.register.byte_0);
                        quotient.register.byte_1 = (Byte)(left.register.byte_1 / right.register.byte_1);
                        quotient.register.byte_2 = (Byte)(left.register.byte_2 / right.register.byte_2);
                        quotient.register.byte_3 = (Byte)(left.register.byte_3 / right.register.byte_3);
                        quotient.register.byte_4 = (Byte)(left.register.byte_4 / right.register.byte_4);
                        quotient.register.byte_5 = (Byte)(left.register.byte_5 / right.register.byte_5);
                        quotient.register.byte_6 = (Byte)(left.register.byte_6 / right.register.byte_6);
                        quotient.register.byte_7 = (Byte)(left.register.byte_7 / right.register.byte_7);
                        quotient.register.byte_8 = (Byte)(left.register.byte_8 / right.register.byte_8);
                        quotient.register.byte_9 = (Byte)(left.register.byte_9 / right.register.byte_9);
                        quotient.register.byte_10 = (Byte)(left.register.byte_10 / right.register.byte_10);
                        quotient.register.byte_11 = (Byte)(left.register.byte_11 / right.register.byte_11);
                        quotient.register.byte_12 = (Byte)(left.register.byte_12 / right.register.byte_12);
                        quotient.register.byte_13 = (Byte)(left.register.byte_13 / right.register.byte_13);
                        quotient.register.byte_14 = (Byte)(left.register.byte_14 / right.register.byte_14);
                        quotient.register.byte_15 = (Byte)(left.register.byte_15 / right.register.byte_15);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        quotient.register.sbyte_0 = (SByte)(left.register.sbyte_0 / right.register.sbyte_0);
                        quotient.register.sbyte_1 = (SByte)(left.register.sbyte_1 / right.register.sbyte_1);
                        quotient.register.sbyte_2 = (SByte)(left.register.sbyte_2 / right.register.sbyte_2);
                        quotient.register.sbyte_3 = (SByte)(left.register.sbyte_3 / right.register.sbyte_3);
                        quotient.register.sbyte_4 = (SByte)(left.register.sbyte_4 / right.register.sbyte_4);
                        quotient.register.sbyte_5 = (SByte)(left.register.sbyte_5 / right.register.sbyte_5);
                        quotient.register.sbyte_6 = (SByte)(left.register.sbyte_6 / right.register.sbyte_6);
                        quotient.register.sbyte_7 = (SByte)(left.register.sbyte_7 / right.register.sbyte_7);
                        quotient.register.sbyte_8 = (SByte)(left.register.sbyte_8 / right.register.sbyte_8);
                        quotient.register.sbyte_9 = (SByte)(left.register.sbyte_9 / right.register.sbyte_9);
                        quotient.register.sbyte_10 = (SByte)(left.register.sbyte_10 / right.register.sbyte_10);
                        quotient.register.sbyte_11 = (SByte)(left.register.sbyte_11 / right.register.sbyte_11);
                        quotient.register.sbyte_12 = (SByte)(left.register.sbyte_12 / right.register.sbyte_12);
                        quotient.register.sbyte_13 = (SByte)(left.register.sbyte_13 / right.register.sbyte_13);
                        quotient.register.sbyte_14 = (SByte)(left.register.sbyte_14 / right.register.sbyte_14);
                        quotient.register.sbyte_15 = (SByte)(left.register.sbyte_15 / right.register.sbyte_15);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        quotient.register.uint16_0 = (UInt16)(left.register.uint16_0 / right.register.uint16_0);
                        quotient.register.uint16_1 = (UInt16)(left.register.uint16_1 / right.register.uint16_1);
                        quotient.register.uint16_2 = (UInt16)(left.register.uint16_2 / right.register.uint16_2);
                        quotient.register.uint16_3 = (UInt16)(left.register.uint16_3 / right.register.uint16_3);
                        quotient.register.uint16_4 = (UInt16)(left.register.uint16_4 / right.register.uint16_4);
                        quotient.register.uint16_5 = (UInt16)(left.register.uint16_5 / right.register.uint16_5);
                        quotient.register.uint16_6 = (UInt16)(left.register.uint16_6 / right.register.uint16_6);
                        quotient.register.uint16_7 = (UInt16)(left.register.uint16_7 / right.register.uint16_7);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        quotient.register.int16_0 = (Int16)(left.register.int16_0 / right.register.int16_0);
                        quotient.register.int16_1 = (Int16)(left.register.int16_1 / right.register.int16_1);
                        quotient.register.int16_2 = (Int16)(left.register.int16_2 / right.register.int16_2);
                        quotient.register.int16_3 = (Int16)(left.register.int16_3 / right.register.int16_3);
                        quotient.register.int16_4 = (Int16)(left.register.int16_4 / right.register.int16_4);
                        quotient.register.int16_5 = (Int16)(left.register.int16_5 / right.register.int16_5);
                        quotient.register.int16_6 = (Int16)(left.register.int16_6 / right.register.int16_6);
                        quotient.register.int16_7 = (Int16)(left.register.int16_7 / right.register.int16_7);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        quotient.register.uint32_0 = (UInt32)(left.register.uint32_0 / right.register.uint32_0);
                        quotient.register.uint32_1 = (UInt32)(left.register.uint32_1 / right.register.uint32_1);
                        quotient.register.uint32_2 = (UInt32)(left.register.uint32_2 / right.register.uint32_2);
                        quotient.register.uint32_3 = (UInt32)(left.register.uint32_3 / right.register.uint32_3);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        quotient.register.int32_0 = (Int32)(left.register.int32_0 / right.register.int32_0);
                        quotient.register.int32_1 = (Int32)(left.register.int32_1 / right.register.int32_1);
                        quotient.register.int32_2 = (Int32)(left.register.int32_2 / right.register.int32_2);
                        quotient.register.int32_3 = (Int32)(left.register.int32_3 / right.register.int32_3);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        quotient.register.uint64_0 = (UInt64)(left.register.uint64_0 / right.register.uint64_0);
                        quotient.register.uint64_1 = (UInt64)(left.register.uint64_1 / right.register.uint64_1);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        quotient.register.int64_0 = (Int64)(left.register.int64_0 / right.register.int64_0);
                        quotient.register.int64_1 = (Int64)(left.register.int64_1 / right.register.int64_1);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        quotient.register.single_0 = (Single)(left.register.single_0 / right.register.single_0);
                        quotient.register.single_1 = (Single)(left.register.single_1 / right.register.single_1);
                        quotient.register.single_2 = (Single)(left.register.single_2 / right.register.single_2);
                        quotient.register.single_3 = (Single)(left.register.single_3 / right.register.single_3);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        quotient.register.double_0 = (Double)(left.register.double_0 / right.register.double_0);
                        quotient.register.double_1 = (Double)(left.register.double_1 / right.register.double_1);
                    }
                    return quotient;
                }
            }
        }

        /// <summary>
        /// Negates a given vector.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        public static Vector<T> operator -(Vector<T> value)
        {
            return Zero - value;
        }
        #endregion Arithmetic Operators

        #region Bitwise Operators
        /// <summary>
        /// Returns a new vector by performing a bitwise-and operation on each of the elements in the given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The resultant vector.</returns>
        [JitIntrinsic]
        public static unsafe Vector<T> operator &(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = new Vector<T>();
            unchecked
            {
                if (Vector.IsHardwareAccelerated)
                {
                    Int64* resultBase = &result.register.int64_0;
                    Int64* leftBase = &left.register.int64_0;
                    Int64* rightBase = &right.register.int64_0;
                    for (int g = 0; g < Vector<Int64>.Count; g++)
                    {
                        resultBase[g] = leftBase[g] & rightBase[g];
                    }
                }
                else
                {
                    result.register.int64_0 = left.register.int64_0 & right.register.int64_0;
                    result.register.int64_1 = left.register.int64_1 & right.register.int64_1;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a new vector by performing a bitwise-or operation on each of the elements in the given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The resultant vector.</returns>
        [JitIntrinsic]
        public static unsafe Vector<T> operator |(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = new Vector<T>();
            unchecked
            {
                if (Vector.IsHardwareAccelerated)
                {
                    Int64* resultBase = &result.register.int64_0;
                    Int64* leftBase = &left.register.int64_0;
                    Int64* rightBase = &right.register.int64_0;
                    for (int g = 0; g < Vector<Int64>.Count; g++)
                    {
                        resultBase[g] = leftBase[g] | rightBase[g];
                    }
                }
                else
                {
                    result.register.int64_0 = left.register.int64_0 | right.register.int64_0;
                    result.register.int64_1 = left.register.int64_1 | right.register.int64_1;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a new vector by performing a bitwise-exclusive-or operation on each of the elements in the given vectors.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The resultant vector.</returns>
        [JitIntrinsic]
        public static unsafe Vector<T> operator ^(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = new Vector<T>();
            unchecked
            {
                if (Vector.IsHardwareAccelerated)
                {
                    Int64* resultBase = &result.register.int64_0;
                    Int64* leftBase = &left.register.int64_0;
                    Int64* rightBase = &right.register.int64_0;
                    for (int g = 0; g < Vector<Int64>.Count; g++)
                    {
                        resultBase[g] = leftBase[g] ^ rightBase[g];
                    }
                }
                else
                {
                    result.register.int64_0 = left.register.int64_0 ^ right.register.int64_0;
                    result.register.int64_1 = left.register.int64_1 ^ right.register.int64_1;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a new vector whose elements are obtained by taking the one's complement of the given vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The one's complement vector.</returns>
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator ~(Vector<T> value)
        {
            return allOnes ^ value;
        }
        #endregion Bitwise Operators

        #region Logical Operators
        /// <summary>
        /// Returns a boolean indicating whether each pair of elements in the given vectors are equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The first vector to compare.</param>
        /// <returns>True if all elements are equal; False otherwise.</returns>
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector<T> left, Vector<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns a boolean indicating whether any single pair of elements in the given vectors are not equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if left and right are not equal; False otherwise.</returns>
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector<T> left, Vector<T> right)
        {
            return !(left == right);
        }
        #endregion Logical Operators

        #region Conversions
        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [JitIntrinsic]
        public static explicit operator Vector<Byte>(Vector<T> value)
        {
            return new Vector<Byte>(ref value.register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static explicit operator Vector<SByte>(Vector<T> value)
        {
            return new Vector<SByte>(ref value.register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static explicit operator Vector<UInt16>(Vector<T> value)
        {
            return new Vector<UInt16>(ref value.register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [JitIntrinsic]
        public static explicit operator Vector<Int16>(Vector<T> value)
        {
            return new Vector<Int16>(ref value.register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static explicit operator Vector<UInt32>(Vector<T> value)
        {
            return new Vector<UInt32>(ref value.register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [JitIntrinsic]
        public static explicit operator Vector<Int32>(Vector<T> value)
        {
            return new Vector<Int32>(ref value.register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static explicit operator Vector<UInt64>(Vector<T> value)
        {
            return new Vector<UInt64>(ref value.register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [JitIntrinsic]
        public static explicit operator Vector<Int64>(Vector<T> value)
        {
            return new Vector<Int64>(ref value.register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [JitIntrinsic]
        public static explicit operator Vector<Single>(Vector<T> value)
        {
            return new Vector<Single>(ref value.register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [JitIntrinsic]
        public static explicit operator Vector<Double>(Vector<T> value)
        {
            return new Vector<Double>(ref value.register);
        }

        #endregion Conversions

        #region Internal Comparison Methods
        [JitIntrinsic]
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        internal static unsafe Vector<T> Equals(Vector<T> left, Vector<T> right)
        {
            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(Byte))
                {
                    Byte* dataPtr = stackalloc Byte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarEquals(left[g], right[g]) ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte* dataPtr = stackalloc SByte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarEquals(left[g], right[g]) ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16* dataPtr = stackalloc UInt16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarEquals(left[g], right[g]) ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16* dataPtr = stackalloc Int16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarEquals(left[g], right[g]) ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32* dataPtr = stackalloc UInt32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarEquals(left[g], right[g]) ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32* dataPtr = stackalloc Int32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarEquals(left[g], right[g]) ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64* dataPtr = stackalloc UInt64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarEquals(left[g], right[g]) ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64* dataPtr = stackalloc Int64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarEquals(left[g], right[g]) ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single* dataPtr = stackalloc Single[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarEquals(left[g], right[g]) ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double* dataPtr = stackalloc Double[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarEquals(left[g], right[g]) ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
            else
            {
                Register register = new Register();
                if (typeof(T) == typeof(Byte))
                {
                    register.byte_0 = left.register.byte_0 == right.register.byte_0 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_1 = left.register.byte_1 == right.register.byte_1 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_2 = left.register.byte_2 == right.register.byte_2 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_3 = left.register.byte_3 == right.register.byte_3 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_4 = left.register.byte_4 == right.register.byte_4 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_5 = left.register.byte_5 == right.register.byte_5 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_6 = left.register.byte_6 == right.register.byte_6 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_7 = left.register.byte_7 == right.register.byte_7 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_8 = left.register.byte_8 == right.register.byte_8 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_9 = left.register.byte_9 == right.register.byte_9 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_10 = left.register.byte_10 == right.register.byte_10 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_11 = left.register.byte_11 == right.register.byte_11 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_12 = left.register.byte_12 == right.register.byte_12 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_13 = left.register.byte_13 == right.register.byte_13 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_14 = left.register.byte_14 == right.register.byte_14 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_15 = left.register.byte_15 == right.register.byte_15 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    register.sbyte_0 = left.register.sbyte_0 == right.register.sbyte_0 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_1 = left.register.sbyte_1 == right.register.sbyte_1 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_2 = left.register.sbyte_2 == right.register.sbyte_2 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_3 = left.register.sbyte_3 == right.register.sbyte_3 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_4 = left.register.sbyte_4 == right.register.sbyte_4 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_5 = left.register.sbyte_5 == right.register.sbyte_5 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_6 = left.register.sbyte_6 == right.register.sbyte_6 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_7 = left.register.sbyte_7 == right.register.sbyte_7 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_8 = left.register.sbyte_8 == right.register.sbyte_8 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_9 = left.register.sbyte_9 == right.register.sbyte_9 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_10 = left.register.sbyte_10 == right.register.sbyte_10 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_11 = left.register.sbyte_11 == right.register.sbyte_11 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_12 = left.register.sbyte_12 == right.register.sbyte_12 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_13 = left.register.sbyte_13 == right.register.sbyte_13 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_14 = left.register.sbyte_14 == right.register.sbyte_14 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_15 = left.register.sbyte_15 == right.register.sbyte_15 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    register.uint16_0 = left.register.uint16_0 == right.register.uint16_0 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_1 = left.register.uint16_1 == right.register.uint16_1 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_2 = left.register.uint16_2 == right.register.uint16_2 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_3 = left.register.uint16_3 == right.register.uint16_3 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_4 = left.register.uint16_4 == right.register.uint16_4 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_5 = left.register.uint16_5 == right.register.uint16_5 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_6 = left.register.uint16_6 == right.register.uint16_6 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_7 = left.register.uint16_7 == right.register.uint16_7 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    register.int16_0 = left.register.int16_0 == right.register.int16_0 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_1 = left.register.int16_1 == right.register.int16_1 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_2 = left.register.int16_2 == right.register.int16_2 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_3 = left.register.int16_3 == right.register.int16_3 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_4 = left.register.int16_4 == right.register.int16_4 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_5 = left.register.int16_5 == right.register.int16_5 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_6 = left.register.int16_6 == right.register.int16_6 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_7 = left.register.int16_7 == right.register.int16_7 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    register.uint32_0 = left.register.uint32_0 == right.register.uint32_0 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_1 = left.register.uint32_1 == right.register.uint32_1 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_2 = left.register.uint32_2 == right.register.uint32_2 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_3 = left.register.uint32_3 == right.register.uint32_3 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    register.int32_0 = left.register.int32_0 == right.register.int32_0 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_1 = left.register.int32_1 == right.register.int32_1 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_2 = left.register.int32_2 == right.register.int32_2 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_3 = left.register.int32_3 == right.register.int32_3 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    register.uint64_0 = left.register.uint64_0 == right.register.uint64_0 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    register.uint64_1 = left.register.uint64_1 == right.register.uint64_1 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    register.int64_0 = left.register.int64_0 == right.register.int64_0 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    register.int64_1 = left.register.int64_1 == right.register.int64_1 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Single))
                {
                    register.single_0 = left.register.single_0 == right.register.single_0 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_1 = left.register.single_1 == right.register.single_1 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_2 = left.register.single_2 == right.register.single_2 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_3 = left.register.single_3 == right.register.single_3 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Double))
                {
                    register.double_0 = left.register.double_0 == right.register.double_0 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    register.double_1 = left.register.double_1 == right.register.double_1 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    return new Vector<T>(ref register);
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
        }

        [JitIntrinsic]
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        internal static unsafe Vector<T> LessThan(Vector<T> left, Vector<T> right)
        {
            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(Byte))
                {
                    Byte* dataPtr = stackalloc Byte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte* dataPtr = stackalloc SByte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16* dataPtr = stackalloc UInt16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16* dataPtr = stackalloc Int16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32* dataPtr = stackalloc UInt32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32* dataPtr = stackalloc Int32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64* dataPtr = stackalloc UInt64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64* dataPtr = stackalloc Int64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single* dataPtr = stackalloc Single[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double* dataPtr = stackalloc Double[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
            else
            {
                Register register = new Register();
                if (typeof(T) == typeof(Byte))
                {
                    register.byte_0 = left.register.byte_0 < right.register.byte_0 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_1 = left.register.byte_1 < right.register.byte_1 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_2 = left.register.byte_2 < right.register.byte_2 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_3 = left.register.byte_3 < right.register.byte_3 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_4 = left.register.byte_4 < right.register.byte_4 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_5 = left.register.byte_5 < right.register.byte_5 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_6 = left.register.byte_6 < right.register.byte_6 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_7 = left.register.byte_7 < right.register.byte_7 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_8 = left.register.byte_8 < right.register.byte_8 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_9 = left.register.byte_9 < right.register.byte_9 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_10 = left.register.byte_10 < right.register.byte_10 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_11 = left.register.byte_11 < right.register.byte_11 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_12 = left.register.byte_12 < right.register.byte_12 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_13 = left.register.byte_13 < right.register.byte_13 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_14 = left.register.byte_14 < right.register.byte_14 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_15 = left.register.byte_15 < right.register.byte_15 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    register.sbyte_0 = left.register.sbyte_0 < right.register.sbyte_0 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_1 = left.register.sbyte_1 < right.register.sbyte_1 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_2 = left.register.sbyte_2 < right.register.sbyte_2 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_3 = left.register.sbyte_3 < right.register.sbyte_3 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_4 = left.register.sbyte_4 < right.register.sbyte_4 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_5 = left.register.sbyte_5 < right.register.sbyte_5 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_6 = left.register.sbyte_6 < right.register.sbyte_6 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_7 = left.register.sbyte_7 < right.register.sbyte_7 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_8 = left.register.sbyte_8 < right.register.sbyte_8 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_9 = left.register.sbyte_9 < right.register.sbyte_9 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_10 = left.register.sbyte_10 < right.register.sbyte_10 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_11 = left.register.sbyte_11 < right.register.sbyte_11 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_12 = left.register.sbyte_12 < right.register.sbyte_12 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_13 = left.register.sbyte_13 < right.register.sbyte_13 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_14 = left.register.sbyte_14 < right.register.sbyte_14 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_15 = left.register.sbyte_15 < right.register.sbyte_15 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    register.uint16_0 = left.register.uint16_0 < right.register.uint16_0 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_1 = left.register.uint16_1 < right.register.uint16_1 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_2 = left.register.uint16_2 < right.register.uint16_2 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_3 = left.register.uint16_3 < right.register.uint16_3 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_4 = left.register.uint16_4 < right.register.uint16_4 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_5 = left.register.uint16_5 < right.register.uint16_5 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_6 = left.register.uint16_6 < right.register.uint16_6 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_7 = left.register.uint16_7 < right.register.uint16_7 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    register.int16_0 = left.register.int16_0 < right.register.int16_0 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_1 = left.register.int16_1 < right.register.int16_1 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_2 = left.register.int16_2 < right.register.int16_2 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_3 = left.register.int16_3 < right.register.int16_3 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_4 = left.register.int16_4 < right.register.int16_4 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_5 = left.register.int16_5 < right.register.int16_5 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_6 = left.register.int16_6 < right.register.int16_6 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_7 = left.register.int16_7 < right.register.int16_7 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    register.uint32_0 = left.register.uint32_0 < right.register.uint32_0 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_1 = left.register.uint32_1 < right.register.uint32_1 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_2 = left.register.uint32_2 < right.register.uint32_2 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_3 = left.register.uint32_3 < right.register.uint32_3 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    register.int32_0 = left.register.int32_0 < right.register.int32_0 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_1 = left.register.int32_1 < right.register.int32_1 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_2 = left.register.int32_2 < right.register.int32_2 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_3 = left.register.int32_3 < right.register.int32_3 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    register.uint64_0 = left.register.uint64_0 < right.register.uint64_0 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    register.uint64_1 = left.register.uint64_1 < right.register.uint64_1 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    register.int64_0 = left.register.int64_0 < right.register.int64_0 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    register.int64_1 = left.register.int64_1 < right.register.int64_1 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Single))
                {
                    register.single_0 = left.register.single_0 < right.register.single_0 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_1 = left.register.single_1 < right.register.single_1 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_2 = left.register.single_2 < right.register.single_2 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_3 = left.register.single_3 < right.register.single_3 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Double))
                {
                    register.double_0 = left.register.double_0 < right.register.double_0 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    register.double_1 = left.register.double_1 < right.register.double_1 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    return new Vector<T>(ref register);
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
        }

        [JitIntrinsic]
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        internal static unsafe Vector<T> GreaterThan(Vector<T> left, Vector<T> right)
        {
            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(Byte))
                {
                    Byte* dataPtr = stackalloc Byte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte* dataPtr = stackalloc SByte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16* dataPtr = stackalloc UInt16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16* dataPtr = stackalloc Int16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32* dataPtr = stackalloc UInt32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32* dataPtr = stackalloc Int32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64* dataPtr = stackalloc UInt64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64* dataPtr = stackalloc Int64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single* dataPtr = stackalloc Single[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double* dataPtr = stackalloc Double[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    }
                    return new Vector<T>(dataPtr);
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
            else
            {
                Register register = new Register();
                if (typeof(T) == typeof(Byte))
                {
                    register.byte_0 = left.register.byte_0 > right.register.byte_0 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_1 = left.register.byte_1 > right.register.byte_1 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_2 = left.register.byte_2 > right.register.byte_2 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_3 = left.register.byte_3 > right.register.byte_3 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_4 = left.register.byte_4 > right.register.byte_4 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_5 = left.register.byte_5 > right.register.byte_5 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_6 = left.register.byte_6 > right.register.byte_6 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_7 = left.register.byte_7 > right.register.byte_7 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_8 = left.register.byte_8 > right.register.byte_8 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_9 = left.register.byte_9 > right.register.byte_9 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_10 = left.register.byte_10 > right.register.byte_10 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_11 = left.register.byte_11 > right.register.byte_11 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_12 = left.register.byte_12 > right.register.byte_12 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_13 = left.register.byte_13 > right.register.byte_13 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_14 = left.register.byte_14 > right.register.byte_14 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_15 = left.register.byte_15 > right.register.byte_15 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    register.sbyte_0 = left.register.sbyte_0 > right.register.sbyte_0 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_1 = left.register.sbyte_1 > right.register.sbyte_1 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_2 = left.register.sbyte_2 > right.register.sbyte_2 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_3 = left.register.sbyte_3 > right.register.sbyte_3 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_4 = left.register.sbyte_4 > right.register.sbyte_4 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_5 = left.register.sbyte_5 > right.register.sbyte_5 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_6 = left.register.sbyte_6 > right.register.sbyte_6 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_7 = left.register.sbyte_7 > right.register.sbyte_7 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_8 = left.register.sbyte_8 > right.register.sbyte_8 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_9 = left.register.sbyte_9 > right.register.sbyte_9 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_10 = left.register.sbyte_10 > right.register.sbyte_10 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_11 = left.register.sbyte_11 > right.register.sbyte_11 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_12 = left.register.sbyte_12 > right.register.sbyte_12 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_13 = left.register.sbyte_13 > right.register.sbyte_13 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_14 = left.register.sbyte_14 > right.register.sbyte_14 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_15 = left.register.sbyte_15 > right.register.sbyte_15 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    register.uint16_0 = left.register.uint16_0 > right.register.uint16_0 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_1 = left.register.uint16_1 > right.register.uint16_1 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_2 = left.register.uint16_2 > right.register.uint16_2 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_3 = left.register.uint16_3 > right.register.uint16_3 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_4 = left.register.uint16_4 > right.register.uint16_4 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_5 = left.register.uint16_5 > right.register.uint16_5 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_6 = left.register.uint16_6 > right.register.uint16_6 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_7 = left.register.uint16_7 > right.register.uint16_7 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    register.int16_0 = left.register.int16_0 > right.register.int16_0 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_1 = left.register.int16_1 > right.register.int16_1 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_2 = left.register.int16_2 > right.register.int16_2 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_3 = left.register.int16_3 > right.register.int16_3 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_4 = left.register.int16_4 > right.register.int16_4 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_5 = left.register.int16_5 > right.register.int16_5 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_6 = left.register.int16_6 > right.register.int16_6 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_7 = left.register.int16_7 > right.register.int16_7 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    register.uint32_0 = left.register.uint32_0 > right.register.uint32_0 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_1 = left.register.uint32_1 > right.register.uint32_1 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_2 = left.register.uint32_2 > right.register.uint32_2 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_3 = left.register.uint32_3 > right.register.uint32_3 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    register.int32_0 = left.register.int32_0 > right.register.int32_0 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_1 = left.register.int32_1 > right.register.int32_1 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_2 = left.register.int32_2 > right.register.int32_2 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_3 = left.register.int32_3 > right.register.int32_3 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    register.uint64_0 = left.register.uint64_0 > right.register.uint64_0 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    register.uint64_1 = left.register.uint64_1 > right.register.uint64_1 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    register.int64_0 = left.register.int64_0 > right.register.int64_0 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    register.int64_1 = left.register.int64_1 > right.register.int64_1 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Single))
                {
                    register.single_0 = left.register.single_0 > right.register.single_0 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_1 = left.register.single_1 > right.register.single_1 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_2 = left.register.single_2 > right.register.single_2 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_3 = left.register.single_3 > right.register.single_3 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Double))
                {
                    register.double_0 = left.register.double_0 > right.register.double_0 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    register.double_1 = left.register.double_1 > right.register.double_1 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    return new Vector<T>(ref register);
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
        }

        [JitIntrinsic]
        internal static Vector<T> GreaterThanOrEqual(Vector<T> left, Vector<T> right)
        {
            return Equals(left, right) | GreaterThan(left, right);
        }

        [JitIntrinsic]
        internal static Vector<T> LessThanOrEqual(Vector<T> left, Vector<T> right)
        {
            return Equals(left, right) | LessThan(left, right);
        }

        [JitIntrinsic]
        internal static Vector<T> ConditionalSelect(Vector<T> condition, Vector<T> left, Vector<T> right)
        {
            return (left & condition) | (Vector.AndNot(right, condition));
        }
        #endregion Comparison Methods

        #region Internal Math Methods
        [JitIntrinsic]
        internal static unsafe Vector<T> Abs(Vector<T> value)
        {
            if (typeof(T) == typeof(Byte))
            {
                return value;
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return value;
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return value;
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return value;
            }
            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(SByte))
                {
                    SByte* dataPtr = stackalloc SByte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (SByte)(object)(Math.Abs((SByte)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16* dataPtr = stackalloc Int16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (Int16)(object)(Math.Abs((Int16)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32* dataPtr = stackalloc Int32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (Int32)(object)(Math.Abs((Int32)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64* dataPtr = stackalloc Int64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (Int64)(object)(Math.Abs((Int64)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single* dataPtr = stackalloc Single[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (Single)(object)(Math.Abs((Single)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double* dataPtr = stackalloc Double[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (Double)(object)(Math.Abs((Double)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
            else
            {
                if (typeof(T) == typeof(SByte))
                {
                    value.register.sbyte_0 = (SByte)(Math.Abs(value.register.sbyte_0));
                    value.register.sbyte_1 = (SByte)(Math.Abs(value.register.sbyte_1));
                    value.register.sbyte_2 = (SByte)(Math.Abs(value.register.sbyte_2));
                    value.register.sbyte_3 = (SByte)(Math.Abs(value.register.sbyte_3));
                    value.register.sbyte_4 = (SByte)(Math.Abs(value.register.sbyte_4));
                    value.register.sbyte_5 = (SByte)(Math.Abs(value.register.sbyte_5));
                    value.register.sbyte_6 = (SByte)(Math.Abs(value.register.sbyte_6));
                    value.register.sbyte_7 = (SByte)(Math.Abs(value.register.sbyte_7));
                    value.register.sbyte_8 = (SByte)(Math.Abs(value.register.sbyte_8));
                    value.register.sbyte_9 = (SByte)(Math.Abs(value.register.sbyte_9));
                    value.register.sbyte_10 = (SByte)(Math.Abs(value.register.sbyte_10));
                    value.register.sbyte_11 = (SByte)(Math.Abs(value.register.sbyte_11));
                    value.register.sbyte_12 = (SByte)(Math.Abs(value.register.sbyte_12));
                    value.register.sbyte_13 = (SByte)(Math.Abs(value.register.sbyte_13));
                    value.register.sbyte_14 = (SByte)(Math.Abs(value.register.sbyte_14));
                    value.register.sbyte_15 = (SByte)(Math.Abs(value.register.sbyte_15));
                    return value;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    value.register.int16_0 = (Int16)(Math.Abs(value.register.int16_0));
                    value.register.int16_1 = (Int16)(Math.Abs(value.register.int16_1));
                    value.register.int16_2 = (Int16)(Math.Abs(value.register.int16_2));
                    value.register.int16_3 = (Int16)(Math.Abs(value.register.int16_3));
                    value.register.int16_4 = (Int16)(Math.Abs(value.register.int16_4));
                    value.register.int16_5 = (Int16)(Math.Abs(value.register.int16_5));
                    value.register.int16_6 = (Int16)(Math.Abs(value.register.int16_6));
                    value.register.int16_7 = (Int16)(Math.Abs(value.register.int16_7));
                    return value;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    value.register.int32_0 = (Int32)(Math.Abs(value.register.int32_0));
                    value.register.int32_1 = (Int32)(Math.Abs(value.register.int32_1));
                    value.register.int32_2 = (Int32)(Math.Abs(value.register.int32_2));
                    value.register.int32_3 = (Int32)(Math.Abs(value.register.int32_3));
                    return value;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    value.register.int64_0 = (Int64)(Math.Abs(value.register.int64_0));
                    value.register.int64_1 = (Int64)(Math.Abs(value.register.int64_1));
                    return value;
                }
                else if (typeof(T) == typeof(Single))
                {
                    value.register.single_0 = (Single)(Math.Abs(value.register.single_0));
                    value.register.single_1 = (Single)(Math.Abs(value.register.single_1));
                    value.register.single_2 = (Single)(Math.Abs(value.register.single_2));
                    value.register.single_3 = (Single)(Math.Abs(value.register.single_3));
                    return value;
                }
                else if (typeof(T) == typeof(Double))
                {
                    value.register.double_0 = (Double)(Math.Abs(value.register.double_0));
                    value.register.double_1 = (Double)(Math.Abs(value.register.double_1));
                    return value;
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
        }

        [JitIntrinsic]
        internal static unsafe Vector<T> Min(Vector<T> left, Vector<T> right)
        {
            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(Byte))
                {
                    Byte* dataPtr = stackalloc Byte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? (Byte)(object)left[g] : (Byte)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte* dataPtr = stackalloc SByte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? (SByte)(object)left[g] : (SByte)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16* dataPtr = stackalloc UInt16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? (UInt16)(object)left[g] : (UInt16)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16* dataPtr = stackalloc Int16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? (Int16)(object)left[g] : (Int16)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32* dataPtr = stackalloc UInt32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? (UInt32)(object)left[g] : (UInt32)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32* dataPtr = stackalloc Int32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? (Int32)(object)left[g] : (Int32)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64* dataPtr = stackalloc UInt64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? (UInt64)(object)left[g] : (UInt64)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64* dataPtr = stackalloc Int64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? (Int64)(object)left[g] : (Int64)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single* dataPtr = stackalloc Single[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? (Single)(object)left[g] : (Single)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double* dataPtr = stackalloc Double[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarLessThan(left[g], right[g]) ? (Double)(object)left[g] : (Double)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
            else
            {
                Vector<T> vec = new Vector<T>();
                if (typeof(T) == typeof(Byte))
                {
                    vec.register.byte_0 = left.register.byte_0 < right.register.byte_0 ? left.register.byte_0 : right.register.byte_0;
                    vec.register.byte_1 = left.register.byte_1 < right.register.byte_1 ? left.register.byte_1 : right.register.byte_1;
                    vec.register.byte_2 = left.register.byte_2 < right.register.byte_2 ? left.register.byte_2 : right.register.byte_2;
                    vec.register.byte_3 = left.register.byte_3 < right.register.byte_3 ? left.register.byte_3 : right.register.byte_3;
                    vec.register.byte_4 = left.register.byte_4 < right.register.byte_4 ? left.register.byte_4 : right.register.byte_4;
                    vec.register.byte_5 = left.register.byte_5 < right.register.byte_5 ? left.register.byte_5 : right.register.byte_5;
                    vec.register.byte_6 = left.register.byte_6 < right.register.byte_6 ? left.register.byte_6 : right.register.byte_6;
                    vec.register.byte_7 = left.register.byte_7 < right.register.byte_7 ? left.register.byte_7 : right.register.byte_7;
                    vec.register.byte_8 = left.register.byte_8 < right.register.byte_8 ? left.register.byte_8 : right.register.byte_8;
                    vec.register.byte_9 = left.register.byte_9 < right.register.byte_9 ? left.register.byte_9 : right.register.byte_9;
                    vec.register.byte_10 = left.register.byte_10 < right.register.byte_10 ? left.register.byte_10 : right.register.byte_10;
                    vec.register.byte_11 = left.register.byte_11 < right.register.byte_11 ? left.register.byte_11 : right.register.byte_11;
                    vec.register.byte_12 = left.register.byte_12 < right.register.byte_12 ? left.register.byte_12 : right.register.byte_12;
                    vec.register.byte_13 = left.register.byte_13 < right.register.byte_13 ? left.register.byte_13 : right.register.byte_13;
                    vec.register.byte_14 = left.register.byte_14 < right.register.byte_14 ? left.register.byte_14 : right.register.byte_14;
                    vec.register.byte_15 = left.register.byte_15 < right.register.byte_15 ? left.register.byte_15 : right.register.byte_15;
                    return vec;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    vec.register.sbyte_0 = left.register.sbyte_0 < right.register.sbyte_0 ? left.register.sbyte_0 : right.register.sbyte_0;
                    vec.register.sbyte_1 = left.register.sbyte_1 < right.register.sbyte_1 ? left.register.sbyte_1 : right.register.sbyte_1;
                    vec.register.sbyte_2 = left.register.sbyte_2 < right.register.sbyte_2 ? left.register.sbyte_2 : right.register.sbyte_2;
                    vec.register.sbyte_3 = left.register.sbyte_3 < right.register.sbyte_3 ? left.register.sbyte_3 : right.register.sbyte_3;
                    vec.register.sbyte_4 = left.register.sbyte_4 < right.register.sbyte_4 ? left.register.sbyte_4 : right.register.sbyte_4;
                    vec.register.sbyte_5 = left.register.sbyte_5 < right.register.sbyte_5 ? left.register.sbyte_5 : right.register.sbyte_5;
                    vec.register.sbyte_6 = left.register.sbyte_6 < right.register.sbyte_6 ? left.register.sbyte_6 : right.register.sbyte_6;
                    vec.register.sbyte_7 = left.register.sbyte_7 < right.register.sbyte_7 ? left.register.sbyte_7 : right.register.sbyte_7;
                    vec.register.sbyte_8 = left.register.sbyte_8 < right.register.sbyte_8 ? left.register.sbyte_8 : right.register.sbyte_8;
                    vec.register.sbyte_9 = left.register.sbyte_9 < right.register.sbyte_9 ? left.register.sbyte_9 : right.register.sbyte_9;
                    vec.register.sbyte_10 = left.register.sbyte_10 < right.register.sbyte_10 ? left.register.sbyte_10 : right.register.sbyte_10;
                    vec.register.sbyte_11 = left.register.sbyte_11 < right.register.sbyte_11 ? left.register.sbyte_11 : right.register.sbyte_11;
                    vec.register.sbyte_12 = left.register.sbyte_12 < right.register.sbyte_12 ? left.register.sbyte_12 : right.register.sbyte_12;
                    vec.register.sbyte_13 = left.register.sbyte_13 < right.register.sbyte_13 ? left.register.sbyte_13 : right.register.sbyte_13;
                    vec.register.sbyte_14 = left.register.sbyte_14 < right.register.sbyte_14 ? left.register.sbyte_14 : right.register.sbyte_14;
                    vec.register.sbyte_15 = left.register.sbyte_15 < right.register.sbyte_15 ? left.register.sbyte_15 : right.register.sbyte_15;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    vec.register.uint16_0 = left.register.uint16_0 < right.register.uint16_0 ? left.register.uint16_0 : right.register.uint16_0;
                    vec.register.uint16_1 = left.register.uint16_1 < right.register.uint16_1 ? left.register.uint16_1 : right.register.uint16_1;
                    vec.register.uint16_2 = left.register.uint16_2 < right.register.uint16_2 ? left.register.uint16_2 : right.register.uint16_2;
                    vec.register.uint16_3 = left.register.uint16_3 < right.register.uint16_3 ? left.register.uint16_3 : right.register.uint16_3;
                    vec.register.uint16_4 = left.register.uint16_4 < right.register.uint16_4 ? left.register.uint16_4 : right.register.uint16_4;
                    vec.register.uint16_5 = left.register.uint16_5 < right.register.uint16_5 ? left.register.uint16_5 : right.register.uint16_5;
                    vec.register.uint16_6 = left.register.uint16_6 < right.register.uint16_6 ? left.register.uint16_6 : right.register.uint16_6;
                    vec.register.uint16_7 = left.register.uint16_7 < right.register.uint16_7 ? left.register.uint16_7 : right.register.uint16_7;
                    return vec;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    vec.register.int16_0 = left.register.int16_0 < right.register.int16_0 ? left.register.int16_0 : right.register.int16_0;
                    vec.register.int16_1 = left.register.int16_1 < right.register.int16_1 ? left.register.int16_1 : right.register.int16_1;
                    vec.register.int16_2 = left.register.int16_2 < right.register.int16_2 ? left.register.int16_2 : right.register.int16_2;
                    vec.register.int16_3 = left.register.int16_3 < right.register.int16_3 ? left.register.int16_3 : right.register.int16_3;
                    vec.register.int16_4 = left.register.int16_4 < right.register.int16_4 ? left.register.int16_4 : right.register.int16_4;
                    vec.register.int16_5 = left.register.int16_5 < right.register.int16_5 ? left.register.int16_5 : right.register.int16_5;
                    vec.register.int16_6 = left.register.int16_6 < right.register.int16_6 ? left.register.int16_6 : right.register.int16_6;
                    vec.register.int16_7 = left.register.int16_7 < right.register.int16_7 ? left.register.int16_7 : right.register.int16_7;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    vec.register.uint32_0 = left.register.uint32_0 < right.register.uint32_0 ? left.register.uint32_0 : right.register.uint32_0;
                    vec.register.uint32_1 = left.register.uint32_1 < right.register.uint32_1 ? left.register.uint32_1 : right.register.uint32_1;
                    vec.register.uint32_2 = left.register.uint32_2 < right.register.uint32_2 ? left.register.uint32_2 : right.register.uint32_2;
                    vec.register.uint32_3 = left.register.uint32_3 < right.register.uint32_3 ? left.register.uint32_3 : right.register.uint32_3;
                    return vec;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    vec.register.int32_0 = left.register.int32_0 < right.register.int32_0 ? left.register.int32_0 : right.register.int32_0;
                    vec.register.int32_1 = left.register.int32_1 < right.register.int32_1 ? left.register.int32_1 : right.register.int32_1;
                    vec.register.int32_2 = left.register.int32_2 < right.register.int32_2 ? left.register.int32_2 : right.register.int32_2;
                    vec.register.int32_3 = left.register.int32_3 < right.register.int32_3 ? left.register.int32_3 : right.register.int32_3;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    vec.register.uint64_0 = left.register.uint64_0 < right.register.uint64_0 ? left.register.uint64_0 : right.register.uint64_0;
                    vec.register.uint64_1 = left.register.uint64_1 < right.register.uint64_1 ? left.register.uint64_1 : right.register.uint64_1;
                    return vec;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    vec.register.int64_0 = left.register.int64_0 < right.register.int64_0 ? left.register.int64_0 : right.register.int64_0;
                    vec.register.int64_1 = left.register.int64_1 < right.register.int64_1 ? left.register.int64_1 : right.register.int64_1;
                    return vec;
                }
                else if (typeof(T) == typeof(Single))
                {
                    vec.register.single_0 = left.register.single_0 < right.register.single_0 ? left.register.single_0 : right.register.single_0;
                    vec.register.single_1 = left.register.single_1 < right.register.single_1 ? left.register.single_1 : right.register.single_1;
                    vec.register.single_2 = left.register.single_2 < right.register.single_2 ? left.register.single_2 : right.register.single_2;
                    vec.register.single_3 = left.register.single_3 < right.register.single_3 ? left.register.single_3 : right.register.single_3;
                    return vec;
                }
                else if (typeof(T) == typeof(Double))
                {
                    vec.register.double_0 = left.register.double_0 < right.register.double_0 ? left.register.double_0 : right.register.double_0;
                    vec.register.double_1 = left.register.double_1 < right.register.double_1 ? left.register.double_1 : right.register.double_1;
                    return vec;
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
        }

        [JitIntrinsic]
        internal static unsafe Vector<T> Max(Vector<T> left, Vector<T> right)
        {
            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(Byte))
                {
                    Byte* dataPtr = stackalloc Byte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? (Byte)(object)left[g] : (Byte)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte* dataPtr = stackalloc SByte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? (SByte)(object)left[g] : (SByte)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16* dataPtr = stackalloc UInt16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? (UInt16)(object)left[g] : (UInt16)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16* dataPtr = stackalloc Int16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? (Int16)(object)left[g] : (Int16)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32* dataPtr = stackalloc UInt32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? (UInt32)(object)left[g] : (UInt32)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32* dataPtr = stackalloc Int32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? (Int32)(object)left[g] : (Int32)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64* dataPtr = stackalloc UInt64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? (UInt64)(object)left[g] : (UInt64)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64* dataPtr = stackalloc Int64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? (Int64)(object)left[g] : (Int64)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single* dataPtr = stackalloc Single[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? (Single)(object)left[g] : (Single)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double* dataPtr = stackalloc Double[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = ScalarGreaterThan(left[g], right[g]) ? (Double)(object)left[g] : (Double)(object)right[g];
                    }
                    return new Vector<T>(dataPtr);
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
            else
            {
                Vector<T> vec = new Vector<T>();
                if (typeof(T) == typeof(Byte))
                {
                    vec.register.byte_0 = left.register.byte_0 > right.register.byte_0 ? left.register.byte_0 : right.register.byte_0;
                    vec.register.byte_1 = left.register.byte_1 > right.register.byte_1 ? left.register.byte_1 : right.register.byte_1;
                    vec.register.byte_2 = left.register.byte_2 > right.register.byte_2 ? left.register.byte_2 : right.register.byte_2;
                    vec.register.byte_3 = left.register.byte_3 > right.register.byte_3 ? left.register.byte_3 : right.register.byte_3;
                    vec.register.byte_4 = left.register.byte_4 > right.register.byte_4 ? left.register.byte_4 : right.register.byte_4;
                    vec.register.byte_5 = left.register.byte_5 > right.register.byte_5 ? left.register.byte_5 : right.register.byte_5;
                    vec.register.byte_6 = left.register.byte_6 > right.register.byte_6 ? left.register.byte_6 : right.register.byte_6;
                    vec.register.byte_7 = left.register.byte_7 > right.register.byte_7 ? left.register.byte_7 : right.register.byte_7;
                    vec.register.byte_8 = left.register.byte_8 > right.register.byte_8 ? left.register.byte_8 : right.register.byte_8;
                    vec.register.byte_9 = left.register.byte_9 > right.register.byte_9 ? left.register.byte_9 : right.register.byte_9;
                    vec.register.byte_10 = left.register.byte_10 > right.register.byte_10 ? left.register.byte_10 : right.register.byte_10;
                    vec.register.byte_11 = left.register.byte_11 > right.register.byte_11 ? left.register.byte_11 : right.register.byte_11;
                    vec.register.byte_12 = left.register.byte_12 > right.register.byte_12 ? left.register.byte_12 : right.register.byte_12;
                    vec.register.byte_13 = left.register.byte_13 > right.register.byte_13 ? left.register.byte_13 : right.register.byte_13;
                    vec.register.byte_14 = left.register.byte_14 > right.register.byte_14 ? left.register.byte_14 : right.register.byte_14;
                    vec.register.byte_15 = left.register.byte_15 > right.register.byte_15 ? left.register.byte_15 : right.register.byte_15;
                    return vec;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    vec.register.sbyte_0 = left.register.sbyte_0 > right.register.sbyte_0 ? left.register.sbyte_0 : right.register.sbyte_0;
                    vec.register.sbyte_1 = left.register.sbyte_1 > right.register.sbyte_1 ? left.register.sbyte_1 : right.register.sbyte_1;
                    vec.register.sbyte_2 = left.register.sbyte_2 > right.register.sbyte_2 ? left.register.sbyte_2 : right.register.sbyte_2;
                    vec.register.sbyte_3 = left.register.sbyte_3 > right.register.sbyte_3 ? left.register.sbyte_3 : right.register.sbyte_3;
                    vec.register.sbyte_4 = left.register.sbyte_4 > right.register.sbyte_4 ? left.register.sbyte_4 : right.register.sbyte_4;
                    vec.register.sbyte_5 = left.register.sbyte_5 > right.register.sbyte_5 ? left.register.sbyte_5 : right.register.sbyte_5;
                    vec.register.sbyte_6 = left.register.sbyte_6 > right.register.sbyte_6 ? left.register.sbyte_6 : right.register.sbyte_6;
                    vec.register.sbyte_7 = left.register.sbyte_7 > right.register.sbyte_7 ? left.register.sbyte_7 : right.register.sbyte_7;
                    vec.register.sbyte_8 = left.register.sbyte_8 > right.register.sbyte_8 ? left.register.sbyte_8 : right.register.sbyte_8;
                    vec.register.sbyte_9 = left.register.sbyte_9 > right.register.sbyte_9 ? left.register.sbyte_9 : right.register.sbyte_9;
                    vec.register.sbyte_10 = left.register.sbyte_10 > right.register.sbyte_10 ? left.register.sbyte_10 : right.register.sbyte_10;
                    vec.register.sbyte_11 = left.register.sbyte_11 > right.register.sbyte_11 ? left.register.sbyte_11 : right.register.sbyte_11;
                    vec.register.sbyte_12 = left.register.sbyte_12 > right.register.sbyte_12 ? left.register.sbyte_12 : right.register.sbyte_12;
                    vec.register.sbyte_13 = left.register.sbyte_13 > right.register.sbyte_13 ? left.register.sbyte_13 : right.register.sbyte_13;
                    vec.register.sbyte_14 = left.register.sbyte_14 > right.register.sbyte_14 ? left.register.sbyte_14 : right.register.sbyte_14;
                    vec.register.sbyte_15 = left.register.sbyte_15 > right.register.sbyte_15 ? left.register.sbyte_15 : right.register.sbyte_15;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    vec.register.uint16_0 = left.register.uint16_0 > right.register.uint16_0 ? left.register.uint16_0 : right.register.uint16_0;
                    vec.register.uint16_1 = left.register.uint16_1 > right.register.uint16_1 ? left.register.uint16_1 : right.register.uint16_1;
                    vec.register.uint16_2 = left.register.uint16_2 > right.register.uint16_2 ? left.register.uint16_2 : right.register.uint16_2;
                    vec.register.uint16_3 = left.register.uint16_3 > right.register.uint16_3 ? left.register.uint16_3 : right.register.uint16_3;
                    vec.register.uint16_4 = left.register.uint16_4 > right.register.uint16_4 ? left.register.uint16_4 : right.register.uint16_4;
                    vec.register.uint16_5 = left.register.uint16_5 > right.register.uint16_5 ? left.register.uint16_5 : right.register.uint16_5;
                    vec.register.uint16_6 = left.register.uint16_6 > right.register.uint16_6 ? left.register.uint16_6 : right.register.uint16_6;
                    vec.register.uint16_7 = left.register.uint16_7 > right.register.uint16_7 ? left.register.uint16_7 : right.register.uint16_7;
                    return vec;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    vec.register.int16_0 = left.register.int16_0 > right.register.int16_0 ? left.register.int16_0 : right.register.int16_0;
                    vec.register.int16_1 = left.register.int16_1 > right.register.int16_1 ? left.register.int16_1 : right.register.int16_1;
                    vec.register.int16_2 = left.register.int16_2 > right.register.int16_2 ? left.register.int16_2 : right.register.int16_2;
                    vec.register.int16_3 = left.register.int16_3 > right.register.int16_3 ? left.register.int16_3 : right.register.int16_3;
                    vec.register.int16_4 = left.register.int16_4 > right.register.int16_4 ? left.register.int16_4 : right.register.int16_4;
                    vec.register.int16_5 = left.register.int16_5 > right.register.int16_5 ? left.register.int16_5 : right.register.int16_5;
                    vec.register.int16_6 = left.register.int16_6 > right.register.int16_6 ? left.register.int16_6 : right.register.int16_6;
                    vec.register.int16_7 = left.register.int16_7 > right.register.int16_7 ? left.register.int16_7 : right.register.int16_7;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    vec.register.uint32_0 = left.register.uint32_0 > right.register.uint32_0 ? left.register.uint32_0 : right.register.uint32_0;
                    vec.register.uint32_1 = left.register.uint32_1 > right.register.uint32_1 ? left.register.uint32_1 : right.register.uint32_1;
                    vec.register.uint32_2 = left.register.uint32_2 > right.register.uint32_2 ? left.register.uint32_2 : right.register.uint32_2;
                    vec.register.uint32_3 = left.register.uint32_3 > right.register.uint32_3 ? left.register.uint32_3 : right.register.uint32_3;
                    return vec;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    vec.register.int32_0 = left.register.int32_0 > right.register.int32_0 ? left.register.int32_0 : right.register.int32_0;
                    vec.register.int32_1 = left.register.int32_1 > right.register.int32_1 ? left.register.int32_1 : right.register.int32_1;
                    vec.register.int32_2 = left.register.int32_2 > right.register.int32_2 ? left.register.int32_2 : right.register.int32_2;
                    vec.register.int32_3 = left.register.int32_3 > right.register.int32_3 ? left.register.int32_3 : right.register.int32_3;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    vec.register.uint64_0 = left.register.uint64_0 > right.register.uint64_0 ? left.register.uint64_0 : right.register.uint64_0;
                    vec.register.uint64_1 = left.register.uint64_1 > right.register.uint64_1 ? left.register.uint64_1 : right.register.uint64_1;
                    return vec;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    vec.register.int64_0 = left.register.int64_0 > right.register.int64_0 ? left.register.int64_0 : right.register.int64_0;
                    vec.register.int64_1 = left.register.int64_1 > right.register.int64_1 ? left.register.int64_1 : right.register.int64_1;
                    return vec;
                }
                else if (typeof(T) == typeof(Single))
                {
                    vec.register.single_0 = left.register.single_0 > right.register.single_0 ? left.register.single_0 : right.register.single_0;
                    vec.register.single_1 = left.register.single_1 > right.register.single_1 ? left.register.single_1 : right.register.single_1;
                    vec.register.single_2 = left.register.single_2 > right.register.single_2 ? left.register.single_2 : right.register.single_2;
                    vec.register.single_3 = left.register.single_3 > right.register.single_3 ? left.register.single_3 : right.register.single_3;
                    return vec;
                }
                else if (typeof(T) == typeof(Double))
                {
                    vec.register.double_0 = left.register.double_0 > right.register.double_0 ? left.register.double_0 : right.register.double_0;
                    vec.register.double_1 = left.register.double_1 > right.register.double_1 ? left.register.double_1 : right.register.double_1;
                    return vec;
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
        }

        [JitIntrinsic]
        internal static T DotProduct(Vector<T> left, Vector<T> right)
        {
            if (Vector.IsHardwareAccelerated)
            {
                T product = GetZeroValue();
                for (int g = 0; g < Count; g++)
                {
                    product = ScalarAdd(product, ScalarMultiply(left[g], right[g]));
                }
                return product;
            }
            else
            {
                if (typeof(T) == typeof(Byte))
                {
                    Byte product = 0;
                    product += (Byte)(left.register.byte_0 * right.register.byte_0);
                    product += (Byte)(left.register.byte_1 * right.register.byte_1);
                    product += (Byte)(left.register.byte_2 * right.register.byte_2);
                    product += (Byte)(left.register.byte_3 * right.register.byte_3);
                    product += (Byte)(left.register.byte_4 * right.register.byte_4);
                    product += (Byte)(left.register.byte_5 * right.register.byte_5);
                    product += (Byte)(left.register.byte_6 * right.register.byte_6);
                    product += (Byte)(left.register.byte_7 * right.register.byte_7);
                    product += (Byte)(left.register.byte_8 * right.register.byte_8);
                    product += (Byte)(left.register.byte_9 * right.register.byte_9);
                    product += (Byte)(left.register.byte_10 * right.register.byte_10);
                    product += (Byte)(left.register.byte_11 * right.register.byte_11);
                    product += (Byte)(left.register.byte_12 * right.register.byte_12);
                    product += (Byte)(left.register.byte_13 * right.register.byte_13);
                    product += (Byte)(left.register.byte_14 * right.register.byte_14);
                    product += (Byte)(left.register.byte_15 * right.register.byte_15);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte product = 0;
                    product += (SByte)(left.register.sbyte_0 * right.register.sbyte_0);
                    product += (SByte)(left.register.sbyte_1 * right.register.sbyte_1);
                    product += (SByte)(left.register.sbyte_2 * right.register.sbyte_2);
                    product += (SByte)(left.register.sbyte_3 * right.register.sbyte_3);
                    product += (SByte)(left.register.sbyte_4 * right.register.sbyte_4);
                    product += (SByte)(left.register.sbyte_5 * right.register.sbyte_5);
                    product += (SByte)(left.register.sbyte_6 * right.register.sbyte_6);
                    product += (SByte)(left.register.sbyte_7 * right.register.sbyte_7);
                    product += (SByte)(left.register.sbyte_8 * right.register.sbyte_8);
                    product += (SByte)(left.register.sbyte_9 * right.register.sbyte_9);
                    product += (SByte)(left.register.sbyte_10 * right.register.sbyte_10);
                    product += (SByte)(left.register.sbyte_11 * right.register.sbyte_11);
                    product += (SByte)(left.register.sbyte_12 * right.register.sbyte_12);
                    product += (SByte)(left.register.sbyte_13 * right.register.sbyte_13);
                    product += (SByte)(left.register.sbyte_14 * right.register.sbyte_14);
                    product += (SByte)(left.register.sbyte_15 * right.register.sbyte_15);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16 product = 0;
                    product += (UInt16)(left.register.uint16_0 * right.register.uint16_0);
                    product += (UInt16)(left.register.uint16_1 * right.register.uint16_1);
                    product += (UInt16)(left.register.uint16_2 * right.register.uint16_2);
                    product += (UInt16)(left.register.uint16_3 * right.register.uint16_3);
                    product += (UInt16)(left.register.uint16_4 * right.register.uint16_4);
                    product += (UInt16)(left.register.uint16_5 * right.register.uint16_5);
                    product += (UInt16)(left.register.uint16_6 * right.register.uint16_6);
                    product += (UInt16)(left.register.uint16_7 * right.register.uint16_7);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16 product = 0;
                    product += (Int16)(left.register.int16_0 * right.register.int16_0);
                    product += (Int16)(left.register.int16_1 * right.register.int16_1);
                    product += (Int16)(left.register.int16_2 * right.register.int16_2);
                    product += (Int16)(left.register.int16_3 * right.register.int16_3);
                    product += (Int16)(left.register.int16_4 * right.register.int16_4);
                    product += (Int16)(left.register.int16_5 * right.register.int16_5);
                    product += (Int16)(left.register.int16_6 * right.register.int16_6);
                    product += (Int16)(left.register.int16_7 * right.register.int16_7);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32 product = 0;
                    product += (UInt32)(left.register.uint32_0 * right.register.uint32_0);
                    product += (UInt32)(left.register.uint32_1 * right.register.uint32_1);
                    product += (UInt32)(left.register.uint32_2 * right.register.uint32_2);
                    product += (UInt32)(left.register.uint32_3 * right.register.uint32_3);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32 product = 0;
                    product += (Int32)(left.register.int32_0 * right.register.int32_0);
                    product += (Int32)(left.register.int32_1 * right.register.int32_1);
                    product += (Int32)(left.register.int32_2 * right.register.int32_2);
                    product += (Int32)(left.register.int32_3 * right.register.int32_3);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64 product = 0;
                    product += (UInt64)(left.register.uint64_0 * right.register.uint64_0);
                    product += (UInt64)(left.register.uint64_1 * right.register.uint64_1);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64 product = 0;
                    product += (Int64)(left.register.int64_0 * right.register.int64_0);
                    product += (Int64)(left.register.int64_1 * right.register.int64_1);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single product = 0;
                    product += (Single)(left.register.single_0 * right.register.single_0);
                    product += (Single)(left.register.single_1 * right.register.single_1);
                    product += (Single)(left.register.single_2 * right.register.single_2);
                    product += (Single)(left.register.single_3 * right.register.single_3);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double product = 0;
                    product += (Double)(left.register.double_0 * right.register.double_0);
                    product += (Double)(left.register.double_1 * right.register.double_1);
                    return (T)(object)product;
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
        }

        [JitIntrinsic]
        internal static unsafe Vector<T> SquareRoot(Vector<T> value)
        {
            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(Byte))
                {
                    Byte* dataPtr = stackalloc Byte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = unchecked((Byte)Math.Sqrt((Byte)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte* dataPtr = stackalloc SByte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = unchecked((SByte)Math.Sqrt((SByte)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16* dataPtr = stackalloc UInt16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = unchecked((UInt16)Math.Sqrt((UInt16)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16* dataPtr = stackalloc Int16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = unchecked((Int16)Math.Sqrt((Int16)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32* dataPtr = stackalloc UInt32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = unchecked((UInt32)Math.Sqrt((UInt32)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32* dataPtr = stackalloc Int32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = unchecked((Int32)Math.Sqrt((Int32)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64* dataPtr = stackalloc UInt64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = unchecked((UInt64)Math.Sqrt((UInt64)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64* dataPtr = stackalloc Int64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = unchecked((Int64)Math.Sqrt((Int64)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single* dataPtr = stackalloc Single[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = unchecked((Single)Math.Sqrt((Single)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double* dataPtr = stackalloc Double[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = unchecked((Double)Math.Sqrt((Double)(object)value[g]));
                    }
                    return new Vector<T>(dataPtr);
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
            else
            {
                if (typeof(T) == typeof(Byte))
                {
                    value.register.byte_0 = (Byte)Math.Sqrt(value.register.byte_0);
                    value.register.byte_1 = (Byte)Math.Sqrt(value.register.byte_1);
                    value.register.byte_2 = (Byte)Math.Sqrt(value.register.byte_2);
                    value.register.byte_3 = (Byte)Math.Sqrt(value.register.byte_3);
                    value.register.byte_4 = (Byte)Math.Sqrt(value.register.byte_4);
                    value.register.byte_5 = (Byte)Math.Sqrt(value.register.byte_5);
                    value.register.byte_6 = (Byte)Math.Sqrt(value.register.byte_6);
                    value.register.byte_7 = (Byte)Math.Sqrt(value.register.byte_7);
                    value.register.byte_8 = (Byte)Math.Sqrt(value.register.byte_8);
                    value.register.byte_9 = (Byte)Math.Sqrt(value.register.byte_9);
                    value.register.byte_10 = (Byte)Math.Sqrt(value.register.byte_10);
                    value.register.byte_11 = (Byte)Math.Sqrt(value.register.byte_11);
                    value.register.byte_12 = (Byte)Math.Sqrt(value.register.byte_12);
                    value.register.byte_13 = (Byte)Math.Sqrt(value.register.byte_13);
                    value.register.byte_14 = (Byte)Math.Sqrt(value.register.byte_14);
                    value.register.byte_15 = (Byte)Math.Sqrt(value.register.byte_15);
                    return value;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    value.register.sbyte_0 = (SByte)Math.Sqrt(value.register.sbyte_0);
                    value.register.sbyte_1 = (SByte)Math.Sqrt(value.register.sbyte_1);
                    value.register.sbyte_2 = (SByte)Math.Sqrt(value.register.sbyte_2);
                    value.register.sbyte_3 = (SByte)Math.Sqrt(value.register.sbyte_3);
                    value.register.sbyte_4 = (SByte)Math.Sqrt(value.register.sbyte_4);
                    value.register.sbyte_5 = (SByte)Math.Sqrt(value.register.sbyte_5);
                    value.register.sbyte_6 = (SByte)Math.Sqrt(value.register.sbyte_6);
                    value.register.sbyte_7 = (SByte)Math.Sqrt(value.register.sbyte_7);
                    value.register.sbyte_8 = (SByte)Math.Sqrt(value.register.sbyte_8);
                    value.register.sbyte_9 = (SByte)Math.Sqrt(value.register.sbyte_9);
                    value.register.sbyte_10 = (SByte)Math.Sqrt(value.register.sbyte_10);
                    value.register.sbyte_11 = (SByte)Math.Sqrt(value.register.sbyte_11);
                    value.register.sbyte_12 = (SByte)Math.Sqrt(value.register.sbyte_12);
                    value.register.sbyte_13 = (SByte)Math.Sqrt(value.register.sbyte_13);
                    value.register.sbyte_14 = (SByte)Math.Sqrt(value.register.sbyte_14);
                    value.register.sbyte_15 = (SByte)Math.Sqrt(value.register.sbyte_15);
                    return value;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    value.register.uint16_0 = (UInt16)Math.Sqrt(value.register.uint16_0);
                    value.register.uint16_1 = (UInt16)Math.Sqrt(value.register.uint16_1);
                    value.register.uint16_2 = (UInt16)Math.Sqrt(value.register.uint16_2);
                    value.register.uint16_3 = (UInt16)Math.Sqrt(value.register.uint16_3);
                    value.register.uint16_4 = (UInt16)Math.Sqrt(value.register.uint16_4);
                    value.register.uint16_5 = (UInt16)Math.Sqrt(value.register.uint16_5);
                    value.register.uint16_6 = (UInt16)Math.Sqrt(value.register.uint16_6);
                    value.register.uint16_7 = (UInt16)Math.Sqrt(value.register.uint16_7);
                    return value;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    value.register.int16_0 = (Int16)Math.Sqrt(value.register.int16_0);
                    value.register.int16_1 = (Int16)Math.Sqrt(value.register.int16_1);
                    value.register.int16_2 = (Int16)Math.Sqrt(value.register.int16_2);
                    value.register.int16_3 = (Int16)Math.Sqrt(value.register.int16_3);
                    value.register.int16_4 = (Int16)Math.Sqrt(value.register.int16_4);
                    value.register.int16_5 = (Int16)Math.Sqrt(value.register.int16_5);
                    value.register.int16_6 = (Int16)Math.Sqrt(value.register.int16_6);
                    value.register.int16_7 = (Int16)Math.Sqrt(value.register.int16_7);
                    return value;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    value.register.uint32_0 = (UInt32)Math.Sqrt(value.register.uint32_0);
                    value.register.uint32_1 = (UInt32)Math.Sqrt(value.register.uint32_1);
                    value.register.uint32_2 = (UInt32)Math.Sqrt(value.register.uint32_2);
                    value.register.uint32_3 = (UInt32)Math.Sqrt(value.register.uint32_3);
                    return value;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    value.register.int32_0 = (Int32)Math.Sqrt(value.register.int32_0);
                    value.register.int32_1 = (Int32)Math.Sqrt(value.register.int32_1);
                    value.register.int32_2 = (Int32)Math.Sqrt(value.register.int32_2);
                    value.register.int32_3 = (Int32)Math.Sqrt(value.register.int32_3);
                    return value;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    value.register.uint64_0 = (UInt64)Math.Sqrt(value.register.uint64_0);
                    value.register.uint64_1 = (UInt64)Math.Sqrt(value.register.uint64_1);
                    return value;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    value.register.int64_0 = (Int64)Math.Sqrt(value.register.int64_0);
                    value.register.int64_1 = (Int64)Math.Sqrt(value.register.int64_1);
                    return value;
                }
                else if (typeof(T) == typeof(Single))
                {
                    value.register.single_0 = (Single)Math.Sqrt(value.register.single_0);
                    value.register.single_1 = (Single)Math.Sqrt(value.register.single_1);
                    value.register.single_2 = (Single)Math.Sqrt(value.register.single_2);
                    value.register.single_3 = (Single)Math.Sqrt(value.register.single_3);
                    return value;
                }
                else if (typeof(T) == typeof(Double))
                {
                    value.register.double_0 = (Double)Math.Sqrt(value.register.double_0);
                    value.register.double_1 = (Double)Math.Sqrt(value.register.double_1);
                    return value;
                }
                else
                {
                    throw new NotSupportedException(SR.Arg_TypeNotSupported);
                }
            }
        }
        #endregion Internal Math Methods

        #region Helper Methods
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static bool ScalarEquals(T left, T right)
        {
            if (typeof(T) == typeof(Byte))
            {
                return (Byte)(object)left == (Byte)(object)right;
            }
            else if (typeof(T) == typeof(SByte))
            {
                return (SByte)(object)left == (SByte)(object)right;
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return (UInt16)(object)left == (UInt16)(object)right;
            }
            else if (typeof(T) == typeof(Int16))
            {
                return (Int16)(object)left == (Int16)(object)right;
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return (UInt32)(object)left == (UInt32)(object)right;
            }
            else if (typeof(T) == typeof(Int32))
            {
                return (Int32)(object)left == (Int32)(object)right;
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return (UInt64)(object)left == (UInt64)(object)right;
            }
            else if (typeof(T) == typeof(Int64))
            {
                return (Int64)(object)left == (Int64)(object)right;
            }
            else if (typeof(T) == typeof(Single))
            {
                return (Single)(object)left == (Single)(object)right;
            }
            else if (typeof(T) == typeof(Double))
            {
                return (Double)(object)left == (Double)(object)right;
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static bool ScalarLessThan(T left, T right)
        {
            if (typeof(T) == typeof(Byte))
            {
                return (Byte)(object)left < (Byte)(object)right;
            }
            else if (typeof(T) == typeof(SByte))
            {
                return (SByte)(object)left < (SByte)(object)right;
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return (UInt16)(object)left < (UInt16)(object)right;
            }
            else if (typeof(T) == typeof(Int16))
            {
                return (Int16)(object)left < (Int16)(object)right;
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return (UInt32)(object)left < (UInt32)(object)right;
            }
            else if (typeof(T) == typeof(Int32))
            {
                return (Int32)(object)left < (Int32)(object)right;
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return (UInt64)(object)left < (UInt64)(object)right;
            }
            else if (typeof(T) == typeof(Int64))
            {
                return (Int64)(object)left < (Int64)(object)right;
            }
            else if (typeof(T) == typeof(Single))
            {
                return (Single)(object)left < (Single)(object)right;
            }
            else if (typeof(T) == typeof(Double))
            {
                return (Double)(object)left < (Double)(object)right;
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static bool ScalarGreaterThan(T left, T right)
        {
            if (typeof(T) == typeof(Byte))
            {
                return (Byte)(object)left > (Byte)(object)right;
            }
            else if (typeof(T) == typeof(SByte))
            {
                return (SByte)(object)left > (SByte)(object)right;
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return (UInt16)(object)left > (UInt16)(object)right;
            }
            else if (typeof(T) == typeof(Int16))
            {
                return (Int16)(object)left > (Int16)(object)right;
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return (UInt32)(object)left > (UInt32)(object)right;
            }
            else if (typeof(T) == typeof(Int32))
            {
                return (Int32)(object)left > (Int32)(object)right;
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return (UInt64)(object)left > (UInt64)(object)right;
            }
            else if (typeof(T) == typeof(Int64))
            {
                return (Int64)(object)left > (Int64)(object)right;
            }
            else if (typeof(T) == typeof(Single))
            {
                return (Single)(object)left > (Single)(object)right;
            }
            else if (typeof(T) == typeof(Double))
            {
                return (Double)(object)left > (Double)(object)right;
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static T ScalarAdd(T left, T right)
        {
            if (typeof(T) == typeof(Byte))
            {
                return (T)(object)unchecked((Byte)((Byte)(object)left + (Byte)(object)right));
            }
            else if (typeof(T) == typeof(SByte))
            {
                return (T)(object)unchecked((SByte)((SByte)(object)left + (SByte)(object)right));
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return (T)(object)unchecked((UInt16)((UInt16)(object)left + (UInt16)(object)right));
            }
            else if (typeof(T) == typeof(Int16))
            {
                return (T)(object)unchecked((Int16)((Int16)(object)left + (Int16)(object)right));
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return (T)(object)unchecked((UInt32)((UInt32)(object)left + (UInt32)(object)right));
            }
            else if (typeof(T) == typeof(Int32))
            {
                return (T)(object)unchecked((Int32)((Int32)(object)left + (Int32)(object)right));
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return (T)(object)unchecked((UInt64)((UInt64)(object)left + (UInt64)(object)right));
            }
            else if (typeof(T) == typeof(Int64))
            {
                return (T)(object)unchecked((Int64)((Int64)(object)left + (Int64)(object)right));
            }
            else if (typeof(T) == typeof(Single))
            {
                return (T)(object)unchecked((Single)((Single)(object)left + (Single)(object)right));
            }
            else if (typeof(T) == typeof(Double))
            {
                return (T)(object)unchecked((Double)((Double)(object)left + (Double)(object)right));
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static T ScalarSubtract(T left, T right)
        {
            if (typeof(T) == typeof(Byte))
            {
                return (T)(object)(Byte)((Byte)(object)left - (Byte)(object)right);
            }
            else if (typeof(T) == typeof(SByte))
            {
                return (T)(object)(SByte)((SByte)(object)left - (SByte)(object)right);
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return (T)(object)(UInt16)((UInt16)(object)left - (UInt16)(object)right);
            }
            else if (typeof(T) == typeof(Int16))
            {
                return (T)(object)(Int16)((Int16)(object)left - (Int16)(object)right);
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return (T)(object)(UInt32)((UInt32)(object)left - (UInt32)(object)right);
            }
            else if (typeof(T) == typeof(Int32))
            {
                return (T)(object)(Int32)((Int32)(object)left - (Int32)(object)right);
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return (T)(object)(UInt64)((UInt64)(object)left - (UInt64)(object)right);
            }
            else if (typeof(T) == typeof(Int64))
            {
                return (T)(object)(Int64)((Int64)(object)left - (Int64)(object)right);
            }
            else if (typeof(T) == typeof(Single))
            {
                return (T)(object)(Single)((Single)(object)left - (Single)(object)right);
            }
            else if (typeof(T) == typeof(Double))
            {
                return (T)(object)(Double)((Double)(object)left - (Double)(object)right);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static T ScalarMultiply(T left, T right)
        {
            if (typeof(T) == typeof(Byte))
            {
                return (T)(object)unchecked((Byte)((Byte)(object)left * (Byte)(object)right));
            }
            else if (typeof(T) == typeof(SByte))
            {
                return (T)(object)unchecked((SByte)((SByte)(object)left * (SByte)(object)right));
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return (T)(object)unchecked((UInt16)((UInt16)(object)left * (UInt16)(object)right));
            }
            else if (typeof(T) == typeof(Int16))
            {
                return (T)(object)unchecked((Int16)((Int16)(object)left * (Int16)(object)right));
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return (T)(object)unchecked((UInt32)((UInt32)(object)left * (UInt32)(object)right));
            }
            else if (typeof(T) == typeof(Int32))
            {
                return (T)(object)unchecked((Int32)((Int32)(object)left * (Int32)(object)right));
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return (T)(object)unchecked((UInt64)((UInt64)(object)left * (UInt64)(object)right));
            }
            else if (typeof(T) == typeof(Int64))
            {
                return (T)(object)unchecked((Int64)((Int64)(object)left * (Int64)(object)right));
            }
            else if (typeof(T) == typeof(Single))
            {
                return (T)(object)unchecked((Single)((Single)(object)left * (Single)(object)right));
            }
            else if (typeof(T) == typeof(Double))
            {
                return (T)(object)unchecked((Double)((Double)(object)left * (Double)(object)right));
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static T ScalarDivide(T left, T right)
        {
            if (typeof(T) == typeof(Byte))
            {
                return (T)(object)(Byte)((Byte)(object)left / (Byte)(object)right);
            }
            else if (typeof(T) == typeof(SByte))
            {
                return (T)(object)(SByte)((SByte)(object)left / (SByte)(object)right);
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return (T)(object)(UInt16)((UInt16)(object)left / (UInt16)(object)right);
            }
            else if (typeof(T) == typeof(Int16))
            {
                return (T)(object)(Int16)((Int16)(object)left / (Int16)(object)right);
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return (T)(object)(UInt32)((UInt32)(object)left / (UInt32)(object)right);
            }
            else if (typeof(T) == typeof(Int32))
            {
                return (T)(object)(Int32)((Int32)(object)left / (Int32)(object)right);
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return (T)(object)(UInt64)((UInt64)(object)left / (UInt64)(object)right);
            }
            else if (typeof(T) == typeof(Int64))
            {
                return (T)(object)(Int64)((Int64)(object)left / (Int64)(object)right);
            }
            else if (typeof(T) == typeof(Single))
            {
                return (T)(object)(Single)((Single)(object)left / (Single)(object)right);
            }
            else if (typeof(T) == typeof(Double))
            {
                return (T)(object)(Double)((Double)(object)left / (Double)(object)right);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static T GetZeroValue()
        {
            if (typeof(T) == typeof(Byte))
            {
                Byte value = 0;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(SByte))
            {
                SByte value = 0;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(UInt16))
            {
                UInt16 value = 0;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(Int16))
            {
                Int16 value = 0;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(UInt32))
            {
                UInt32 value = 0;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(Int32))
            {
                Int32 value = 0;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(UInt64))
            {
                UInt64 value = 0;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(Int64))
            {
                Int64 value = 0;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(Single))
            {
                Single value = 0;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(Double))
            {
                Double value = 0;
                return (T)(object)value;
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static T GetOneValue()
        {
            if (typeof(T) == typeof(Byte))
            {
                Byte value = 1;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(SByte))
            {
                SByte value = 1;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(UInt16))
            {
                UInt16 value = 1;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(Int16))
            {
                Int16 value = 1;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(UInt32))
            {
                UInt32 value = 1;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(Int32))
            {
                Int32 value = 1;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(UInt64))
            {
                UInt64 value = 1;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(Int64))
            {
                Int64 value = 1;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(Single))
            {
                Single value = 1;
                return (T)(object)value;
            }
            else if (typeof(T) == typeof(Double))
            {
                Double value = 1;
                return (T)(object)value;
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static T GetAllBitsSetValue()
        {
            if (typeof(T) == typeof(Byte))
            {
                return (T)(object)ConstantHelper.GetByteWithAllBitsSet();
            }
            else if (typeof(T) == typeof(SByte))
            {
                return (T)(object)ConstantHelper.GetSByteWithAllBitsSet();
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return (T)(object)ConstantHelper.GetUInt16WithAllBitsSet();
            }
            else if (typeof(T) == typeof(Int16))
            {
                return (T)(object)ConstantHelper.GetInt16WithAllBitsSet();
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return (T)(object)ConstantHelper.GetUInt32WithAllBitsSet();
            }
            else if (typeof(T) == typeof(Int32))
            {
                return (T)(object)ConstantHelper.GetInt32WithAllBitsSet();
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return (T)(object)ConstantHelper.GetUInt64WithAllBitsSet();
            }
            else if (typeof(T) == typeof(Int64))
            {
                return (T)(object)ConstantHelper.GetInt64WithAllBitsSet();
            }
            else if (typeof(T) == typeof(Single))
            {
                return (T)(object)ConstantHelper.GetSingleWithAllBitsSet();
            }
            else if (typeof(T) == typeof(Double))
            {
                return (T)(object)ConstantHelper.GetDoubleWithAllBitsSet();
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }
        #endregion
    }

    public static partial class Vector
    {
        #region Widen/Narrow
        /// <summary>
        /// Widens a Vector{Byte} into two Vector{UInt16}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe void Widen(Vector<Byte> source, out Vector<UInt16> low, out Vector<UInt16> high)
        {
            int elements = Vector<Byte>.Count;
            UInt16* lowPtr = stackalloc UInt16[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (UInt16)source[i];
            }
            UInt16* highPtr = stackalloc UInt16[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (UInt16)source[i + (elements / 2)];
            }

            low = new Vector<UInt16>(lowPtr);
            high = new Vector<UInt16>(highPtr);
        }

        /// <summary>
        /// Widens a Vector{UInt16} into two Vector{UInt32}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe void Widen(Vector<UInt16> source, out Vector<UInt32> low, out Vector<UInt32> high)
        {
            int elements = Vector<UInt16>.Count;
            UInt32* lowPtr = stackalloc UInt32[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (UInt32)source[i];
            }
            UInt32* highPtr = stackalloc UInt32[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (UInt32)source[i + (elements / 2)];
            }

            low = new Vector<UInt32>(lowPtr);
            high = new Vector<UInt32>(highPtr);
        }

        /// <summary>
        /// Widens a Vector{UInt32} into two Vector{UInt64}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe void Widen(Vector<UInt32> source, out Vector<UInt64> low, out Vector<UInt64> high)
        {
            int elements = Vector<UInt32>.Count;
            UInt64* lowPtr = stackalloc UInt64[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (UInt64)source[i];
            }
            UInt64* highPtr = stackalloc UInt64[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (UInt64)source[i + (elements / 2)];
            }

            low = new Vector<UInt64>(lowPtr);
            high = new Vector<UInt64>(highPtr);
        }

        /// <summary>
        /// Widens a Vector{SByte} into two Vector{Int16}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe void Widen(Vector<SByte> source, out Vector<Int16> low, out Vector<Int16> high)
        {
            int elements = Vector<SByte>.Count;
            Int16* lowPtr = stackalloc Int16[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (Int16)source[i];
            }
            Int16* highPtr = stackalloc Int16[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (Int16)source[i + (elements / 2)];
            }

            low = new Vector<Int16>(lowPtr);
            high = new Vector<Int16>(highPtr);
        }

        /// <summary>
        /// Widens a Vector{Int16} into two Vector{Int32}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [JitIntrinsic]
        public static unsafe void Widen(Vector<Int16> source, out Vector<Int32> low, out Vector<Int32> high)
        {
            int elements = Vector<Int16>.Count;
            Int32* lowPtr = stackalloc Int32[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (Int32)source[i];
            }
            Int32* highPtr = stackalloc Int32[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (Int32)source[i + (elements / 2)];
            }

            low = new Vector<Int32>(lowPtr);
            high = new Vector<Int32>(highPtr);
        }

        /// <summary>
        /// Widens a Vector{Int32} into two Vector{Int64}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [JitIntrinsic]
        public static unsafe void Widen(Vector<Int32> source, out Vector<Int64> low, out Vector<Int64> high)
        {
            int elements = Vector<Int32>.Count;
            Int64* lowPtr = stackalloc Int64[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (Int64)source[i];
            }
            Int64* highPtr = stackalloc Int64[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (Int64)source[i + (elements / 2)];
            }

            low = new Vector<Int64>(lowPtr);
            high = new Vector<Int64>(highPtr);
        }

        /// <summary>
        /// Widens a Vector{Single} into two Vector{Double}'s.
        /// <param name="source">The source vector whose elements are widened into the outputs.</param>
        /// <param name="low">The first output vector, whose elements will contain the widened elements from lower indices in the source vector.</param>
        /// <param name="high">The second output vector, whose elements will contain the widened elements from higher indices in the source vector.</param>
        /// </summary>
        [JitIntrinsic]
        public static unsafe void Widen(Vector<Single> source, out Vector<Double> low, out Vector<Double> high)
        {
            int elements = Vector<Single>.Count;
            Double* lowPtr = stackalloc Double[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                lowPtr[i] = (Double)source[i];
            }
            Double* highPtr = stackalloc Double[elements / 2];
            for (int i = 0; i < elements / 2; i++)
            {
                highPtr[i] = (Double)source[i + (elements / 2)];
            }

            low = new Vector<Double>(lowPtr);
            high = new Vector<Double>(highPtr);
        }

        /// <summary>
        /// Narrows two Vector{UInt16}'s into one Vector{Byte}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{Byte} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe Vector<Byte> Narrow(Vector<UInt16> low, Vector<UInt16> high)
        {
		    unchecked
		    {
				int elements = Vector<Byte>.Count;
				Byte* retPtr = stackalloc Byte[elements];
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i] = (Byte)low[i];
				}
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i + (elements / 2)] = (Byte)high[i];
				}

				return new Vector<Byte>(retPtr);
		    }
        }

        /// <summary>
        /// Narrows two Vector{UInt32}'s into one Vector{UInt16}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{UInt16} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe Vector<UInt16> Narrow(Vector<UInt32> low, Vector<UInt32> high)
        {
		    unchecked
		    {
				int elements = Vector<UInt16>.Count;
				UInt16* retPtr = stackalloc UInt16[elements];
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i] = (UInt16)low[i];
				}
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i + (elements / 2)] = (UInt16)high[i];
				}

				return new Vector<UInt16>(retPtr);
		    }
        }

        /// <summary>
        /// Narrows two Vector{UInt64}'s into one Vector{UInt32}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{UInt32} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe Vector<UInt32> Narrow(Vector<UInt64> low, Vector<UInt64> high)
        {
		    unchecked
		    {
				int elements = Vector<UInt32>.Count;
				UInt32* retPtr = stackalloc UInt32[elements];
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i] = (UInt32)low[i];
				}
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i + (elements / 2)] = (UInt32)high[i];
				}

				return new Vector<UInt32>(retPtr);
		    }
        }

        /// <summary>
        /// Narrows two Vector{Int16}'s into one Vector{SByte}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{SByte} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe Vector<SByte> Narrow(Vector<Int16> low, Vector<Int16> high)
        {
		    unchecked
		    {
				int elements = Vector<SByte>.Count;
				SByte* retPtr = stackalloc SByte[elements];
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i] = (SByte)low[i];
				}
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i + (elements / 2)] = (SByte)high[i];
				}

				return new Vector<SByte>(retPtr);
		    }
        }

        /// <summary>
        /// Narrows two Vector{Int32}'s into one Vector{Int16}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{Int16} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [JitIntrinsic]
        public static unsafe Vector<Int16> Narrow(Vector<Int32> low, Vector<Int32> high)
        {
		    unchecked
		    {
				int elements = Vector<Int16>.Count;
				Int16* retPtr = stackalloc Int16[elements];
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i] = (Int16)low[i];
				}
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i + (elements / 2)] = (Int16)high[i];
				}

				return new Vector<Int16>(retPtr);
		    }
        }

        /// <summary>
        /// Narrows two Vector{Int64}'s into one Vector{Int32}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{Int32} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [JitIntrinsic]
        public static unsafe Vector<Int32> Narrow(Vector<Int64> low, Vector<Int64> high)
        {
		    unchecked
		    {
				int elements = Vector<Int32>.Count;
				Int32* retPtr = stackalloc Int32[elements];
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i] = (Int32)low[i];
				}
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i + (elements / 2)] = (Int32)high[i];
				}

				return new Vector<Int32>(retPtr);
		    }
        }

        /// <summary>
        /// Narrows two Vector{Double}'s into one Vector{Single}.
        /// <param name="low">The first source vector, whose elements become the lower-index elements of the return value.</param>
        /// <param name="high">The second source vector, whose elements become the higher-index elements of the return value.</param>
        /// <returns>A Vector{Single} containing elements narrowed from the source vectors.</returns>
        /// </summary>
        [JitIntrinsic]
        public static unsafe Vector<Single> Narrow(Vector<Double> low, Vector<Double> high)
        {
		    unchecked
		    {
				int elements = Vector<Single>.Count;
				Single* retPtr = stackalloc Single[elements];
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i] = (Single)low[i];
				}
				for (int i = 0; i < elements / 2; i++)
				{
					retPtr[i + (elements / 2)] = (Single)high[i];
				}

				return new Vector<Single>(retPtr);
		    }
        }

        #endregion Widen/Narrow

        #region Same-Size Conversion
        /// <summary>
        /// Converts a Vector{Int32} to a Vector{Single}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [JitIntrinsic]
        public static unsafe Vector<Single> ConvertToSingle(Vector<Int32> value)
        {
			unchecked
			{
				int elements = Vector<Single>.Count;
				Single* retPtr = stackalloc Single[elements];
				for (int i = 0; i < elements; i++)
				{
					retPtr[i] = (Single)value[i];
				}

				return new Vector<Single>(retPtr);
			}
        }

        /// <summary>
        /// Converts a Vector{UInt32} to a Vector{Single}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe Vector<Single> ConvertToSingle(Vector<UInt32> value)
        {
			unchecked
			{
				int elements = Vector<Single>.Count;
				Single* retPtr = stackalloc Single[elements];
				for (int i = 0; i < elements; i++)
				{
					retPtr[i] = (Single)value[i];
				}

				return new Vector<Single>(retPtr);
			}
        }

        /// <summary>
        /// Converts a Vector{Int64} to a Vector{Double}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [JitIntrinsic]
        public static unsafe Vector<Double> ConvertToDouble(Vector<Int64> value)
        {
			unchecked
			{
				int elements = Vector<Double>.Count;
				Double* retPtr = stackalloc Double[elements];
				for (int i = 0; i < elements; i++)
				{
					retPtr[i] = (Double)value[i];
				}

				return new Vector<Double>(retPtr);
			}
        }

        /// <summary>
        /// Converts a Vector{UInt64} to a Vector{Double}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe Vector<Double> ConvertToDouble(Vector<UInt64> value)
        {
			unchecked
			{
				int elements = Vector<Double>.Count;
				Double* retPtr = stackalloc Double[elements];
				for (int i = 0; i < elements; i++)
				{
					retPtr[i] = (Double)value[i];
				}

				return new Vector<Double>(retPtr);
			}
        }

        /// <summary>
        /// Converts a Vector{Single} to a Vector{Int32}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [JitIntrinsic]
        public static unsafe Vector<Int32> ConvertToInt32(Vector<Single> value)
        {
			unchecked
			{
				int elements = Vector<Int32>.Count;
				Int32* retPtr = stackalloc Int32[elements];
				for (int i = 0; i < elements; i++)
				{
					retPtr[i] = (Int32)value[i];
				}

				return new Vector<Int32>(retPtr);
			}
        }

        /// <summary>
        /// Converts a Vector{Single} to a Vector{UInt32}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe Vector<UInt32> ConvertToUInt32(Vector<Single> value)
        {
			unchecked
			{
				int elements = Vector<UInt32>.Count;
				UInt32* retPtr = stackalloc UInt32[elements];
				for (int i = 0; i < elements; i++)
				{
					retPtr[i] = (UInt32)value[i];
				}

				return new Vector<UInt32>(retPtr);
			}
        }

        /// <summary>
        /// Converts a Vector{Double} to a Vector{Int64}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [JitIntrinsic]
        public static unsafe Vector<Int64> ConvertToInt64(Vector<Double> value)
        {
			unchecked
			{
				int elements = Vector<Int64>.Count;
				Int64* retPtr = stackalloc Int64[elements];
				for (int i = 0; i < elements; i++)
				{
					retPtr[i] = (Int64)value[i];
				}

				return new Vector<Int64>(retPtr);
			}
        }

        /// <summary>
        /// Converts a Vector{Double} to a Vector{UInt64}.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The converted vector.</returns>
        [CLSCompliant(false)]
        [JitIntrinsic]
        public static unsafe Vector<UInt64> ConvertToUInt64(Vector<Double> value)
        {
			unchecked
			{
				int elements = Vector<UInt64>.Count;
				UInt64* retPtr = stackalloc UInt64[elements];
				for (int i = 0; i < elements; i++)
				{
					retPtr[i] = (UInt64)value[i];
				}

				return new Vector<UInt64>(retPtr);
			}
        }

        #endregion Same-Size Conversion
    }
}
