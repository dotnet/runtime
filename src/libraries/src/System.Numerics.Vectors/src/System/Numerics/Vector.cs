// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// This file is auto-generated, do not make permanent modifications.

using System.Diagnostics.Contracts;
using System.Globalization;
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
        private Register _register;
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
                if (Vector.IsHardwareAccelerated)
                {
                    throw new NotSupportedException(SR.GetString("Reflection_MethodNotSupported"));
                }
                else
                {
                    return s_count;
                }
            }
        }
        private static int s_count = InitializeCount();

        /// <summary> 
        /// Returns a vector containing all zeroes. 
        /// </summary>
        [JitIntrinsic]
        public static Vector<T> Zero { get { return s_zero; } }
        private static readonly Vector<T> s_zero = new Vector<T>(GetZeroValue());

        /// <summary> 
        /// Returns a vector containing all ones. 
        /// </summary>
        [JitIntrinsic]
        public static Vector<T> One { get { return s_one; } }
        private static readonly Vector<T> s_one = new Vector<T>(GetOneValue());

        internal static Vector<T> AllOnes { get { return s_allOnes; } }
        private static readonly Vector<T> s_allOnes = new Vector<T>(GetAllBitsSetValue());
        #endregion Static Members

        #region Static Initialization
        private static int InitializeCount()
        {
            Contract.Requires(
                Vector.IsHardwareAccelerated == false,
                "InitializeCount cannot be invoked when running under hardware acceleration");
            if (typeof(T) == typeof(Byte))
            {
                return 16;
            }
            else if (typeof(T) == typeof(SByte))
            {
                return 16;
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return 8;
            }
            else if (typeof(T) == typeof(Int16))
            {
                return 8;
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return 4;
            }
            else if (typeof(T) == typeof(Int32))
            {
                return 4;
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return 2;
            }
            else if (typeof(T) == typeof(Int64))
            {
                return 2;
            }
            else if (typeof(T) == typeof(Single))
            {
                return 4;
            }
            else if (typeof(T) == typeof(Double))
            {
                return 2;
            }
            else
            {
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
            }
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
                    fixed (Byte* basePtr = &_register.byte_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Byte)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(SByte))
                {
                    fixed (SByte* basePtr = &_register.sbyte_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (SByte)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    fixed (UInt16* basePtr = &_register.uint16_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt16)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(Int16))
                {
                    fixed (Int16* basePtr = &_register.int16_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int16)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    fixed (UInt32* basePtr = &_register.uint32_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt32)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(Int32))
                {
                    fixed (Int32* basePtr = &_register.int32_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int32)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    fixed (UInt64* basePtr = &_register.uint64_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt64)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(Int64))
                {
                    fixed (Int64* basePtr = &_register.int64_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int64)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(Single))
                {
                    fixed (Single* basePtr = &_register.single_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Single)(object)value;
                        }
                    }
                }
                else if (typeof(T) == typeof(Double))
                {
                    fixed (Double* basePtr = &_register.double_0)
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
                    _register.byte_0 = (Byte)(object)value;
                    _register.byte_1 = (Byte)(object)value;
                    _register.byte_2 = (Byte)(object)value;
                    _register.byte_3 = (Byte)(object)value;
                    _register.byte_4 = (Byte)(object)value;
                    _register.byte_5 = (Byte)(object)value;
                    _register.byte_6 = (Byte)(object)value;
                    _register.byte_7 = (Byte)(object)value;
                    _register.byte_8 = (Byte)(object)value;
                    _register.byte_9 = (Byte)(object)value;
                    _register.byte_10 = (Byte)(object)value;
                    _register.byte_11 = (Byte)(object)value;
                    _register.byte_12 = (Byte)(object)value;
                    _register.byte_13 = (Byte)(object)value;
                    _register.byte_14 = (Byte)(object)value;
                    _register.byte_15 = (Byte)(object)value;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    _register.sbyte_0 = (SByte)(object)value;
                    _register.sbyte_1 = (SByte)(object)value;
                    _register.sbyte_2 = (SByte)(object)value;
                    _register.sbyte_3 = (SByte)(object)value;
                    _register.sbyte_4 = (SByte)(object)value;
                    _register.sbyte_5 = (SByte)(object)value;
                    _register.sbyte_6 = (SByte)(object)value;
                    _register.sbyte_7 = (SByte)(object)value;
                    _register.sbyte_8 = (SByte)(object)value;
                    _register.sbyte_9 = (SByte)(object)value;
                    _register.sbyte_10 = (SByte)(object)value;
                    _register.sbyte_11 = (SByte)(object)value;
                    _register.sbyte_12 = (SByte)(object)value;
                    _register.sbyte_13 = (SByte)(object)value;
                    _register.sbyte_14 = (SByte)(object)value;
                    _register.sbyte_15 = (SByte)(object)value;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    _register.uint16_0 = (UInt16)(object)value;
                    _register.uint16_1 = (UInt16)(object)value;
                    _register.uint16_2 = (UInt16)(object)value;
                    _register.uint16_3 = (UInt16)(object)value;
                    _register.uint16_4 = (UInt16)(object)value;
                    _register.uint16_5 = (UInt16)(object)value;
                    _register.uint16_6 = (UInt16)(object)value;
                    _register.uint16_7 = (UInt16)(object)value;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    _register.int16_0 = (Int16)(object)value;
                    _register.int16_1 = (Int16)(object)value;
                    _register.int16_2 = (Int16)(object)value;
                    _register.int16_3 = (Int16)(object)value;
                    _register.int16_4 = (Int16)(object)value;
                    _register.int16_5 = (Int16)(object)value;
                    _register.int16_6 = (Int16)(object)value;
                    _register.int16_7 = (Int16)(object)value;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    _register.uint32_0 = (UInt32)(object)value;
                    _register.uint32_1 = (UInt32)(object)value;
                    _register.uint32_2 = (UInt32)(object)value;
                    _register.uint32_3 = (UInt32)(object)value;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    _register.int32_0 = (Int32)(object)value;
                    _register.int32_1 = (Int32)(object)value;
                    _register.int32_2 = (Int32)(object)value;
                    _register.int32_3 = (Int32)(object)value;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    _register.uint64_0 = (UInt64)(object)value;
                    _register.uint64_1 = (UInt64)(object)value;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    _register.int64_0 = (Int64)(object)value;
                    _register.int64_1 = (Int64)(object)value;
                }
                else if (typeof(T) == typeof(Single))
                {
                    _register.single_0 = (Single)(object)value;
                    _register.single_1 = (Single)(object)value;
                    _register.single_2 = (Single)(object)value;
                    _register.single_3 = (Single)(object)value;
                }
                else if (typeof(T) == typeof(Double))
                {
                    _register.double_0 = (Double)(object)value;
                    _register.double_1 = (Double)(object)value;
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
                throw new ArgumentNullException("values");
            }
            if (index < 0 || (values.Length - index) < Count)
            {
                throw new IndexOutOfRangeException();
            }

            if (Vector.IsHardwareAccelerated)
            {
                if (typeof(T) == typeof(Byte))
                {
                    fixed (Byte* basePtr = &_register.byte_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Byte)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(SByte))
                {
                    fixed (SByte* basePtr = &_register.sbyte_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (SByte)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    fixed (UInt16* basePtr = &_register.uint16_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt16)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(Int16))
                {
                    fixed (Int16* basePtr = &_register.int16_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int16)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    fixed (UInt32* basePtr = &_register.uint32_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt32)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(Int32))
                {
                    fixed (Int32* basePtr = &_register.int32_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int32)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    fixed (UInt64* basePtr = &_register.uint64_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (UInt64)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(Int64))
                {
                    fixed (Int64* basePtr = &_register.int64_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Int64)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(Single))
                {
                    fixed (Single* basePtr = &_register.single_0)
                    {
                        for (int g = 0; g < Count; g++)
                        {
                            *(basePtr + g) = (Single)(object)values[g + index];
                        }
                    }
                }
                else if (typeof(T) == typeof(Double))
                {
                    fixed (Double* basePtr = &_register.double_0)
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
                    fixed (Byte* basePtr = &_register.byte_0)
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
                    fixed (SByte* basePtr = &_register.sbyte_0)
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
                    fixed (UInt16* basePtr = &_register.uint16_0)
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
                    fixed (Int16* basePtr = &_register.int16_0)
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
                    fixed (UInt32* basePtr = &_register.uint32_0)
                    {
                        *(basePtr + 0) = (UInt32)(object)values[0 + index];
                        *(basePtr + 1) = (UInt32)(object)values[1 + index];
                        *(basePtr + 2) = (UInt32)(object)values[2 + index];
                        *(basePtr + 3) = (UInt32)(object)values[3 + index];
                    }
                }
                else if (typeof(T) == typeof(Int32))
                {
                    fixed (Int32* basePtr = &_register.int32_0)
                    {
                        *(basePtr + 0) = (Int32)(object)values[0 + index];
                        *(basePtr + 1) = (Int32)(object)values[1 + index];
                        *(basePtr + 2) = (Int32)(object)values[2 + index];
                        *(basePtr + 3) = (Int32)(object)values[3 + index];
                    }
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    fixed (UInt64* basePtr = &_register.uint64_0)
                    {
                        *(basePtr + 0) = (UInt64)(object)values[0 + index];
                        *(basePtr + 1) = (UInt64)(object)values[1 + index];
                    }
                }
                else if (typeof(T) == typeof(Int64))
                {
                    fixed (Int64* basePtr = &_register.int64_0)
                    {
                        *(basePtr + 0) = (Int64)(object)values[0 + index];
                        *(basePtr + 1) = (Int64)(object)values[1 + index];
                    }
                }
                else if (typeof(T) == typeof(Single))
                {
                    fixed (Single* basePtr = &_register.single_0)
                    {
                        *(basePtr + 0) = (Single)(object)values[0 + index];
                        *(basePtr + 1) = (Single)(object)values[1 + index];
                        *(basePtr + 2) = (Single)(object)values[2 + index];
                        *(basePtr + 3) = (Single)(object)values[3 + index];
                    }
                }
                else if (typeof(T) == typeof(Double))
                {
                    fixed (Double* basePtr = &_register.double_0)
                    {
                        *(basePtr + 0) = (Double)(object)values[0 + index];
                        *(basePtr + 1) = (Double)(object)values[1 + index];
                    }
                }
            }
        }

#pragma warning disable 3001 // void* is not a CLS-Compliant argument type
        private unsafe Vector(void* dataPointer) : this(dataPointer, 0) { }
#pragma warning restore 3001 // void* is not a CLS-Compliant argument type

#pragma warning disable 3001 // void* is not a CLS-Compliant argument type
        // Implemented with offset if this API ever becomes public; an offset of 0 is used internally.
        private unsafe Vector(void* dataPointer, int offset)
            : this()
        {
            if (typeof(T) == typeof(Byte))
            {
                Byte* castedPtr = (Byte*)dataPointer;
                castedPtr += offset;
                fixed (Byte* registerBase = &_register.byte_0)
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
                fixed (SByte* registerBase = &_register.sbyte_0)
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
                fixed (UInt16* registerBase = &_register.uint16_0)
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
                fixed (Int16* registerBase = &_register.int16_0)
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
                fixed (UInt32* registerBase = &_register.uint32_0)
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
                fixed (Int32* registerBase = &_register.int32_0)
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
                fixed (UInt64* registerBase = &_register.uint64_0)
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
                fixed (Int64* registerBase = &_register.int64_0)
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
                fixed (Single* registerBase = &_register.single_0)
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
                fixed (Double* registerBase = &_register.double_0)
                {
                    for (int g = 0; g < Count; g++)
                    {
                        registerBase[g] = castedPtr[g];
                    }
                }
            }
            else
            {
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
            }
        }
#pragma warning restore 3001 // void* is not a CLS-Compliant argument type

        private Vector(ref Register existingRegister)
        {
            _register = existingRegister;
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
                throw new ArgumentNullException("values");
            }
            if (startIndex < 0 || startIndex >= destination.Length)
            {
                throw new ArgumentOutOfRangeException(SR.GetString("Arg_ArgumentOutOfRangeException", startIndex));
            }
            if ((destination.Length - startIndex) < Count)
            {
                throw new ArgumentException(SR.GetString("Arg_ElementsInSourceIsGreaterThanDestination", startIndex));
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
                        destinationBase[startIndex + 0] = _register.byte_0;
                        destinationBase[startIndex + 1] = _register.byte_1;
                        destinationBase[startIndex + 2] = _register.byte_2;
                        destinationBase[startIndex + 3] = _register.byte_3;
                        destinationBase[startIndex + 4] = _register.byte_4;
                        destinationBase[startIndex + 5] = _register.byte_5;
                        destinationBase[startIndex + 6] = _register.byte_6;
                        destinationBase[startIndex + 7] = _register.byte_7;
                        destinationBase[startIndex + 8] = _register.byte_8;
                        destinationBase[startIndex + 9] = _register.byte_9;
                        destinationBase[startIndex + 10] = _register.byte_10;
                        destinationBase[startIndex + 11] = _register.byte_11;
                        destinationBase[startIndex + 12] = _register.byte_12;
                        destinationBase[startIndex + 13] = _register.byte_13;
                        destinationBase[startIndex + 14] = _register.byte_14;
                        destinationBase[startIndex + 15] = _register.byte_15;
                    }
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte[] sbyteArray = (SByte[])(object)destination;
                    fixed (SByte* destinationBase = sbyteArray)
                    {
                        destinationBase[startIndex + 0] = _register.sbyte_0;
                        destinationBase[startIndex + 1] = _register.sbyte_1;
                        destinationBase[startIndex + 2] = _register.sbyte_2;
                        destinationBase[startIndex + 3] = _register.sbyte_3;
                        destinationBase[startIndex + 4] = _register.sbyte_4;
                        destinationBase[startIndex + 5] = _register.sbyte_5;
                        destinationBase[startIndex + 6] = _register.sbyte_6;
                        destinationBase[startIndex + 7] = _register.sbyte_7;
                        destinationBase[startIndex + 8] = _register.sbyte_8;
                        destinationBase[startIndex + 9] = _register.sbyte_9;
                        destinationBase[startIndex + 10] = _register.sbyte_10;
                        destinationBase[startIndex + 11] = _register.sbyte_11;
                        destinationBase[startIndex + 12] = _register.sbyte_12;
                        destinationBase[startIndex + 13] = _register.sbyte_13;
                        destinationBase[startIndex + 14] = _register.sbyte_14;
                        destinationBase[startIndex + 15] = _register.sbyte_15;
                    }
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16[] uint16Array = (UInt16[])(object)destination;
                    fixed (UInt16* destinationBase = uint16Array)
                    {
                        destinationBase[startIndex + 0] = _register.uint16_0;
                        destinationBase[startIndex + 1] = _register.uint16_1;
                        destinationBase[startIndex + 2] = _register.uint16_2;
                        destinationBase[startIndex + 3] = _register.uint16_3;
                        destinationBase[startIndex + 4] = _register.uint16_4;
                        destinationBase[startIndex + 5] = _register.uint16_5;
                        destinationBase[startIndex + 6] = _register.uint16_6;
                        destinationBase[startIndex + 7] = _register.uint16_7;
                    }
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16[] int16Array = (Int16[])(object)destination;
                    fixed (Int16* destinationBase = int16Array)
                    {
                        destinationBase[startIndex + 0] = _register.int16_0;
                        destinationBase[startIndex + 1] = _register.int16_1;
                        destinationBase[startIndex + 2] = _register.int16_2;
                        destinationBase[startIndex + 3] = _register.int16_3;
                        destinationBase[startIndex + 4] = _register.int16_4;
                        destinationBase[startIndex + 5] = _register.int16_5;
                        destinationBase[startIndex + 6] = _register.int16_6;
                        destinationBase[startIndex + 7] = _register.int16_7;
                    }
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32[] uint32Array = (UInt32[])(object)destination;
                    fixed (UInt32* destinationBase = uint32Array)
                    {
                        destinationBase[startIndex + 0] = _register.uint32_0;
                        destinationBase[startIndex + 1] = _register.uint32_1;
                        destinationBase[startIndex + 2] = _register.uint32_2;
                        destinationBase[startIndex + 3] = _register.uint32_3;
                    }
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32[] int32Array = (Int32[])(object)destination;
                    fixed (Int32* destinationBase = int32Array)
                    {
                        destinationBase[startIndex + 0] = _register.int32_0;
                        destinationBase[startIndex + 1] = _register.int32_1;
                        destinationBase[startIndex + 2] = _register.int32_2;
                        destinationBase[startIndex + 3] = _register.int32_3;
                    }
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64[] uint64Array = (UInt64[])(object)destination;
                    fixed (UInt64* destinationBase = uint64Array)
                    {
                        destinationBase[startIndex + 0] = _register.uint64_0;
                        destinationBase[startIndex + 1] = _register.uint64_1;
                    }
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64[] int64Array = (Int64[])(object)destination;
                    fixed (Int64* destinationBase = int64Array)
                    {
                        destinationBase[startIndex + 0] = _register.int64_0;
                        destinationBase[startIndex + 1] = _register.int64_1;
                    }
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single[] singleArray = (Single[])(object)destination;
                    fixed (Single* destinationBase = singleArray)
                    {
                        destinationBase[startIndex + 0] = _register.single_0;
                        destinationBase[startIndex + 1] = _register.single_1;
                        destinationBase[startIndex + 2] = _register.single_2;
                        destinationBase[startIndex + 3] = _register.single_3;
                    }
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double[] doubleArray = (Double[])(object)destination;
                    fixed (Double* destinationBase = doubleArray)
                    {
                        destinationBase[startIndex + 0] = _register.double_0;
                        destinationBase[startIndex + 1] = _register.double_1;
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
                    throw new IndexOutOfRangeException(SR.GetString("Arg_ArgumentOutOfRangeException", index));
                }
                if (typeof(T) == typeof(Byte))
                {
                    fixed (Byte* basePtr = &_register.byte_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(SByte))
                {
                    fixed (SByte* basePtr = &_register.sbyte_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    fixed (UInt16* basePtr = &_register.uint16_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(Int16))
                {
                    fixed (Int16* basePtr = &_register.int16_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    fixed (UInt32* basePtr = &_register.uint32_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(Int32))
                {
                    fixed (Int32* basePtr = &_register.int32_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    fixed (UInt64* basePtr = &_register.uint64_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(Int64))
                {
                    fixed (Int64* basePtr = &_register.int64_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(Single))
                {
                    fixed (Single* basePtr = &_register.single_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else if (typeof(T) == typeof(Double))
                {
                    fixed (Double* basePtr = &_register.double_0)
                    {
                        return (T)(object)*(basePtr + index);
                    }
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
            if (obj == null || !(obj is Vector<T>))
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
                        _register.byte_0 == other._register.byte_0
                        && _register.byte_1 == other._register.byte_1
                        && _register.byte_2 == other._register.byte_2
                        && _register.byte_3 == other._register.byte_3
                        && _register.byte_4 == other._register.byte_4
                        && _register.byte_5 == other._register.byte_5
                        && _register.byte_6 == other._register.byte_6
                        && _register.byte_7 == other._register.byte_7
                        && _register.byte_8 == other._register.byte_8
                        && _register.byte_9 == other._register.byte_9
                        && _register.byte_10 == other._register.byte_10
                        && _register.byte_11 == other._register.byte_11
                        && _register.byte_12 == other._register.byte_12
                        && _register.byte_13 == other._register.byte_13
                        && _register.byte_14 == other._register.byte_14
                        && _register.byte_15 == other._register.byte_15;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    return
                        _register.sbyte_0 == other._register.sbyte_0
                        && _register.sbyte_1 == other._register.sbyte_1
                        && _register.sbyte_2 == other._register.sbyte_2
                        && _register.sbyte_3 == other._register.sbyte_3
                        && _register.sbyte_4 == other._register.sbyte_4
                        && _register.sbyte_5 == other._register.sbyte_5
                        && _register.sbyte_6 == other._register.sbyte_6
                        && _register.sbyte_7 == other._register.sbyte_7
                        && _register.sbyte_8 == other._register.sbyte_8
                        && _register.sbyte_9 == other._register.sbyte_9
                        && _register.sbyte_10 == other._register.sbyte_10
                        && _register.sbyte_11 == other._register.sbyte_11
                        && _register.sbyte_12 == other._register.sbyte_12
                        && _register.sbyte_13 == other._register.sbyte_13
                        && _register.sbyte_14 == other._register.sbyte_14
                        && _register.sbyte_15 == other._register.sbyte_15;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    return
                        _register.uint16_0 == other._register.uint16_0
                        && _register.uint16_1 == other._register.uint16_1
                        && _register.uint16_2 == other._register.uint16_2
                        && _register.uint16_3 == other._register.uint16_3
                        && _register.uint16_4 == other._register.uint16_4
                        && _register.uint16_5 == other._register.uint16_5
                        && _register.uint16_6 == other._register.uint16_6
                        && _register.uint16_7 == other._register.uint16_7;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    return
                        _register.int16_0 == other._register.int16_0
                        && _register.int16_1 == other._register.int16_1
                        && _register.int16_2 == other._register.int16_2
                        && _register.int16_3 == other._register.int16_3
                        && _register.int16_4 == other._register.int16_4
                        && _register.int16_5 == other._register.int16_5
                        && _register.int16_6 == other._register.int16_6
                        && _register.int16_7 == other._register.int16_7;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    return
                        _register.uint32_0 == other._register.uint32_0
                        && _register.uint32_1 == other._register.uint32_1
                        && _register.uint32_2 == other._register.uint32_2
                        && _register.uint32_3 == other._register.uint32_3;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    return
                        _register.int32_0 == other._register.int32_0
                        && _register.int32_1 == other._register.int32_1
                        && _register.int32_2 == other._register.int32_2
                        && _register.int32_3 == other._register.int32_3;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    return
                        _register.uint64_0 == other._register.uint64_0
                        && _register.uint64_1 == other._register.uint64_1;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    return
                        _register.int64_0 == other._register.int64_0
                        && _register.int64_1 == other._register.int64_1;
                }
                else if (typeof(T) == typeof(Single))
                {
                    return
                        _register.single_0 == other._register.single_0
                        && _register.single_1 == other._register.single_1
                        && _register.single_2 == other._register.single_2
                        && _register.single_3 == other._register.single_3;
                }
                else if (typeof(T) == typeof(Double))
                {
                    return
                        _register.double_0 == other._register.double_0
                        && _register.double_1 == other._register.double_1;
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                        hash = HashCodeHelper.CombineHashCodes(hash, ((Byte)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashCodeHelper.CombineHashCodes(hash, ((SByte)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashCodeHelper.CombineHashCodes(hash, ((UInt16)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashCodeHelper.CombineHashCodes(hash, ((Int16)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashCodeHelper.CombineHashCodes(hash, ((UInt32)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashCodeHelper.CombineHashCodes(hash, ((Int32)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashCodeHelper.CombineHashCodes(hash, ((UInt64)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashCodeHelper.CombineHashCodes(hash, ((Int64)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(Single))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashCodeHelper.CombineHashCodes(hash, ((Single)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else if (typeof(T) == typeof(Double))
                {
                    for (int g = 0; g < Count; g++)
                    {
                        hash = HashCodeHelper.CombineHashCodes(hash, ((Double)(object)this[g]).GetHashCode());
                    }
                    return hash;
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                }
            }
            else
            {
                if (typeof(T) == typeof(Byte))
                {
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_0.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_1.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_2.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_3.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_4.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_5.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_6.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_7.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_8.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_9.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_10.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_11.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_12.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_13.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_14.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.byte_15.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_0.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_1.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_2.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_3.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_4.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_5.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_6.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_7.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_8.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_9.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_10.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_11.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_12.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_13.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_14.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.sbyte_15.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint16_0.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint16_1.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint16_2.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint16_3.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint16_4.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint16_5.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint16_6.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint16_7.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int16_0.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int16_1.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int16_2.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int16_3.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int16_4.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int16_5.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int16_6.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int16_7.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint32_0.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint32_1.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint32_2.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint32_3.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int32_0.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int32_1.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int32_2.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int32_3.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint64_0.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.uint64_1.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int64_0.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.int64_1.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(Single))
                {
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.single_0.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.single_1.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.single_2.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.single_3.GetHashCode());
                    return hash;
                }
                else if (typeof(T) == typeof(Double))
                {
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.double_0.GetHashCode());
                    hash = HashCodeHelper.CombineHashCodes(hash, _register.double_1.GetHashCode());
                    return hash;
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator + " ";
            sb.Append("<");
            for (int g = 0; g < Count - 1; g++)
            {
                sb.Append(((IFormattable)this[g]).ToString(format, formatProvider));
                sb.Append(separator);
            }
            // Append last element w/out separator
            sb.Append(((IFormattable)this[Count - 1]).ToString(format, formatProvider));
            sb.Append(">");
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
                        throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                    }
                }
                else
                {
                    Vector<T> sum = new Vector<T>();
                    if (typeof(T) == typeof(Byte))
                    {
                        sum._register.byte_0 = (Byte)(left._register.byte_0 + right._register.byte_0);
                        sum._register.byte_1 = (Byte)(left._register.byte_1 + right._register.byte_1);
                        sum._register.byte_2 = (Byte)(left._register.byte_2 + right._register.byte_2);
                        sum._register.byte_3 = (Byte)(left._register.byte_3 + right._register.byte_3);
                        sum._register.byte_4 = (Byte)(left._register.byte_4 + right._register.byte_4);
                        sum._register.byte_5 = (Byte)(left._register.byte_5 + right._register.byte_5);
                        sum._register.byte_6 = (Byte)(left._register.byte_6 + right._register.byte_6);
                        sum._register.byte_7 = (Byte)(left._register.byte_7 + right._register.byte_7);
                        sum._register.byte_8 = (Byte)(left._register.byte_8 + right._register.byte_8);
                        sum._register.byte_9 = (Byte)(left._register.byte_9 + right._register.byte_9);
                        sum._register.byte_10 = (Byte)(left._register.byte_10 + right._register.byte_10);
                        sum._register.byte_11 = (Byte)(left._register.byte_11 + right._register.byte_11);
                        sum._register.byte_12 = (Byte)(left._register.byte_12 + right._register.byte_12);
                        sum._register.byte_13 = (Byte)(left._register.byte_13 + right._register.byte_13);
                        sum._register.byte_14 = (Byte)(left._register.byte_14 + right._register.byte_14);
                        sum._register.byte_15 = (Byte)(left._register.byte_15 + right._register.byte_15);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        sum._register.sbyte_0 = (SByte)(left._register.sbyte_0 + right._register.sbyte_0);
                        sum._register.sbyte_1 = (SByte)(left._register.sbyte_1 + right._register.sbyte_1);
                        sum._register.sbyte_2 = (SByte)(left._register.sbyte_2 + right._register.sbyte_2);
                        sum._register.sbyte_3 = (SByte)(left._register.sbyte_3 + right._register.sbyte_3);
                        sum._register.sbyte_4 = (SByte)(left._register.sbyte_4 + right._register.sbyte_4);
                        sum._register.sbyte_5 = (SByte)(left._register.sbyte_5 + right._register.sbyte_5);
                        sum._register.sbyte_6 = (SByte)(left._register.sbyte_6 + right._register.sbyte_6);
                        sum._register.sbyte_7 = (SByte)(left._register.sbyte_7 + right._register.sbyte_7);
                        sum._register.sbyte_8 = (SByte)(left._register.sbyte_8 + right._register.sbyte_8);
                        sum._register.sbyte_9 = (SByte)(left._register.sbyte_9 + right._register.sbyte_9);
                        sum._register.sbyte_10 = (SByte)(left._register.sbyte_10 + right._register.sbyte_10);
                        sum._register.sbyte_11 = (SByte)(left._register.sbyte_11 + right._register.sbyte_11);
                        sum._register.sbyte_12 = (SByte)(left._register.sbyte_12 + right._register.sbyte_12);
                        sum._register.sbyte_13 = (SByte)(left._register.sbyte_13 + right._register.sbyte_13);
                        sum._register.sbyte_14 = (SByte)(left._register.sbyte_14 + right._register.sbyte_14);
                        sum._register.sbyte_15 = (SByte)(left._register.sbyte_15 + right._register.sbyte_15);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        sum._register.uint16_0 = (UInt16)(left._register.uint16_0 + right._register.uint16_0);
                        sum._register.uint16_1 = (UInt16)(left._register.uint16_1 + right._register.uint16_1);
                        sum._register.uint16_2 = (UInt16)(left._register.uint16_2 + right._register.uint16_2);
                        sum._register.uint16_3 = (UInt16)(left._register.uint16_3 + right._register.uint16_3);
                        sum._register.uint16_4 = (UInt16)(left._register.uint16_4 + right._register.uint16_4);
                        sum._register.uint16_5 = (UInt16)(left._register.uint16_5 + right._register.uint16_5);
                        sum._register.uint16_6 = (UInt16)(left._register.uint16_6 + right._register.uint16_6);
                        sum._register.uint16_7 = (UInt16)(left._register.uint16_7 + right._register.uint16_7);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        sum._register.int16_0 = (Int16)(left._register.int16_0 + right._register.int16_0);
                        sum._register.int16_1 = (Int16)(left._register.int16_1 + right._register.int16_1);
                        sum._register.int16_2 = (Int16)(left._register.int16_2 + right._register.int16_2);
                        sum._register.int16_3 = (Int16)(left._register.int16_3 + right._register.int16_3);
                        sum._register.int16_4 = (Int16)(left._register.int16_4 + right._register.int16_4);
                        sum._register.int16_5 = (Int16)(left._register.int16_5 + right._register.int16_5);
                        sum._register.int16_6 = (Int16)(left._register.int16_6 + right._register.int16_6);
                        sum._register.int16_7 = (Int16)(left._register.int16_7 + right._register.int16_7);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        sum._register.uint32_0 = (UInt32)(left._register.uint32_0 + right._register.uint32_0);
                        sum._register.uint32_1 = (UInt32)(left._register.uint32_1 + right._register.uint32_1);
                        sum._register.uint32_2 = (UInt32)(left._register.uint32_2 + right._register.uint32_2);
                        sum._register.uint32_3 = (UInt32)(left._register.uint32_3 + right._register.uint32_3);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        sum._register.int32_0 = (Int32)(left._register.int32_0 + right._register.int32_0);
                        sum._register.int32_1 = (Int32)(left._register.int32_1 + right._register.int32_1);
                        sum._register.int32_2 = (Int32)(left._register.int32_2 + right._register.int32_2);
                        sum._register.int32_3 = (Int32)(left._register.int32_3 + right._register.int32_3);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        sum._register.uint64_0 = (UInt64)(left._register.uint64_0 + right._register.uint64_0);
                        sum._register.uint64_1 = (UInt64)(left._register.uint64_1 + right._register.uint64_1);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        sum._register.int64_0 = (Int64)(left._register.int64_0 + right._register.int64_0);
                        sum._register.int64_1 = (Int64)(left._register.int64_1 + right._register.int64_1);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        sum._register.single_0 = (Single)(left._register.single_0 + right._register.single_0);
                        sum._register.single_1 = (Single)(left._register.single_1 + right._register.single_1);
                        sum._register.single_2 = (Single)(left._register.single_2 + right._register.single_2);
                        sum._register.single_3 = (Single)(left._register.single_3 + right._register.single_3);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        sum._register.double_0 = (Double)(left._register.double_0 + right._register.double_0);
                        sum._register.double_1 = (Double)(left._register.double_1 + right._register.double_1);
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
                        throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                    }
                }
                else
                {
                    Vector<T> difference = new Vector<T>();
                    if (typeof(T) == typeof(Byte))
                    {
                        difference._register.byte_0 = (Byte)(left._register.byte_0 - right._register.byte_0);
                        difference._register.byte_1 = (Byte)(left._register.byte_1 - right._register.byte_1);
                        difference._register.byte_2 = (Byte)(left._register.byte_2 - right._register.byte_2);
                        difference._register.byte_3 = (Byte)(left._register.byte_3 - right._register.byte_3);
                        difference._register.byte_4 = (Byte)(left._register.byte_4 - right._register.byte_4);
                        difference._register.byte_5 = (Byte)(left._register.byte_5 - right._register.byte_5);
                        difference._register.byte_6 = (Byte)(left._register.byte_6 - right._register.byte_6);
                        difference._register.byte_7 = (Byte)(left._register.byte_7 - right._register.byte_7);
                        difference._register.byte_8 = (Byte)(left._register.byte_8 - right._register.byte_8);
                        difference._register.byte_9 = (Byte)(left._register.byte_9 - right._register.byte_9);
                        difference._register.byte_10 = (Byte)(left._register.byte_10 - right._register.byte_10);
                        difference._register.byte_11 = (Byte)(left._register.byte_11 - right._register.byte_11);
                        difference._register.byte_12 = (Byte)(left._register.byte_12 - right._register.byte_12);
                        difference._register.byte_13 = (Byte)(left._register.byte_13 - right._register.byte_13);
                        difference._register.byte_14 = (Byte)(left._register.byte_14 - right._register.byte_14);
                        difference._register.byte_15 = (Byte)(left._register.byte_15 - right._register.byte_15);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        difference._register.sbyte_0 = (SByte)(left._register.sbyte_0 - right._register.sbyte_0);
                        difference._register.sbyte_1 = (SByte)(left._register.sbyte_1 - right._register.sbyte_1);
                        difference._register.sbyte_2 = (SByte)(left._register.sbyte_2 - right._register.sbyte_2);
                        difference._register.sbyte_3 = (SByte)(left._register.sbyte_3 - right._register.sbyte_3);
                        difference._register.sbyte_4 = (SByte)(left._register.sbyte_4 - right._register.sbyte_4);
                        difference._register.sbyte_5 = (SByte)(left._register.sbyte_5 - right._register.sbyte_5);
                        difference._register.sbyte_6 = (SByte)(left._register.sbyte_6 - right._register.sbyte_6);
                        difference._register.sbyte_7 = (SByte)(left._register.sbyte_7 - right._register.sbyte_7);
                        difference._register.sbyte_8 = (SByte)(left._register.sbyte_8 - right._register.sbyte_8);
                        difference._register.sbyte_9 = (SByte)(left._register.sbyte_9 - right._register.sbyte_9);
                        difference._register.sbyte_10 = (SByte)(left._register.sbyte_10 - right._register.sbyte_10);
                        difference._register.sbyte_11 = (SByte)(left._register.sbyte_11 - right._register.sbyte_11);
                        difference._register.sbyte_12 = (SByte)(left._register.sbyte_12 - right._register.sbyte_12);
                        difference._register.sbyte_13 = (SByte)(left._register.sbyte_13 - right._register.sbyte_13);
                        difference._register.sbyte_14 = (SByte)(left._register.sbyte_14 - right._register.sbyte_14);
                        difference._register.sbyte_15 = (SByte)(left._register.sbyte_15 - right._register.sbyte_15);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        difference._register.uint16_0 = (UInt16)(left._register.uint16_0 - right._register.uint16_0);
                        difference._register.uint16_1 = (UInt16)(left._register.uint16_1 - right._register.uint16_1);
                        difference._register.uint16_2 = (UInt16)(left._register.uint16_2 - right._register.uint16_2);
                        difference._register.uint16_3 = (UInt16)(left._register.uint16_3 - right._register.uint16_3);
                        difference._register.uint16_4 = (UInt16)(left._register.uint16_4 - right._register.uint16_4);
                        difference._register.uint16_5 = (UInt16)(left._register.uint16_5 - right._register.uint16_5);
                        difference._register.uint16_6 = (UInt16)(left._register.uint16_6 - right._register.uint16_6);
                        difference._register.uint16_7 = (UInt16)(left._register.uint16_7 - right._register.uint16_7);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        difference._register.int16_0 = (Int16)(left._register.int16_0 - right._register.int16_0);
                        difference._register.int16_1 = (Int16)(left._register.int16_1 - right._register.int16_1);
                        difference._register.int16_2 = (Int16)(left._register.int16_2 - right._register.int16_2);
                        difference._register.int16_3 = (Int16)(left._register.int16_3 - right._register.int16_3);
                        difference._register.int16_4 = (Int16)(left._register.int16_4 - right._register.int16_4);
                        difference._register.int16_5 = (Int16)(left._register.int16_5 - right._register.int16_5);
                        difference._register.int16_6 = (Int16)(left._register.int16_6 - right._register.int16_6);
                        difference._register.int16_7 = (Int16)(left._register.int16_7 - right._register.int16_7);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        difference._register.uint32_0 = (UInt32)(left._register.uint32_0 - right._register.uint32_0);
                        difference._register.uint32_1 = (UInt32)(left._register.uint32_1 - right._register.uint32_1);
                        difference._register.uint32_2 = (UInt32)(left._register.uint32_2 - right._register.uint32_2);
                        difference._register.uint32_3 = (UInt32)(left._register.uint32_3 - right._register.uint32_3);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        difference._register.int32_0 = (Int32)(left._register.int32_0 - right._register.int32_0);
                        difference._register.int32_1 = (Int32)(left._register.int32_1 - right._register.int32_1);
                        difference._register.int32_2 = (Int32)(left._register.int32_2 - right._register.int32_2);
                        difference._register.int32_3 = (Int32)(left._register.int32_3 - right._register.int32_3);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        difference._register.uint64_0 = (UInt64)(left._register.uint64_0 - right._register.uint64_0);
                        difference._register.uint64_1 = (UInt64)(left._register.uint64_1 - right._register.uint64_1);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        difference._register.int64_0 = (Int64)(left._register.int64_0 - right._register.int64_0);
                        difference._register.int64_1 = (Int64)(left._register.int64_1 - right._register.int64_1);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        difference._register.single_0 = (Single)(left._register.single_0 - right._register.single_0);
                        difference._register.single_1 = (Single)(left._register.single_1 - right._register.single_1);
                        difference._register.single_2 = (Single)(left._register.single_2 - right._register.single_2);
                        difference._register.single_3 = (Single)(left._register.single_3 - right._register.single_3);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        difference._register.double_0 = (Double)(left._register.double_0 - right._register.double_0);
                        difference._register.double_1 = (Double)(left._register.double_1 - right._register.double_1);
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
                        throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                    }
                }
                else
                {
                    Vector<T> product = new Vector<T>();
                    if (typeof(T) == typeof(Byte))
                    {
                        product._register.byte_0 = (Byte)(left._register.byte_0 * right._register.byte_0);
                        product._register.byte_1 = (Byte)(left._register.byte_1 * right._register.byte_1);
                        product._register.byte_2 = (Byte)(left._register.byte_2 * right._register.byte_2);
                        product._register.byte_3 = (Byte)(left._register.byte_3 * right._register.byte_3);
                        product._register.byte_4 = (Byte)(left._register.byte_4 * right._register.byte_4);
                        product._register.byte_5 = (Byte)(left._register.byte_5 * right._register.byte_5);
                        product._register.byte_6 = (Byte)(left._register.byte_6 * right._register.byte_6);
                        product._register.byte_7 = (Byte)(left._register.byte_7 * right._register.byte_7);
                        product._register.byte_8 = (Byte)(left._register.byte_8 * right._register.byte_8);
                        product._register.byte_9 = (Byte)(left._register.byte_9 * right._register.byte_9);
                        product._register.byte_10 = (Byte)(left._register.byte_10 * right._register.byte_10);
                        product._register.byte_11 = (Byte)(left._register.byte_11 * right._register.byte_11);
                        product._register.byte_12 = (Byte)(left._register.byte_12 * right._register.byte_12);
                        product._register.byte_13 = (Byte)(left._register.byte_13 * right._register.byte_13);
                        product._register.byte_14 = (Byte)(left._register.byte_14 * right._register.byte_14);
                        product._register.byte_15 = (Byte)(left._register.byte_15 * right._register.byte_15);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        product._register.sbyte_0 = (SByte)(left._register.sbyte_0 * right._register.sbyte_0);
                        product._register.sbyte_1 = (SByte)(left._register.sbyte_1 * right._register.sbyte_1);
                        product._register.sbyte_2 = (SByte)(left._register.sbyte_2 * right._register.sbyte_2);
                        product._register.sbyte_3 = (SByte)(left._register.sbyte_3 * right._register.sbyte_3);
                        product._register.sbyte_4 = (SByte)(left._register.sbyte_4 * right._register.sbyte_4);
                        product._register.sbyte_5 = (SByte)(left._register.sbyte_5 * right._register.sbyte_5);
                        product._register.sbyte_6 = (SByte)(left._register.sbyte_6 * right._register.sbyte_6);
                        product._register.sbyte_7 = (SByte)(left._register.sbyte_7 * right._register.sbyte_7);
                        product._register.sbyte_8 = (SByte)(left._register.sbyte_8 * right._register.sbyte_8);
                        product._register.sbyte_9 = (SByte)(left._register.sbyte_9 * right._register.sbyte_9);
                        product._register.sbyte_10 = (SByte)(left._register.sbyte_10 * right._register.sbyte_10);
                        product._register.sbyte_11 = (SByte)(left._register.sbyte_11 * right._register.sbyte_11);
                        product._register.sbyte_12 = (SByte)(left._register.sbyte_12 * right._register.sbyte_12);
                        product._register.sbyte_13 = (SByte)(left._register.sbyte_13 * right._register.sbyte_13);
                        product._register.sbyte_14 = (SByte)(left._register.sbyte_14 * right._register.sbyte_14);
                        product._register.sbyte_15 = (SByte)(left._register.sbyte_15 * right._register.sbyte_15);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        product._register.uint16_0 = (UInt16)(left._register.uint16_0 * right._register.uint16_0);
                        product._register.uint16_1 = (UInt16)(left._register.uint16_1 * right._register.uint16_1);
                        product._register.uint16_2 = (UInt16)(left._register.uint16_2 * right._register.uint16_2);
                        product._register.uint16_3 = (UInt16)(left._register.uint16_3 * right._register.uint16_3);
                        product._register.uint16_4 = (UInt16)(left._register.uint16_4 * right._register.uint16_4);
                        product._register.uint16_5 = (UInt16)(left._register.uint16_5 * right._register.uint16_5);
                        product._register.uint16_6 = (UInt16)(left._register.uint16_6 * right._register.uint16_6);
                        product._register.uint16_7 = (UInt16)(left._register.uint16_7 * right._register.uint16_7);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        product._register.int16_0 = (Int16)(left._register.int16_0 * right._register.int16_0);
                        product._register.int16_1 = (Int16)(left._register.int16_1 * right._register.int16_1);
                        product._register.int16_2 = (Int16)(left._register.int16_2 * right._register.int16_2);
                        product._register.int16_3 = (Int16)(left._register.int16_3 * right._register.int16_3);
                        product._register.int16_4 = (Int16)(left._register.int16_4 * right._register.int16_4);
                        product._register.int16_5 = (Int16)(left._register.int16_5 * right._register.int16_5);
                        product._register.int16_6 = (Int16)(left._register.int16_6 * right._register.int16_6);
                        product._register.int16_7 = (Int16)(left._register.int16_7 * right._register.int16_7);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        product._register.uint32_0 = (UInt32)(left._register.uint32_0 * right._register.uint32_0);
                        product._register.uint32_1 = (UInt32)(left._register.uint32_1 * right._register.uint32_1);
                        product._register.uint32_2 = (UInt32)(left._register.uint32_2 * right._register.uint32_2);
                        product._register.uint32_3 = (UInt32)(left._register.uint32_3 * right._register.uint32_3);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        product._register.int32_0 = (Int32)(left._register.int32_0 * right._register.int32_0);
                        product._register.int32_1 = (Int32)(left._register.int32_1 * right._register.int32_1);
                        product._register.int32_2 = (Int32)(left._register.int32_2 * right._register.int32_2);
                        product._register.int32_3 = (Int32)(left._register.int32_3 * right._register.int32_3);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        product._register.uint64_0 = (UInt64)(left._register.uint64_0 * right._register.uint64_0);
                        product._register.uint64_1 = (UInt64)(left._register.uint64_1 * right._register.uint64_1);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        product._register.int64_0 = (Int64)(left._register.int64_0 * right._register.int64_0);
                        product._register.int64_1 = (Int64)(left._register.int64_1 * right._register.int64_1);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        product._register.single_0 = (Single)(left._register.single_0 * right._register.single_0);
                        product._register.single_1 = (Single)(left._register.single_1 * right._register.single_1);
                        product._register.single_2 = (Single)(left._register.single_2 * right._register.single_2);
                        product._register.single_3 = (Single)(left._register.single_3 * right._register.single_3);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        product._register.double_0 = (Double)(left._register.double_0 * right._register.double_0);
                        product._register.double_1 = (Double)(left._register.double_1 * right._register.double_1);
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
                        product._register.byte_0 = (Byte)(value._register.byte_0 * (Byte)(object)factor);
                        product._register.byte_1 = (Byte)(value._register.byte_1 * (Byte)(object)factor);
                        product._register.byte_2 = (Byte)(value._register.byte_2 * (Byte)(object)factor);
                        product._register.byte_3 = (Byte)(value._register.byte_3 * (Byte)(object)factor);
                        product._register.byte_4 = (Byte)(value._register.byte_4 * (Byte)(object)factor);
                        product._register.byte_5 = (Byte)(value._register.byte_5 * (Byte)(object)factor);
                        product._register.byte_6 = (Byte)(value._register.byte_6 * (Byte)(object)factor);
                        product._register.byte_7 = (Byte)(value._register.byte_7 * (Byte)(object)factor);
                        product._register.byte_8 = (Byte)(value._register.byte_8 * (Byte)(object)factor);
                        product._register.byte_9 = (Byte)(value._register.byte_9 * (Byte)(object)factor);
                        product._register.byte_10 = (Byte)(value._register.byte_10 * (Byte)(object)factor);
                        product._register.byte_11 = (Byte)(value._register.byte_11 * (Byte)(object)factor);
                        product._register.byte_12 = (Byte)(value._register.byte_12 * (Byte)(object)factor);
                        product._register.byte_13 = (Byte)(value._register.byte_13 * (Byte)(object)factor);
                        product._register.byte_14 = (Byte)(value._register.byte_14 * (Byte)(object)factor);
                        product._register.byte_15 = (Byte)(value._register.byte_15 * (Byte)(object)factor);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        product._register.sbyte_0 = (SByte)(value._register.sbyte_0 * (SByte)(object)factor);
                        product._register.sbyte_1 = (SByte)(value._register.sbyte_1 * (SByte)(object)factor);
                        product._register.sbyte_2 = (SByte)(value._register.sbyte_2 * (SByte)(object)factor);
                        product._register.sbyte_3 = (SByte)(value._register.sbyte_3 * (SByte)(object)factor);
                        product._register.sbyte_4 = (SByte)(value._register.sbyte_4 * (SByte)(object)factor);
                        product._register.sbyte_5 = (SByte)(value._register.sbyte_5 * (SByte)(object)factor);
                        product._register.sbyte_6 = (SByte)(value._register.sbyte_6 * (SByte)(object)factor);
                        product._register.sbyte_7 = (SByte)(value._register.sbyte_7 * (SByte)(object)factor);
                        product._register.sbyte_8 = (SByte)(value._register.sbyte_8 * (SByte)(object)factor);
                        product._register.sbyte_9 = (SByte)(value._register.sbyte_9 * (SByte)(object)factor);
                        product._register.sbyte_10 = (SByte)(value._register.sbyte_10 * (SByte)(object)factor);
                        product._register.sbyte_11 = (SByte)(value._register.sbyte_11 * (SByte)(object)factor);
                        product._register.sbyte_12 = (SByte)(value._register.sbyte_12 * (SByte)(object)factor);
                        product._register.sbyte_13 = (SByte)(value._register.sbyte_13 * (SByte)(object)factor);
                        product._register.sbyte_14 = (SByte)(value._register.sbyte_14 * (SByte)(object)factor);
                        product._register.sbyte_15 = (SByte)(value._register.sbyte_15 * (SByte)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        product._register.uint16_0 = (UInt16)(value._register.uint16_0 * (UInt16)(object)factor);
                        product._register.uint16_1 = (UInt16)(value._register.uint16_1 * (UInt16)(object)factor);
                        product._register.uint16_2 = (UInt16)(value._register.uint16_2 * (UInt16)(object)factor);
                        product._register.uint16_3 = (UInt16)(value._register.uint16_3 * (UInt16)(object)factor);
                        product._register.uint16_4 = (UInt16)(value._register.uint16_4 * (UInt16)(object)factor);
                        product._register.uint16_5 = (UInt16)(value._register.uint16_5 * (UInt16)(object)factor);
                        product._register.uint16_6 = (UInt16)(value._register.uint16_6 * (UInt16)(object)factor);
                        product._register.uint16_7 = (UInt16)(value._register.uint16_7 * (UInt16)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        product._register.int16_0 = (Int16)(value._register.int16_0 * (Int16)(object)factor);
                        product._register.int16_1 = (Int16)(value._register.int16_1 * (Int16)(object)factor);
                        product._register.int16_2 = (Int16)(value._register.int16_2 * (Int16)(object)factor);
                        product._register.int16_3 = (Int16)(value._register.int16_3 * (Int16)(object)factor);
                        product._register.int16_4 = (Int16)(value._register.int16_4 * (Int16)(object)factor);
                        product._register.int16_5 = (Int16)(value._register.int16_5 * (Int16)(object)factor);
                        product._register.int16_6 = (Int16)(value._register.int16_6 * (Int16)(object)factor);
                        product._register.int16_7 = (Int16)(value._register.int16_7 * (Int16)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        product._register.uint32_0 = (UInt32)(value._register.uint32_0 * (UInt32)(object)factor);
                        product._register.uint32_1 = (UInt32)(value._register.uint32_1 * (UInt32)(object)factor);
                        product._register.uint32_2 = (UInt32)(value._register.uint32_2 * (UInt32)(object)factor);
                        product._register.uint32_3 = (UInt32)(value._register.uint32_3 * (UInt32)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        product._register.int32_0 = (Int32)(value._register.int32_0 * (Int32)(object)factor);
                        product._register.int32_1 = (Int32)(value._register.int32_1 * (Int32)(object)factor);
                        product._register.int32_2 = (Int32)(value._register.int32_2 * (Int32)(object)factor);
                        product._register.int32_3 = (Int32)(value._register.int32_3 * (Int32)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        product._register.uint64_0 = (UInt64)(value._register.uint64_0 * (UInt64)(object)factor);
                        product._register.uint64_1 = (UInt64)(value._register.uint64_1 * (UInt64)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        product._register.int64_0 = (Int64)(value._register.int64_0 * (Int64)(object)factor);
                        product._register.int64_1 = (Int64)(value._register.int64_1 * (Int64)(object)factor);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        product._register.single_0 = (Single)(value._register.single_0 * (Single)(object)factor);
                        product._register.single_1 = (Single)(value._register.single_1 * (Single)(object)factor);
                        product._register.single_2 = (Single)(value._register.single_2 * (Single)(object)factor);
                        product._register.single_3 = (Single)(value._register.single_3 * (Single)(object)factor);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        product._register.double_0 = (Double)(value._register.double_0 * (Double)(object)factor);
                        product._register.double_1 = (Double)(value._register.double_1 * (Double)(object)factor);
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
                        product._register.byte_0 = (Byte)(value._register.byte_0 * (Byte)(object)factor);
                        product._register.byte_1 = (Byte)(value._register.byte_1 * (Byte)(object)factor);
                        product._register.byte_2 = (Byte)(value._register.byte_2 * (Byte)(object)factor);
                        product._register.byte_3 = (Byte)(value._register.byte_3 * (Byte)(object)factor);
                        product._register.byte_4 = (Byte)(value._register.byte_4 * (Byte)(object)factor);
                        product._register.byte_5 = (Byte)(value._register.byte_5 * (Byte)(object)factor);
                        product._register.byte_6 = (Byte)(value._register.byte_6 * (Byte)(object)factor);
                        product._register.byte_7 = (Byte)(value._register.byte_7 * (Byte)(object)factor);
                        product._register.byte_8 = (Byte)(value._register.byte_8 * (Byte)(object)factor);
                        product._register.byte_9 = (Byte)(value._register.byte_9 * (Byte)(object)factor);
                        product._register.byte_10 = (Byte)(value._register.byte_10 * (Byte)(object)factor);
                        product._register.byte_11 = (Byte)(value._register.byte_11 * (Byte)(object)factor);
                        product._register.byte_12 = (Byte)(value._register.byte_12 * (Byte)(object)factor);
                        product._register.byte_13 = (Byte)(value._register.byte_13 * (Byte)(object)factor);
                        product._register.byte_14 = (Byte)(value._register.byte_14 * (Byte)(object)factor);
                        product._register.byte_15 = (Byte)(value._register.byte_15 * (Byte)(object)factor);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        product._register.sbyte_0 = (SByte)(value._register.sbyte_0 * (SByte)(object)factor);
                        product._register.sbyte_1 = (SByte)(value._register.sbyte_1 * (SByte)(object)factor);
                        product._register.sbyte_2 = (SByte)(value._register.sbyte_2 * (SByte)(object)factor);
                        product._register.sbyte_3 = (SByte)(value._register.sbyte_3 * (SByte)(object)factor);
                        product._register.sbyte_4 = (SByte)(value._register.sbyte_4 * (SByte)(object)factor);
                        product._register.sbyte_5 = (SByte)(value._register.sbyte_5 * (SByte)(object)factor);
                        product._register.sbyte_6 = (SByte)(value._register.sbyte_6 * (SByte)(object)factor);
                        product._register.sbyte_7 = (SByte)(value._register.sbyte_7 * (SByte)(object)factor);
                        product._register.sbyte_8 = (SByte)(value._register.sbyte_8 * (SByte)(object)factor);
                        product._register.sbyte_9 = (SByte)(value._register.sbyte_9 * (SByte)(object)factor);
                        product._register.sbyte_10 = (SByte)(value._register.sbyte_10 * (SByte)(object)factor);
                        product._register.sbyte_11 = (SByte)(value._register.sbyte_11 * (SByte)(object)factor);
                        product._register.sbyte_12 = (SByte)(value._register.sbyte_12 * (SByte)(object)factor);
                        product._register.sbyte_13 = (SByte)(value._register.sbyte_13 * (SByte)(object)factor);
                        product._register.sbyte_14 = (SByte)(value._register.sbyte_14 * (SByte)(object)factor);
                        product._register.sbyte_15 = (SByte)(value._register.sbyte_15 * (SByte)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        product._register.uint16_0 = (UInt16)(value._register.uint16_0 * (UInt16)(object)factor);
                        product._register.uint16_1 = (UInt16)(value._register.uint16_1 * (UInt16)(object)factor);
                        product._register.uint16_2 = (UInt16)(value._register.uint16_2 * (UInt16)(object)factor);
                        product._register.uint16_3 = (UInt16)(value._register.uint16_3 * (UInt16)(object)factor);
                        product._register.uint16_4 = (UInt16)(value._register.uint16_4 * (UInt16)(object)factor);
                        product._register.uint16_5 = (UInt16)(value._register.uint16_5 * (UInt16)(object)factor);
                        product._register.uint16_6 = (UInt16)(value._register.uint16_6 * (UInt16)(object)factor);
                        product._register.uint16_7 = (UInt16)(value._register.uint16_7 * (UInt16)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        product._register.int16_0 = (Int16)(value._register.int16_0 * (Int16)(object)factor);
                        product._register.int16_1 = (Int16)(value._register.int16_1 * (Int16)(object)factor);
                        product._register.int16_2 = (Int16)(value._register.int16_2 * (Int16)(object)factor);
                        product._register.int16_3 = (Int16)(value._register.int16_3 * (Int16)(object)factor);
                        product._register.int16_4 = (Int16)(value._register.int16_4 * (Int16)(object)factor);
                        product._register.int16_5 = (Int16)(value._register.int16_5 * (Int16)(object)factor);
                        product._register.int16_6 = (Int16)(value._register.int16_6 * (Int16)(object)factor);
                        product._register.int16_7 = (Int16)(value._register.int16_7 * (Int16)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        product._register.uint32_0 = (UInt32)(value._register.uint32_0 * (UInt32)(object)factor);
                        product._register.uint32_1 = (UInt32)(value._register.uint32_1 * (UInt32)(object)factor);
                        product._register.uint32_2 = (UInt32)(value._register.uint32_2 * (UInt32)(object)factor);
                        product._register.uint32_3 = (UInt32)(value._register.uint32_3 * (UInt32)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        product._register.int32_0 = (Int32)(value._register.int32_0 * (Int32)(object)factor);
                        product._register.int32_1 = (Int32)(value._register.int32_1 * (Int32)(object)factor);
                        product._register.int32_2 = (Int32)(value._register.int32_2 * (Int32)(object)factor);
                        product._register.int32_3 = (Int32)(value._register.int32_3 * (Int32)(object)factor);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        product._register.uint64_0 = (UInt64)(value._register.uint64_0 * (UInt64)(object)factor);
                        product._register.uint64_1 = (UInt64)(value._register.uint64_1 * (UInt64)(object)factor);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        product._register.int64_0 = (Int64)(value._register.int64_0 * (Int64)(object)factor);
                        product._register.int64_1 = (Int64)(value._register.int64_1 * (Int64)(object)factor);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        product._register.single_0 = (Single)(value._register.single_0 * (Single)(object)factor);
                        product._register.single_1 = (Single)(value._register.single_1 * (Single)(object)factor);
                        product._register.single_2 = (Single)(value._register.single_2 * (Single)(object)factor);
                        product._register.single_3 = (Single)(value._register.single_3 * (Single)(object)factor);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        product._register.double_0 = (Double)(value._register.double_0 * (Double)(object)factor);
                        product._register.double_1 = (Double)(value._register.double_1 * (Double)(object)factor);
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
                        throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                    }
                }
                else
                {
                    Vector<T> quotient = new Vector<T>();
                    if (typeof(T) == typeof(Byte))
                    {
                        quotient._register.byte_0 = (Byte)(left._register.byte_0 / right._register.byte_0);
                        quotient._register.byte_1 = (Byte)(left._register.byte_1 / right._register.byte_1);
                        quotient._register.byte_2 = (Byte)(left._register.byte_2 / right._register.byte_2);
                        quotient._register.byte_3 = (Byte)(left._register.byte_3 / right._register.byte_3);
                        quotient._register.byte_4 = (Byte)(left._register.byte_4 / right._register.byte_4);
                        quotient._register.byte_5 = (Byte)(left._register.byte_5 / right._register.byte_5);
                        quotient._register.byte_6 = (Byte)(left._register.byte_6 / right._register.byte_6);
                        quotient._register.byte_7 = (Byte)(left._register.byte_7 / right._register.byte_7);
                        quotient._register.byte_8 = (Byte)(left._register.byte_8 / right._register.byte_8);
                        quotient._register.byte_9 = (Byte)(left._register.byte_9 / right._register.byte_9);
                        quotient._register.byte_10 = (Byte)(left._register.byte_10 / right._register.byte_10);
                        quotient._register.byte_11 = (Byte)(left._register.byte_11 / right._register.byte_11);
                        quotient._register.byte_12 = (Byte)(left._register.byte_12 / right._register.byte_12);
                        quotient._register.byte_13 = (Byte)(left._register.byte_13 / right._register.byte_13);
                        quotient._register.byte_14 = (Byte)(left._register.byte_14 / right._register.byte_14);
                        quotient._register.byte_15 = (Byte)(left._register.byte_15 / right._register.byte_15);
                    }
                    else if (typeof(T) == typeof(SByte))
                    {
                        quotient._register.sbyte_0 = (SByte)(left._register.sbyte_0 / right._register.sbyte_0);
                        quotient._register.sbyte_1 = (SByte)(left._register.sbyte_1 / right._register.sbyte_1);
                        quotient._register.sbyte_2 = (SByte)(left._register.sbyte_2 / right._register.sbyte_2);
                        quotient._register.sbyte_3 = (SByte)(left._register.sbyte_3 / right._register.sbyte_3);
                        quotient._register.sbyte_4 = (SByte)(left._register.sbyte_4 / right._register.sbyte_4);
                        quotient._register.sbyte_5 = (SByte)(left._register.sbyte_5 / right._register.sbyte_5);
                        quotient._register.sbyte_6 = (SByte)(left._register.sbyte_6 / right._register.sbyte_6);
                        quotient._register.sbyte_7 = (SByte)(left._register.sbyte_7 / right._register.sbyte_7);
                        quotient._register.sbyte_8 = (SByte)(left._register.sbyte_8 / right._register.sbyte_8);
                        quotient._register.sbyte_9 = (SByte)(left._register.sbyte_9 / right._register.sbyte_9);
                        quotient._register.sbyte_10 = (SByte)(left._register.sbyte_10 / right._register.sbyte_10);
                        quotient._register.sbyte_11 = (SByte)(left._register.sbyte_11 / right._register.sbyte_11);
                        quotient._register.sbyte_12 = (SByte)(left._register.sbyte_12 / right._register.sbyte_12);
                        quotient._register.sbyte_13 = (SByte)(left._register.sbyte_13 / right._register.sbyte_13);
                        quotient._register.sbyte_14 = (SByte)(left._register.sbyte_14 / right._register.sbyte_14);
                        quotient._register.sbyte_15 = (SByte)(left._register.sbyte_15 / right._register.sbyte_15);
                    }
                    else if (typeof(T) == typeof(UInt16))
                    {
                        quotient._register.uint16_0 = (UInt16)(left._register.uint16_0 / right._register.uint16_0);
                        quotient._register.uint16_1 = (UInt16)(left._register.uint16_1 / right._register.uint16_1);
                        quotient._register.uint16_2 = (UInt16)(left._register.uint16_2 / right._register.uint16_2);
                        quotient._register.uint16_3 = (UInt16)(left._register.uint16_3 / right._register.uint16_3);
                        quotient._register.uint16_4 = (UInt16)(left._register.uint16_4 / right._register.uint16_4);
                        quotient._register.uint16_5 = (UInt16)(left._register.uint16_5 / right._register.uint16_5);
                        quotient._register.uint16_6 = (UInt16)(left._register.uint16_6 / right._register.uint16_6);
                        quotient._register.uint16_7 = (UInt16)(left._register.uint16_7 / right._register.uint16_7);
                    }
                    else if (typeof(T) == typeof(Int16))
                    {
                        quotient._register.int16_0 = (Int16)(left._register.int16_0 / right._register.int16_0);
                        quotient._register.int16_1 = (Int16)(left._register.int16_1 / right._register.int16_1);
                        quotient._register.int16_2 = (Int16)(left._register.int16_2 / right._register.int16_2);
                        quotient._register.int16_3 = (Int16)(left._register.int16_3 / right._register.int16_3);
                        quotient._register.int16_4 = (Int16)(left._register.int16_4 / right._register.int16_4);
                        quotient._register.int16_5 = (Int16)(left._register.int16_5 / right._register.int16_5);
                        quotient._register.int16_6 = (Int16)(left._register.int16_6 / right._register.int16_6);
                        quotient._register.int16_7 = (Int16)(left._register.int16_7 / right._register.int16_7);
                    }
                    else if (typeof(T) == typeof(UInt32))
                    {
                        quotient._register.uint32_0 = (UInt32)(left._register.uint32_0 / right._register.uint32_0);
                        quotient._register.uint32_1 = (UInt32)(left._register.uint32_1 / right._register.uint32_1);
                        quotient._register.uint32_2 = (UInt32)(left._register.uint32_2 / right._register.uint32_2);
                        quotient._register.uint32_3 = (UInt32)(left._register.uint32_3 / right._register.uint32_3);
                    }
                    else if (typeof(T) == typeof(Int32))
                    {
                        quotient._register.int32_0 = (Int32)(left._register.int32_0 / right._register.int32_0);
                        quotient._register.int32_1 = (Int32)(left._register.int32_1 / right._register.int32_1);
                        quotient._register.int32_2 = (Int32)(left._register.int32_2 / right._register.int32_2);
                        quotient._register.int32_3 = (Int32)(left._register.int32_3 / right._register.int32_3);
                    }
                    else if (typeof(T) == typeof(UInt64))
                    {
                        quotient._register.uint64_0 = (UInt64)(left._register.uint64_0 / right._register.uint64_0);
                        quotient._register.uint64_1 = (UInt64)(left._register.uint64_1 / right._register.uint64_1);
                    }
                    else if (typeof(T) == typeof(Int64))
                    {
                        quotient._register.int64_0 = (Int64)(left._register.int64_0 / right._register.int64_0);
                        quotient._register.int64_1 = (Int64)(left._register.int64_1 / right._register.int64_1);
                    }
                    else if (typeof(T) == typeof(Single))
                    {
                        quotient._register.single_0 = (Single)(left._register.single_0 / right._register.single_0);
                        quotient._register.single_1 = (Single)(left._register.single_1 / right._register.single_1);
                        quotient._register.single_2 = (Single)(left._register.single_2 / right._register.single_2);
                        quotient._register.single_3 = (Single)(left._register.single_3 / right._register.single_3);
                    }
                    else if (typeof(T) == typeof(Double))
                    {
                        quotient._register.double_0 = (Double)(left._register.double_0 / right._register.double_0);
                        quotient._register.double_1 = (Double)(left._register.double_1 / right._register.double_1);
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
                    Int64* resultBase = &result._register.int64_0;
                    Int64* leftBase = &left._register.int64_0;
                    Int64* rightBase = &right._register.int64_0;
                    for (int g = 0; g < Vector<Int64>.Count; g++)
                    {
                        resultBase[g] = leftBase[g] & rightBase[g];
                    }
                }
                else
                {
                    result._register.int64_0 = left._register.int64_0 & right._register.int64_0;
                    result._register.int64_1 = left._register.int64_1 & right._register.int64_1;
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
                    Int64* resultBase = &result._register.int64_0;
                    Int64* leftBase = &left._register.int64_0;
                    Int64* rightBase = &right._register.int64_0;
                    for (int g = 0; g < Vector<Int64>.Count; g++)
                    {
                        resultBase[g] = leftBase[g] | rightBase[g];
                    }
                }
                else
                {
                    result._register.int64_0 = left._register.int64_0 | right._register.int64_0;
                    result._register.int64_1 = left._register.int64_1 | right._register.int64_1;
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
                    Int64* resultBase = &result._register.int64_0;
                    Int64* leftBase = &left._register.int64_0;
                    Int64* rightBase = &right._register.int64_0;
                    for (int g = 0; g < Vector<Int64>.Count; g++)
                    {
                        resultBase[g] = leftBase[g] ^ rightBase[g];
                    }
                }
                else
                {
                    result._register.int64_0 = left._register.int64_0 ^ right._register.int64_0;
                    result._register.int64_1 = left._register.int64_1 ^ right._register.int64_1;
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
            return s_allOnes ^ value;
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
        /// Returns a boolean indicating whether any single pair of elements in the given vectors are equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if any element pairs are equal; False if no element pairs are equal.</returns>
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
            return new Vector<Byte>(ref value._register);
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
            return new Vector<SByte>(ref value._register);
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
            return new Vector<UInt16>(ref value._register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [JitIntrinsic]
        public static explicit operator Vector<Int16>(Vector<T> value)
        {
            return new Vector<Int16>(ref value._register);
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
            return new Vector<UInt32>(ref value._register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [JitIntrinsic]
        public static explicit operator Vector<Int32>(Vector<T> value)
        {
            return new Vector<Int32>(ref value._register);
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
            return new Vector<UInt64>(ref value._register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [JitIntrinsic]
        public static explicit operator Vector<Int64>(Vector<T> value)
        {
            return new Vector<Int64>(ref value._register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [JitIntrinsic]
        public static explicit operator Vector<Single>(Vector<T> value)
        {
            return new Vector<Single>(ref value._register);
        }

        /// <summary>
        /// Reinterprets the bits of the given vector into those of another type.
        /// </summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [JitIntrinsic]
        public static explicit operator Vector<Double>(Vector<T> value)
        {
            return new Vector<Double>(ref value._register);
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
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                }
            }
            else
            {
                Register register = new Register();
                if (typeof(T) == typeof(Byte))
                {
                    register.byte_0 = left._register.byte_0 == right._register.byte_0 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_1 = left._register.byte_1 == right._register.byte_1 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_2 = left._register.byte_2 == right._register.byte_2 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_3 = left._register.byte_3 == right._register.byte_3 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_4 = left._register.byte_4 == right._register.byte_4 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_5 = left._register.byte_5 == right._register.byte_5 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_6 = left._register.byte_6 == right._register.byte_6 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_7 = left._register.byte_7 == right._register.byte_7 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_8 = left._register.byte_8 == right._register.byte_8 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_9 = left._register.byte_9 == right._register.byte_9 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_10 = left._register.byte_10 == right._register.byte_10 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_11 = left._register.byte_11 == right._register.byte_11 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_12 = left._register.byte_12 == right._register.byte_12 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_13 = left._register.byte_13 == right._register.byte_13 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_14 = left._register.byte_14 == right._register.byte_14 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_15 = left._register.byte_15 == right._register.byte_15 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    register.sbyte_0 = left._register.sbyte_0 == right._register.sbyte_0 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_1 = left._register.sbyte_1 == right._register.sbyte_1 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_2 = left._register.sbyte_2 == right._register.sbyte_2 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_3 = left._register.sbyte_3 == right._register.sbyte_3 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_4 = left._register.sbyte_4 == right._register.sbyte_4 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_5 = left._register.sbyte_5 == right._register.sbyte_5 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_6 = left._register.sbyte_6 == right._register.sbyte_6 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_7 = left._register.sbyte_7 == right._register.sbyte_7 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_8 = left._register.sbyte_8 == right._register.sbyte_8 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_9 = left._register.sbyte_9 == right._register.sbyte_9 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_10 = left._register.sbyte_10 == right._register.sbyte_10 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_11 = left._register.sbyte_11 == right._register.sbyte_11 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_12 = left._register.sbyte_12 == right._register.sbyte_12 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_13 = left._register.sbyte_13 == right._register.sbyte_13 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_14 = left._register.sbyte_14 == right._register.sbyte_14 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_15 = left._register.sbyte_15 == right._register.sbyte_15 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    register.uint16_0 = left._register.uint16_0 == right._register.uint16_0 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_1 = left._register.uint16_1 == right._register.uint16_1 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_2 = left._register.uint16_2 == right._register.uint16_2 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_3 = left._register.uint16_3 == right._register.uint16_3 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_4 = left._register.uint16_4 == right._register.uint16_4 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_5 = left._register.uint16_5 == right._register.uint16_5 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_6 = left._register.uint16_6 == right._register.uint16_6 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_7 = left._register.uint16_7 == right._register.uint16_7 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    register.int16_0 = left._register.int16_0 == right._register.int16_0 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_1 = left._register.int16_1 == right._register.int16_1 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_2 = left._register.int16_2 == right._register.int16_2 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_3 = left._register.int16_3 == right._register.int16_3 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_4 = left._register.int16_4 == right._register.int16_4 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_5 = left._register.int16_5 == right._register.int16_5 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_6 = left._register.int16_6 == right._register.int16_6 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_7 = left._register.int16_7 == right._register.int16_7 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    register.uint32_0 = left._register.uint32_0 == right._register.uint32_0 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_1 = left._register.uint32_1 == right._register.uint32_1 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_2 = left._register.uint32_2 == right._register.uint32_2 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_3 = left._register.uint32_3 == right._register.uint32_3 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    register.int32_0 = left._register.int32_0 == right._register.int32_0 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_1 = left._register.int32_1 == right._register.int32_1 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_2 = left._register.int32_2 == right._register.int32_2 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_3 = left._register.int32_3 == right._register.int32_3 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    register.uint64_0 = left._register.uint64_0 == right._register.uint64_0 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    register.uint64_1 = left._register.uint64_1 == right._register.uint64_1 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    register.int64_0 = left._register.int64_0 == right._register.int64_0 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    register.int64_1 = left._register.int64_1 == right._register.int64_1 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Single))
                {
                    register.single_0 = left._register.single_0 == right._register.single_0 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_1 = left._register.single_1 == right._register.single_1 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_2 = left._register.single_2 == right._register.single_2 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_3 = left._register.single_3 == right._register.single_3 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Double))
                {
                    register.double_0 = left._register.double_0 == right._register.double_0 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    register.double_1 = left._register.double_1 == right._register.double_1 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    return new Vector<T>(ref register);
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                }
            }
            else
            {
                Register register = new Register();
                if (typeof(T) == typeof(Byte))
                {
                    register.byte_0 = left._register.byte_0 < right._register.byte_0 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_1 = left._register.byte_1 < right._register.byte_1 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_2 = left._register.byte_2 < right._register.byte_2 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_3 = left._register.byte_3 < right._register.byte_3 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_4 = left._register.byte_4 < right._register.byte_4 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_5 = left._register.byte_5 < right._register.byte_5 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_6 = left._register.byte_6 < right._register.byte_6 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_7 = left._register.byte_7 < right._register.byte_7 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_8 = left._register.byte_8 < right._register.byte_8 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_9 = left._register.byte_9 < right._register.byte_9 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_10 = left._register.byte_10 < right._register.byte_10 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_11 = left._register.byte_11 < right._register.byte_11 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_12 = left._register.byte_12 < right._register.byte_12 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_13 = left._register.byte_13 < right._register.byte_13 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_14 = left._register.byte_14 < right._register.byte_14 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_15 = left._register.byte_15 < right._register.byte_15 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    register.sbyte_0 = left._register.sbyte_0 < right._register.sbyte_0 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_1 = left._register.sbyte_1 < right._register.sbyte_1 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_2 = left._register.sbyte_2 < right._register.sbyte_2 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_3 = left._register.sbyte_3 < right._register.sbyte_3 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_4 = left._register.sbyte_4 < right._register.sbyte_4 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_5 = left._register.sbyte_5 < right._register.sbyte_5 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_6 = left._register.sbyte_6 < right._register.sbyte_6 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_7 = left._register.sbyte_7 < right._register.sbyte_7 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_8 = left._register.sbyte_8 < right._register.sbyte_8 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_9 = left._register.sbyte_9 < right._register.sbyte_9 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_10 = left._register.sbyte_10 < right._register.sbyte_10 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_11 = left._register.sbyte_11 < right._register.sbyte_11 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_12 = left._register.sbyte_12 < right._register.sbyte_12 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_13 = left._register.sbyte_13 < right._register.sbyte_13 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_14 = left._register.sbyte_14 < right._register.sbyte_14 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_15 = left._register.sbyte_15 < right._register.sbyte_15 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    register.uint16_0 = left._register.uint16_0 < right._register.uint16_0 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_1 = left._register.uint16_1 < right._register.uint16_1 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_2 = left._register.uint16_2 < right._register.uint16_2 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_3 = left._register.uint16_3 < right._register.uint16_3 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_4 = left._register.uint16_4 < right._register.uint16_4 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_5 = left._register.uint16_5 < right._register.uint16_5 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_6 = left._register.uint16_6 < right._register.uint16_6 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_7 = left._register.uint16_7 < right._register.uint16_7 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    register.int16_0 = left._register.int16_0 < right._register.int16_0 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_1 = left._register.int16_1 < right._register.int16_1 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_2 = left._register.int16_2 < right._register.int16_2 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_3 = left._register.int16_3 < right._register.int16_3 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_4 = left._register.int16_4 < right._register.int16_4 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_5 = left._register.int16_5 < right._register.int16_5 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_6 = left._register.int16_6 < right._register.int16_6 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_7 = left._register.int16_7 < right._register.int16_7 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    register.uint32_0 = left._register.uint32_0 < right._register.uint32_0 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_1 = left._register.uint32_1 < right._register.uint32_1 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_2 = left._register.uint32_2 < right._register.uint32_2 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_3 = left._register.uint32_3 < right._register.uint32_3 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    register.int32_0 = left._register.int32_0 < right._register.int32_0 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_1 = left._register.int32_1 < right._register.int32_1 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_2 = left._register.int32_2 < right._register.int32_2 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_3 = left._register.int32_3 < right._register.int32_3 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    register.uint64_0 = left._register.uint64_0 < right._register.uint64_0 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    register.uint64_1 = left._register.uint64_1 < right._register.uint64_1 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    register.int64_0 = left._register.int64_0 < right._register.int64_0 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    register.int64_1 = left._register.int64_1 < right._register.int64_1 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Single))
                {
                    register.single_0 = left._register.single_0 < right._register.single_0 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_1 = left._register.single_1 < right._register.single_1 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_2 = left._register.single_2 < right._register.single_2 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_3 = left._register.single_3 < right._register.single_3 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Double))
                {
                    register.double_0 = left._register.double_0 < right._register.double_0 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    register.double_1 = left._register.double_1 < right._register.double_1 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    return new Vector<T>(ref register);
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                }
            }
            else
            {
                Register register = new Register();
                if (typeof(T) == typeof(Byte))
                {
                    register.byte_0 = left._register.byte_0 > right._register.byte_0 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_1 = left._register.byte_1 > right._register.byte_1 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_2 = left._register.byte_2 > right._register.byte_2 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_3 = left._register.byte_3 > right._register.byte_3 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_4 = left._register.byte_4 > right._register.byte_4 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_5 = left._register.byte_5 > right._register.byte_5 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_6 = left._register.byte_6 > right._register.byte_6 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_7 = left._register.byte_7 > right._register.byte_7 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_8 = left._register.byte_8 > right._register.byte_8 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_9 = left._register.byte_9 > right._register.byte_9 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_10 = left._register.byte_10 > right._register.byte_10 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_11 = left._register.byte_11 > right._register.byte_11 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_12 = left._register.byte_12 > right._register.byte_12 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_13 = left._register.byte_13 > right._register.byte_13 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_14 = left._register.byte_14 > right._register.byte_14 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    register.byte_15 = left._register.byte_15 > right._register.byte_15 ? ConstantHelper.GetByteWithAllBitsSet() : (Byte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    register.sbyte_0 = left._register.sbyte_0 > right._register.sbyte_0 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_1 = left._register.sbyte_1 > right._register.sbyte_1 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_2 = left._register.sbyte_2 > right._register.sbyte_2 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_3 = left._register.sbyte_3 > right._register.sbyte_3 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_4 = left._register.sbyte_4 > right._register.sbyte_4 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_5 = left._register.sbyte_5 > right._register.sbyte_5 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_6 = left._register.sbyte_6 > right._register.sbyte_6 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_7 = left._register.sbyte_7 > right._register.sbyte_7 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_8 = left._register.sbyte_8 > right._register.sbyte_8 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_9 = left._register.sbyte_9 > right._register.sbyte_9 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_10 = left._register.sbyte_10 > right._register.sbyte_10 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_11 = left._register.sbyte_11 > right._register.sbyte_11 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_12 = left._register.sbyte_12 > right._register.sbyte_12 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_13 = left._register.sbyte_13 > right._register.sbyte_13 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_14 = left._register.sbyte_14 > right._register.sbyte_14 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    register.sbyte_15 = left._register.sbyte_15 > right._register.sbyte_15 ? ConstantHelper.GetSByteWithAllBitsSet() : (SByte)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    register.uint16_0 = left._register.uint16_0 > right._register.uint16_0 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_1 = left._register.uint16_1 > right._register.uint16_1 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_2 = left._register.uint16_2 > right._register.uint16_2 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_3 = left._register.uint16_3 > right._register.uint16_3 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_4 = left._register.uint16_4 > right._register.uint16_4 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_5 = left._register.uint16_5 > right._register.uint16_5 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_6 = left._register.uint16_6 > right._register.uint16_6 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    register.uint16_7 = left._register.uint16_7 > right._register.uint16_7 ? ConstantHelper.GetUInt16WithAllBitsSet() : (UInt16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    register.int16_0 = left._register.int16_0 > right._register.int16_0 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_1 = left._register.int16_1 > right._register.int16_1 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_2 = left._register.int16_2 > right._register.int16_2 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_3 = left._register.int16_3 > right._register.int16_3 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_4 = left._register.int16_4 > right._register.int16_4 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_5 = left._register.int16_5 > right._register.int16_5 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_6 = left._register.int16_6 > right._register.int16_6 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    register.int16_7 = left._register.int16_7 > right._register.int16_7 ? ConstantHelper.GetInt16WithAllBitsSet() : (Int16)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    register.uint32_0 = left._register.uint32_0 > right._register.uint32_0 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_1 = left._register.uint32_1 > right._register.uint32_1 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_2 = left._register.uint32_2 > right._register.uint32_2 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    register.uint32_3 = left._register.uint32_3 > right._register.uint32_3 ? ConstantHelper.GetUInt32WithAllBitsSet() : (UInt32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    register.int32_0 = left._register.int32_0 > right._register.int32_0 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_1 = left._register.int32_1 > right._register.int32_1 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_2 = left._register.int32_2 > right._register.int32_2 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    register.int32_3 = left._register.int32_3 > right._register.int32_3 ? ConstantHelper.GetInt32WithAllBitsSet() : (Int32)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    register.uint64_0 = left._register.uint64_0 > right._register.uint64_0 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    register.uint64_1 = left._register.uint64_1 > right._register.uint64_1 ? ConstantHelper.GetUInt64WithAllBitsSet() : (UInt64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    register.int64_0 = left._register.int64_0 > right._register.int64_0 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    register.int64_1 = left._register.int64_1 > right._register.int64_1 ? ConstantHelper.GetInt64WithAllBitsSet() : (Int64)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Single))
                {
                    register.single_0 = left._register.single_0 > right._register.single_0 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_1 = left._register.single_1 > right._register.single_1 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_2 = left._register.single_2 > right._register.single_2 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    register.single_3 = left._register.single_3 > right._register.single_3 ? ConstantHelper.GetSingleWithAllBitsSet() : (Single)0;
                    return new Vector<T>(ref register);
                }
                else if (typeof(T) == typeof(Double))
                {
                    register.double_0 = left._register.double_0 > right._register.double_0 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    register.double_1 = left._register.double_1 > right._register.double_1 ? ConstantHelper.GetDoubleWithAllBitsSet() : (Double)0;
                    return new Vector<T>(ref register);
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                }
            }
            else
            {
                if (typeof(T) == typeof(SByte))
                {
                    value._register.sbyte_0 = (SByte)(Math.Abs(value._register.sbyte_0));
                    value._register.sbyte_1 = (SByte)(Math.Abs(value._register.sbyte_1));
                    value._register.sbyte_2 = (SByte)(Math.Abs(value._register.sbyte_2));
                    value._register.sbyte_3 = (SByte)(Math.Abs(value._register.sbyte_3));
                    value._register.sbyte_4 = (SByte)(Math.Abs(value._register.sbyte_4));
                    value._register.sbyte_5 = (SByte)(Math.Abs(value._register.sbyte_5));
                    value._register.sbyte_6 = (SByte)(Math.Abs(value._register.sbyte_6));
                    value._register.sbyte_7 = (SByte)(Math.Abs(value._register.sbyte_7));
                    value._register.sbyte_8 = (SByte)(Math.Abs(value._register.sbyte_8));
                    value._register.sbyte_9 = (SByte)(Math.Abs(value._register.sbyte_9));
                    value._register.sbyte_10 = (SByte)(Math.Abs(value._register.sbyte_10));
                    value._register.sbyte_11 = (SByte)(Math.Abs(value._register.sbyte_11));
                    value._register.sbyte_12 = (SByte)(Math.Abs(value._register.sbyte_12));
                    value._register.sbyte_13 = (SByte)(Math.Abs(value._register.sbyte_13));
                    value._register.sbyte_14 = (SByte)(Math.Abs(value._register.sbyte_14));
                    value._register.sbyte_15 = (SByte)(Math.Abs(value._register.sbyte_15));
                    return value;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    value._register.int16_0 = (Int16)(Math.Abs(value._register.int16_0));
                    value._register.int16_1 = (Int16)(Math.Abs(value._register.int16_1));
                    value._register.int16_2 = (Int16)(Math.Abs(value._register.int16_2));
                    value._register.int16_3 = (Int16)(Math.Abs(value._register.int16_3));
                    value._register.int16_4 = (Int16)(Math.Abs(value._register.int16_4));
                    value._register.int16_5 = (Int16)(Math.Abs(value._register.int16_5));
                    value._register.int16_6 = (Int16)(Math.Abs(value._register.int16_6));
                    value._register.int16_7 = (Int16)(Math.Abs(value._register.int16_7));
                    return value;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    value._register.int32_0 = (Int32)(Math.Abs(value._register.int32_0));
                    value._register.int32_1 = (Int32)(Math.Abs(value._register.int32_1));
                    value._register.int32_2 = (Int32)(Math.Abs(value._register.int32_2));
                    value._register.int32_3 = (Int32)(Math.Abs(value._register.int32_3));
                    return value;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    value._register.int64_0 = (Int64)(Math.Abs(value._register.int64_0));
                    value._register.int64_1 = (Int64)(Math.Abs(value._register.int64_1));
                    return value;
                }
                else if (typeof(T) == typeof(Single))
                {
                    value._register.single_0 = (Single)(Math.Abs(value._register.single_0));
                    value._register.single_1 = (Single)(Math.Abs(value._register.single_1));
                    value._register.single_2 = (Single)(Math.Abs(value._register.single_2));
                    value._register.single_3 = (Single)(Math.Abs(value._register.single_3));
                    return value;
                }
                else if (typeof(T) == typeof(Double))
                {
                    value._register.double_0 = (Double)(Math.Abs(value._register.double_0));
                    value._register.double_1 = (Double)(Math.Abs(value._register.double_1));
                    return value;
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                }
            }
            else
            {
                Vector<T> vec = new Vector<T>();
                if (typeof(T) == typeof(Byte))
                {
                    vec._register.byte_0 = left._register.byte_0 < right._register.byte_0 ? left._register.byte_0 : right._register.byte_0;
                    vec._register.byte_1 = left._register.byte_1 < right._register.byte_1 ? left._register.byte_1 : right._register.byte_1;
                    vec._register.byte_2 = left._register.byte_2 < right._register.byte_2 ? left._register.byte_2 : right._register.byte_2;
                    vec._register.byte_3 = left._register.byte_3 < right._register.byte_3 ? left._register.byte_3 : right._register.byte_3;
                    vec._register.byte_4 = left._register.byte_4 < right._register.byte_4 ? left._register.byte_4 : right._register.byte_4;
                    vec._register.byte_5 = left._register.byte_5 < right._register.byte_5 ? left._register.byte_5 : right._register.byte_5;
                    vec._register.byte_6 = left._register.byte_6 < right._register.byte_6 ? left._register.byte_6 : right._register.byte_6;
                    vec._register.byte_7 = left._register.byte_7 < right._register.byte_7 ? left._register.byte_7 : right._register.byte_7;
                    vec._register.byte_8 = left._register.byte_8 < right._register.byte_8 ? left._register.byte_8 : right._register.byte_8;
                    vec._register.byte_9 = left._register.byte_9 < right._register.byte_9 ? left._register.byte_9 : right._register.byte_9;
                    vec._register.byte_10 = left._register.byte_10 < right._register.byte_10 ? left._register.byte_10 : right._register.byte_10;
                    vec._register.byte_11 = left._register.byte_11 < right._register.byte_11 ? left._register.byte_11 : right._register.byte_11;
                    vec._register.byte_12 = left._register.byte_12 < right._register.byte_12 ? left._register.byte_12 : right._register.byte_12;
                    vec._register.byte_13 = left._register.byte_13 < right._register.byte_13 ? left._register.byte_13 : right._register.byte_13;
                    vec._register.byte_14 = left._register.byte_14 < right._register.byte_14 ? left._register.byte_14 : right._register.byte_14;
                    vec._register.byte_15 = left._register.byte_15 < right._register.byte_15 ? left._register.byte_15 : right._register.byte_15;
                    return vec;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    vec._register.sbyte_0 = left._register.sbyte_0 < right._register.sbyte_0 ? left._register.sbyte_0 : right._register.sbyte_0;
                    vec._register.sbyte_1 = left._register.sbyte_1 < right._register.sbyte_1 ? left._register.sbyte_1 : right._register.sbyte_1;
                    vec._register.sbyte_2 = left._register.sbyte_2 < right._register.sbyte_2 ? left._register.sbyte_2 : right._register.sbyte_2;
                    vec._register.sbyte_3 = left._register.sbyte_3 < right._register.sbyte_3 ? left._register.sbyte_3 : right._register.sbyte_3;
                    vec._register.sbyte_4 = left._register.sbyte_4 < right._register.sbyte_4 ? left._register.sbyte_4 : right._register.sbyte_4;
                    vec._register.sbyte_5 = left._register.sbyte_5 < right._register.sbyte_5 ? left._register.sbyte_5 : right._register.sbyte_5;
                    vec._register.sbyte_6 = left._register.sbyte_6 < right._register.sbyte_6 ? left._register.sbyte_6 : right._register.sbyte_6;
                    vec._register.sbyte_7 = left._register.sbyte_7 < right._register.sbyte_7 ? left._register.sbyte_7 : right._register.sbyte_7;
                    vec._register.sbyte_8 = left._register.sbyte_8 < right._register.sbyte_8 ? left._register.sbyte_8 : right._register.sbyte_8;
                    vec._register.sbyte_9 = left._register.sbyte_9 < right._register.sbyte_9 ? left._register.sbyte_9 : right._register.sbyte_9;
                    vec._register.sbyte_10 = left._register.sbyte_10 < right._register.sbyte_10 ? left._register.sbyte_10 : right._register.sbyte_10;
                    vec._register.sbyte_11 = left._register.sbyte_11 < right._register.sbyte_11 ? left._register.sbyte_11 : right._register.sbyte_11;
                    vec._register.sbyte_12 = left._register.sbyte_12 < right._register.sbyte_12 ? left._register.sbyte_12 : right._register.sbyte_12;
                    vec._register.sbyte_13 = left._register.sbyte_13 < right._register.sbyte_13 ? left._register.sbyte_13 : right._register.sbyte_13;
                    vec._register.sbyte_14 = left._register.sbyte_14 < right._register.sbyte_14 ? left._register.sbyte_14 : right._register.sbyte_14;
                    vec._register.sbyte_15 = left._register.sbyte_15 < right._register.sbyte_15 ? left._register.sbyte_15 : right._register.sbyte_15;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    vec._register.uint16_0 = left._register.uint16_0 < right._register.uint16_0 ? left._register.uint16_0 : right._register.uint16_0;
                    vec._register.uint16_1 = left._register.uint16_1 < right._register.uint16_1 ? left._register.uint16_1 : right._register.uint16_1;
                    vec._register.uint16_2 = left._register.uint16_2 < right._register.uint16_2 ? left._register.uint16_2 : right._register.uint16_2;
                    vec._register.uint16_3 = left._register.uint16_3 < right._register.uint16_3 ? left._register.uint16_3 : right._register.uint16_3;
                    vec._register.uint16_4 = left._register.uint16_4 < right._register.uint16_4 ? left._register.uint16_4 : right._register.uint16_4;
                    vec._register.uint16_5 = left._register.uint16_5 < right._register.uint16_5 ? left._register.uint16_5 : right._register.uint16_5;
                    vec._register.uint16_6 = left._register.uint16_6 < right._register.uint16_6 ? left._register.uint16_6 : right._register.uint16_6;
                    vec._register.uint16_7 = left._register.uint16_7 < right._register.uint16_7 ? left._register.uint16_7 : right._register.uint16_7;
                    return vec;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    vec._register.int16_0 = left._register.int16_0 < right._register.int16_0 ? left._register.int16_0 : right._register.int16_0;
                    vec._register.int16_1 = left._register.int16_1 < right._register.int16_1 ? left._register.int16_1 : right._register.int16_1;
                    vec._register.int16_2 = left._register.int16_2 < right._register.int16_2 ? left._register.int16_2 : right._register.int16_2;
                    vec._register.int16_3 = left._register.int16_3 < right._register.int16_3 ? left._register.int16_3 : right._register.int16_3;
                    vec._register.int16_4 = left._register.int16_4 < right._register.int16_4 ? left._register.int16_4 : right._register.int16_4;
                    vec._register.int16_5 = left._register.int16_5 < right._register.int16_5 ? left._register.int16_5 : right._register.int16_5;
                    vec._register.int16_6 = left._register.int16_6 < right._register.int16_6 ? left._register.int16_6 : right._register.int16_6;
                    vec._register.int16_7 = left._register.int16_7 < right._register.int16_7 ? left._register.int16_7 : right._register.int16_7;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    vec._register.uint32_0 = left._register.uint32_0 < right._register.uint32_0 ? left._register.uint32_0 : right._register.uint32_0;
                    vec._register.uint32_1 = left._register.uint32_1 < right._register.uint32_1 ? left._register.uint32_1 : right._register.uint32_1;
                    vec._register.uint32_2 = left._register.uint32_2 < right._register.uint32_2 ? left._register.uint32_2 : right._register.uint32_2;
                    vec._register.uint32_3 = left._register.uint32_3 < right._register.uint32_3 ? left._register.uint32_3 : right._register.uint32_3;
                    return vec;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    vec._register.int32_0 = left._register.int32_0 < right._register.int32_0 ? left._register.int32_0 : right._register.int32_0;
                    vec._register.int32_1 = left._register.int32_1 < right._register.int32_1 ? left._register.int32_1 : right._register.int32_1;
                    vec._register.int32_2 = left._register.int32_2 < right._register.int32_2 ? left._register.int32_2 : right._register.int32_2;
                    vec._register.int32_3 = left._register.int32_3 < right._register.int32_3 ? left._register.int32_3 : right._register.int32_3;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    vec._register.uint64_0 = left._register.uint64_0 < right._register.uint64_0 ? left._register.uint64_0 : right._register.uint64_0;
                    vec._register.uint64_1 = left._register.uint64_1 < right._register.uint64_1 ? left._register.uint64_1 : right._register.uint64_1;
                    return vec;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    vec._register.int64_0 = left._register.int64_0 < right._register.int64_0 ? left._register.int64_0 : right._register.int64_0;
                    vec._register.int64_1 = left._register.int64_1 < right._register.int64_1 ? left._register.int64_1 : right._register.int64_1;
                    return vec;
                }
                else if (typeof(T) == typeof(Single))
                {
                    vec._register.single_0 = left._register.single_0 < right._register.single_0 ? left._register.single_0 : right._register.single_0;
                    vec._register.single_1 = left._register.single_1 < right._register.single_1 ? left._register.single_1 : right._register.single_1;
                    vec._register.single_2 = left._register.single_2 < right._register.single_2 ? left._register.single_2 : right._register.single_2;
                    vec._register.single_3 = left._register.single_3 < right._register.single_3 ? left._register.single_3 : right._register.single_3;
                    return vec;
                }
                else if (typeof(T) == typeof(Double))
                {
                    vec._register.double_0 = left._register.double_0 < right._register.double_0 ? left._register.double_0 : right._register.double_0;
                    vec._register.double_1 = left._register.double_1 < right._register.double_1 ? left._register.double_1 : right._register.double_1;
                    return vec;
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                }
            }
            else
            {
                Vector<T> vec = new Vector<T>();
                if (typeof(T) == typeof(Byte))
                {
                    vec._register.byte_0 = left._register.byte_0 > right._register.byte_0 ? left._register.byte_0 : right._register.byte_0;
                    vec._register.byte_1 = left._register.byte_1 > right._register.byte_1 ? left._register.byte_1 : right._register.byte_1;
                    vec._register.byte_2 = left._register.byte_2 > right._register.byte_2 ? left._register.byte_2 : right._register.byte_2;
                    vec._register.byte_3 = left._register.byte_3 > right._register.byte_3 ? left._register.byte_3 : right._register.byte_3;
                    vec._register.byte_4 = left._register.byte_4 > right._register.byte_4 ? left._register.byte_4 : right._register.byte_4;
                    vec._register.byte_5 = left._register.byte_5 > right._register.byte_5 ? left._register.byte_5 : right._register.byte_5;
                    vec._register.byte_6 = left._register.byte_6 > right._register.byte_6 ? left._register.byte_6 : right._register.byte_6;
                    vec._register.byte_7 = left._register.byte_7 > right._register.byte_7 ? left._register.byte_7 : right._register.byte_7;
                    vec._register.byte_8 = left._register.byte_8 > right._register.byte_8 ? left._register.byte_8 : right._register.byte_8;
                    vec._register.byte_9 = left._register.byte_9 > right._register.byte_9 ? left._register.byte_9 : right._register.byte_9;
                    vec._register.byte_10 = left._register.byte_10 > right._register.byte_10 ? left._register.byte_10 : right._register.byte_10;
                    vec._register.byte_11 = left._register.byte_11 > right._register.byte_11 ? left._register.byte_11 : right._register.byte_11;
                    vec._register.byte_12 = left._register.byte_12 > right._register.byte_12 ? left._register.byte_12 : right._register.byte_12;
                    vec._register.byte_13 = left._register.byte_13 > right._register.byte_13 ? left._register.byte_13 : right._register.byte_13;
                    vec._register.byte_14 = left._register.byte_14 > right._register.byte_14 ? left._register.byte_14 : right._register.byte_14;
                    vec._register.byte_15 = left._register.byte_15 > right._register.byte_15 ? left._register.byte_15 : right._register.byte_15;
                    return vec;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    vec._register.sbyte_0 = left._register.sbyte_0 > right._register.sbyte_0 ? left._register.sbyte_0 : right._register.sbyte_0;
                    vec._register.sbyte_1 = left._register.sbyte_1 > right._register.sbyte_1 ? left._register.sbyte_1 : right._register.sbyte_1;
                    vec._register.sbyte_2 = left._register.sbyte_2 > right._register.sbyte_2 ? left._register.sbyte_2 : right._register.sbyte_2;
                    vec._register.sbyte_3 = left._register.sbyte_3 > right._register.sbyte_3 ? left._register.sbyte_3 : right._register.sbyte_3;
                    vec._register.sbyte_4 = left._register.sbyte_4 > right._register.sbyte_4 ? left._register.sbyte_4 : right._register.sbyte_4;
                    vec._register.sbyte_5 = left._register.sbyte_5 > right._register.sbyte_5 ? left._register.sbyte_5 : right._register.sbyte_5;
                    vec._register.sbyte_6 = left._register.sbyte_6 > right._register.sbyte_6 ? left._register.sbyte_6 : right._register.sbyte_6;
                    vec._register.sbyte_7 = left._register.sbyte_7 > right._register.sbyte_7 ? left._register.sbyte_7 : right._register.sbyte_7;
                    vec._register.sbyte_8 = left._register.sbyte_8 > right._register.sbyte_8 ? left._register.sbyte_8 : right._register.sbyte_8;
                    vec._register.sbyte_9 = left._register.sbyte_9 > right._register.sbyte_9 ? left._register.sbyte_9 : right._register.sbyte_9;
                    vec._register.sbyte_10 = left._register.sbyte_10 > right._register.sbyte_10 ? left._register.sbyte_10 : right._register.sbyte_10;
                    vec._register.sbyte_11 = left._register.sbyte_11 > right._register.sbyte_11 ? left._register.sbyte_11 : right._register.sbyte_11;
                    vec._register.sbyte_12 = left._register.sbyte_12 > right._register.sbyte_12 ? left._register.sbyte_12 : right._register.sbyte_12;
                    vec._register.sbyte_13 = left._register.sbyte_13 > right._register.sbyte_13 ? left._register.sbyte_13 : right._register.sbyte_13;
                    vec._register.sbyte_14 = left._register.sbyte_14 > right._register.sbyte_14 ? left._register.sbyte_14 : right._register.sbyte_14;
                    vec._register.sbyte_15 = left._register.sbyte_15 > right._register.sbyte_15 ? left._register.sbyte_15 : right._register.sbyte_15;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    vec._register.uint16_0 = left._register.uint16_0 > right._register.uint16_0 ? left._register.uint16_0 : right._register.uint16_0;
                    vec._register.uint16_1 = left._register.uint16_1 > right._register.uint16_1 ? left._register.uint16_1 : right._register.uint16_1;
                    vec._register.uint16_2 = left._register.uint16_2 > right._register.uint16_2 ? left._register.uint16_2 : right._register.uint16_2;
                    vec._register.uint16_3 = left._register.uint16_3 > right._register.uint16_3 ? left._register.uint16_3 : right._register.uint16_3;
                    vec._register.uint16_4 = left._register.uint16_4 > right._register.uint16_4 ? left._register.uint16_4 : right._register.uint16_4;
                    vec._register.uint16_5 = left._register.uint16_5 > right._register.uint16_5 ? left._register.uint16_5 : right._register.uint16_5;
                    vec._register.uint16_6 = left._register.uint16_6 > right._register.uint16_6 ? left._register.uint16_6 : right._register.uint16_6;
                    vec._register.uint16_7 = left._register.uint16_7 > right._register.uint16_7 ? left._register.uint16_7 : right._register.uint16_7;
                    return vec;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    vec._register.int16_0 = left._register.int16_0 > right._register.int16_0 ? left._register.int16_0 : right._register.int16_0;
                    vec._register.int16_1 = left._register.int16_1 > right._register.int16_1 ? left._register.int16_1 : right._register.int16_1;
                    vec._register.int16_2 = left._register.int16_2 > right._register.int16_2 ? left._register.int16_2 : right._register.int16_2;
                    vec._register.int16_3 = left._register.int16_3 > right._register.int16_3 ? left._register.int16_3 : right._register.int16_3;
                    vec._register.int16_4 = left._register.int16_4 > right._register.int16_4 ? left._register.int16_4 : right._register.int16_4;
                    vec._register.int16_5 = left._register.int16_5 > right._register.int16_5 ? left._register.int16_5 : right._register.int16_5;
                    vec._register.int16_6 = left._register.int16_6 > right._register.int16_6 ? left._register.int16_6 : right._register.int16_6;
                    vec._register.int16_7 = left._register.int16_7 > right._register.int16_7 ? left._register.int16_7 : right._register.int16_7;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    vec._register.uint32_0 = left._register.uint32_0 > right._register.uint32_0 ? left._register.uint32_0 : right._register.uint32_0;
                    vec._register.uint32_1 = left._register.uint32_1 > right._register.uint32_1 ? left._register.uint32_1 : right._register.uint32_1;
                    vec._register.uint32_2 = left._register.uint32_2 > right._register.uint32_2 ? left._register.uint32_2 : right._register.uint32_2;
                    vec._register.uint32_3 = left._register.uint32_3 > right._register.uint32_3 ? left._register.uint32_3 : right._register.uint32_3;
                    return vec;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    vec._register.int32_0 = left._register.int32_0 > right._register.int32_0 ? left._register.int32_0 : right._register.int32_0;
                    vec._register.int32_1 = left._register.int32_1 > right._register.int32_1 ? left._register.int32_1 : right._register.int32_1;
                    vec._register.int32_2 = left._register.int32_2 > right._register.int32_2 ? left._register.int32_2 : right._register.int32_2;
                    vec._register.int32_3 = left._register.int32_3 > right._register.int32_3 ? left._register.int32_3 : right._register.int32_3;
                    return vec;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    vec._register.uint64_0 = left._register.uint64_0 > right._register.uint64_0 ? left._register.uint64_0 : right._register.uint64_0;
                    vec._register.uint64_1 = left._register.uint64_1 > right._register.uint64_1 ? left._register.uint64_1 : right._register.uint64_1;
                    return vec;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    vec._register.int64_0 = left._register.int64_0 > right._register.int64_0 ? left._register.int64_0 : right._register.int64_0;
                    vec._register.int64_1 = left._register.int64_1 > right._register.int64_1 ? left._register.int64_1 : right._register.int64_1;
                    return vec;
                }
                else if (typeof(T) == typeof(Single))
                {
                    vec._register.single_0 = left._register.single_0 > right._register.single_0 ? left._register.single_0 : right._register.single_0;
                    vec._register.single_1 = left._register.single_1 > right._register.single_1 ? left._register.single_1 : right._register.single_1;
                    vec._register.single_2 = left._register.single_2 > right._register.single_2 ? left._register.single_2 : right._register.single_2;
                    vec._register.single_3 = left._register.single_3 > right._register.single_3 ? left._register.single_3 : right._register.single_3;
                    return vec;
                }
                else if (typeof(T) == typeof(Double))
                {
                    vec._register.double_0 = left._register.double_0 > right._register.double_0 ? left._register.double_0 : right._register.double_0;
                    vec._register.double_1 = left._register.double_1 > right._register.double_1 ? left._register.double_1 : right._register.double_1;
                    return vec;
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                    product += (Byte)(left._register.byte_0 * right._register.byte_0);
                    product += (Byte)(left._register.byte_1 * right._register.byte_1);
                    product += (Byte)(left._register.byte_2 * right._register.byte_2);
                    product += (Byte)(left._register.byte_3 * right._register.byte_3);
                    product += (Byte)(left._register.byte_4 * right._register.byte_4);
                    product += (Byte)(left._register.byte_5 * right._register.byte_5);
                    product += (Byte)(left._register.byte_6 * right._register.byte_6);
                    product += (Byte)(left._register.byte_7 * right._register.byte_7);
                    product += (Byte)(left._register.byte_8 * right._register.byte_8);
                    product += (Byte)(left._register.byte_9 * right._register.byte_9);
                    product += (Byte)(left._register.byte_10 * right._register.byte_10);
                    product += (Byte)(left._register.byte_11 * right._register.byte_11);
                    product += (Byte)(left._register.byte_12 * right._register.byte_12);
                    product += (Byte)(left._register.byte_13 * right._register.byte_13);
                    product += (Byte)(left._register.byte_14 * right._register.byte_14);
                    product += (Byte)(left._register.byte_15 * right._register.byte_15);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte product = 0;
                    product += (SByte)(left._register.sbyte_0 * right._register.sbyte_0);
                    product += (SByte)(left._register.sbyte_1 * right._register.sbyte_1);
                    product += (SByte)(left._register.sbyte_2 * right._register.sbyte_2);
                    product += (SByte)(left._register.sbyte_3 * right._register.sbyte_3);
                    product += (SByte)(left._register.sbyte_4 * right._register.sbyte_4);
                    product += (SByte)(left._register.sbyte_5 * right._register.sbyte_5);
                    product += (SByte)(left._register.sbyte_6 * right._register.sbyte_6);
                    product += (SByte)(left._register.sbyte_7 * right._register.sbyte_7);
                    product += (SByte)(left._register.sbyte_8 * right._register.sbyte_8);
                    product += (SByte)(left._register.sbyte_9 * right._register.sbyte_9);
                    product += (SByte)(left._register.sbyte_10 * right._register.sbyte_10);
                    product += (SByte)(left._register.sbyte_11 * right._register.sbyte_11);
                    product += (SByte)(left._register.sbyte_12 * right._register.sbyte_12);
                    product += (SByte)(left._register.sbyte_13 * right._register.sbyte_13);
                    product += (SByte)(left._register.sbyte_14 * right._register.sbyte_14);
                    product += (SByte)(left._register.sbyte_15 * right._register.sbyte_15);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16 product = 0;
                    product += (UInt16)(left._register.uint16_0 * right._register.uint16_0);
                    product += (UInt16)(left._register.uint16_1 * right._register.uint16_1);
                    product += (UInt16)(left._register.uint16_2 * right._register.uint16_2);
                    product += (UInt16)(left._register.uint16_3 * right._register.uint16_3);
                    product += (UInt16)(left._register.uint16_4 * right._register.uint16_4);
                    product += (UInt16)(left._register.uint16_5 * right._register.uint16_5);
                    product += (UInt16)(left._register.uint16_6 * right._register.uint16_6);
                    product += (UInt16)(left._register.uint16_7 * right._register.uint16_7);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16 product = 0;
                    product += (Int16)(left._register.int16_0 * right._register.int16_0);
                    product += (Int16)(left._register.int16_1 * right._register.int16_1);
                    product += (Int16)(left._register.int16_2 * right._register.int16_2);
                    product += (Int16)(left._register.int16_3 * right._register.int16_3);
                    product += (Int16)(left._register.int16_4 * right._register.int16_4);
                    product += (Int16)(left._register.int16_5 * right._register.int16_5);
                    product += (Int16)(left._register.int16_6 * right._register.int16_6);
                    product += (Int16)(left._register.int16_7 * right._register.int16_7);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32 product = 0;
                    product += (UInt32)(left._register.uint32_0 * right._register.uint32_0);
                    product += (UInt32)(left._register.uint32_1 * right._register.uint32_1);
                    product += (UInt32)(left._register.uint32_2 * right._register.uint32_2);
                    product += (UInt32)(left._register.uint32_3 * right._register.uint32_3);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32 product = 0;
                    product += (Int32)(left._register.int32_0 * right._register.int32_0);
                    product += (Int32)(left._register.int32_1 * right._register.int32_1);
                    product += (Int32)(left._register.int32_2 * right._register.int32_2);
                    product += (Int32)(left._register.int32_3 * right._register.int32_3);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64 product = 0;
                    product += (UInt64)(left._register.uint64_0 * right._register.uint64_0);
                    product += (UInt64)(left._register.uint64_1 * right._register.uint64_1);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64 product = 0;
                    product += (Int64)(left._register.int64_0 * right._register.int64_0);
                    product += (Int64)(left._register.int64_1 * right._register.int64_1);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single product = 0;
                    product += (Single)(left._register.single_0 * right._register.single_0);
                    product += (Single)(left._register.single_1 * right._register.single_1);
                    product += (Single)(left._register.single_2 * right._register.single_2);
                    product += (Single)(left._register.single_3 * right._register.single_3);
                    return (T)(object)product;
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double product = 0;
                    product += (Double)(left._register.double_0 * right._register.double_0);
                    product += (Double)(left._register.double_1 * right._register.double_1);
                    return (T)(object)product;
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                        dataPtr[g] = (Byte)Math.Sqrt((Byte)(object)value[g]);
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(SByte))
                {
                    SByte* dataPtr = stackalloc SByte[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (SByte)Math.Sqrt((SByte)(object)value[g]);
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    UInt16* dataPtr = stackalloc UInt16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (UInt16)Math.Sqrt((UInt16)(object)value[g]);
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int16))
                {
                    Int16* dataPtr = stackalloc Int16[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (Int16)Math.Sqrt((Int16)(object)value[g]);
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    UInt32* dataPtr = stackalloc UInt32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (UInt32)Math.Sqrt((UInt32)(object)value[g]);
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int32))
                {
                    Int32* dataPtr = stackalloc Int32[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (Int32)Math.Sqrt((Int32)(object)value[g]);
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    UInt64* dataPtr = stackalloc UInt64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (UInt64)Math.Sqrt((UInt64)(object)value[g]);
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Int64))
                {
                    Int64* dataPtr = stackalloc Int64[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (Int64)Math.Sqrt((Int64)(object)value[g]);
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Single))
                {
                    Single* dataPtr = stackalloc Single[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (Single)Math.Sqrt((Single)(object)value[g]);
                    }
                    return new Vector<T>(dataPtr);
                }
                else if (typeof(T) == typeof(Double))
                {
                    Double* dataPtr = stackalloc Double[Count];
                    for (int g = 0; g < Count; g++)
                    {
                        dataPtr[g] = (Double)Math.Sqrt((Double)(object)value[g]);
                    }
                    return new Vector<T>(dataPtr);
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
                }
            }
            else
            {
                if (typeof(T) == typeof(Byte))
                {
                    value._register.byte_0 = (Byte)Math.Sqrt(value._register.byte_0);
                    value._register.byte_1 = (Byte)Math.Sqrt(value._register.byte_1);
                    value._register.byte_2 = (Byte)Math.Sqrt(value._register.byte_2);
                    value._register.byte_3 = (Byte)Math.Sqrt(value._register.byte_3);
                    value._register.byte_4 = (Byte)Math.Sqrt(value._register.byte_4);
                    value._register.byte_5 = (Byte)Math.Sqrt(value._register.byte_5);
                    value._register.byte_6 = (Byte)Math.Sqrt(value._register.byte_6);
                    value._register.byte_7 = (Byte)Math.Sqrt(value._register.byte_7);
                    value._register.byte_8 = (Byte)Math.Sqrt(value._register.byte_8);
                    value._register.byte_9 = (Byte)Math.Sqrt(value._register.byte_9);
                    value._register.byte_10 = (Byte)Math.Sqrt(value._register.byte_10);
                    value._register.byte_11 = (Byte)Math.Sqrt(value._register.byte_11);
                    value._register.byte_12 = (Byte)Math.Sqrt(value._register.byte_12);
                    value._register.byte_13 = (Byte)Math.Sqrt(value._register.byte_13);
                    value._register.byte_14 = (Byte)Math.Sqrt(value._register.byte_14);
                    value._register.byte_15 = (Byte)Math.Sqrt(value._register.byte_15);
                    return value;
                }
                else if (typeof(T) == typeof(SByte))
                {
                    value._register.sbyte_0 = (SByte)Math.Sqrt(value._register.sbyte_0);
                    value._register.sbyte_1 = (SByte)Math.Sqrt(value._register.sbyte_1);
                    value._register.sbyte_2 = (SByte)Math.Sqrt(value._register.sbyte_2);
                    value._register.sbyte_3 = (SByte)Math.Sqrt(value._register.sbyte_3);
                    value._register.sbyte_4 = (SByte)Math.Sqrt(value._register.sbyte_4);
                    value._register.sbyte_5 = (SByte)Math.Sqrt(value._register.sbyte_5);
                    value._register.sbyte_6 = (SByte)Math.Sqrt(value._register.sbyte_6);
                    value._register.sbyte_7 = (SByte)Math.Sqrt(value._register.sbyte_7);
                    value._register.sbyte_8 = (SByte)Math.Sqrt(value._register.sbyte_8);
                    value._register.sbyte_9 = (SByte)Math.Sqrt(value._register.sbyte_9);
                    value._register.sbyte_10 = (SByte)Math.Sqrt(value._register.sbyte_10);
                    value._register.sbyte_11 = (SByte)Math.Sqrt(value._register.sbyte_11);
                    value._register.sbyte_12 = (SByte)Math.Sqrt(value._register.sbyte_12);
                    value._register.sbyte_13 = (SByte)Math.Sqrt(value._register.sbyte_13);
                    value._register.sbyte_14 = (SByte)Math.Sqrt(value._register.sbyte_14);
                    value._register.sbyte_15 = (SByte)Math.Sqrt(value._register.sbyte_15);
                    return value;
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    value._register.uint16_0 = (UInt16)Math.Sqrt(value._register.uint16_0);
                    value._register.uint16_1 = (UInt16)Math.Sqrt(value._register.uint16_1);
                    value._register.uint16_2 = (UInt16)Math.Sqrt(value._register.uint16_2);
                    value._register.uint16_3 = (UInt16)Math.Sqrt(value._register.uint16_3);
                    value._register.uint16_4 = (UInt16)Math.Sqrt(value._register.uint16_4);
                    value._register.uint16_5 = (UInt16)Math.Sqrt(value._register.uint16_5);
                    value._register.uint16_6 = (UInt16)Math.Sqrt(value._register.uint16_6);
                    value._register.uint16_7 = (UInt16)Math.Sqrt(value._register.uint16_7);
                    return value;
                }
                else if (typeof(T) == typeof(Int16))
                {
                    value._register.int16_0 = (Int16)Math.Sqrt(value._register.int16_0);
                    value._register.int16_1 = (Int16)Math.Sqrt(value._register.int16_1);
                    value._register.int16_2 = (Int16)Math.Sqrt(value._register.int16_2);
                    value._register.int16_3 = (Int16)Math.Sqrt(value._register.int16_3);
                    value._register.int16_4 = (Int16)Math.Sqrt(value._register.int16_4);
                    value._register.int16_5 = (Int16)Math.Sqrt(value._register.int16_5);
                    value._register.int16_6 = (Int16)Math.Sqrt(value._register.int16_6);
                    value._register.int16_7 = (Int16)Math.Sqrt(value._register.int16_7);
                    return value;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    value._register.uint32_0 = (UInt32)Math.Sqrt(value._register.uint32_0);
                    value._register.uint32_1 = (UInt32)Math.Sqrt(value._register.uint32_1);
                    value._register.uint32_2 = (UInt32)Math.Sqrt(value._register.uint32_2);
                    value._register.uint32_3 = (UInt32)Math.Sqrt(value._register.uint32_3);
                    return value;
                }
                else if (typeof(T) == typeof(Int32))
                {
                    value._register.int32_0 = (Int32)Math.Sqrt(value._register.int32_0);
                    value._register.int32_1 = (Int32)Math.Sqrt(value._register.int32_1);
                    value._register.int32_2 = (Int32)Math.Sqrt(value._register.int32_2);
                    value._register.int32_3 = (Int32)Math.Sqrt(value._register.int32_3);
                    return value;
                }
                else if (typeof(T) == typeof(UInt64))
                {
                    value._register.uint64_0 = (UInt64)Math.Sqrt(value._register.uint64_0);
                    value._register.uint64_1 = (UInt64)Math.Sqrt(value._register.uint64_1);
                    return value;
                }
                else if (typeof(T) == typeof(Int64))
                {
                    value._register.int64_0 = (Int64)Math.Sqrt(value._register.int64_0);
                    value._register.int64_1 = (Int64)Math.Sqrt(value._register.int64_1);
                    return value;
                }
                else if (typeof(T) == typeof(Single))
                {
                    value._register.single_0 = (Single)Math.Sqrt(value._register.single_0);
                    value._register.single_1 = (Single)Math.Sqrt(value._register.single_1);
                    value._register.single_2 = (Single)Math.Sqrt(value._register.single_2);
                    value._register.single_3 = (Single)Math.Sqrt(value._register.single_3);
                    return value;
                }
                else if (typeof(T) == typeof(Double))
                {
                    value._register.double_0 = (Double)Math.Sqrt(value._register.double_0);
                    value._register.double_1 = (Double)Math.Sqrt(value._register.double_1);
                    return value;
                }
                else
                {
                    throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
            }
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static T ScalarAdd(T left, T right)
        {
            if (typeof(T) == typeof(Byte))
            {
                return (T)(object)(Byte)((Byte)(object)left + (Byte)(object)right);
            }
            else if (typeof(T) == typeof(SByte))
            {
                return (T)(object)(SByte)((SByte)(object)left + (SByte)(object)right);
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return (T)(object)(UInt16)((UInt16)(object)left + (UInt16)(object)right);
            }
            else if (typeof(T) == typeof(Int16))
            {
                return (T)(object)(Int16)((Int16)(object)left + (Int16)(object)right);
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return (T)(object)(UInt32)((UInt32)(object)left + (UInt32)(object)right);
            }
            else if (typeof(T) == typeof(Int32))
            {
                return (T)(object)(Int32)((Int32)(object)left + (Int32)(object)right);
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return (T)(object)(UInt64)((UInt64)(object)left + (UInt64)(object)right);
            }
            else if (typeof(T) == typeof(Int64))
            {
                return (T)(object)(Int64)((Int64)(object)left + (Int64)(object)right);
            }
            else if (typeof(T) == typeof(Single))
            {
                return (T)(object)(Single)((Single)(object)left + (Single)(object)right);
            }
            else if (typeof(T) == typeof(Double))
            {
                return (T)(object)(Double)((Double)(object)left + (Double)(object)right);
            }
            else
            {
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
            }
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        private static T ScalarMultiply(T left, T right)
        {
            if (typeof(T) == typeof(Byte))
            {
                return (T)(object)(Byte)((Byte)(object)left * (Byte)(object)right);
            }
            else if (typeof(T) == typeof(SByte))
            {
                return (T)(object)(SByte)((SByte)(object)left * (SByte)(object)right);
            }
            else if (typeof(T) == typeof(UInt16))
            {
                return (T)(object)(UInt16)((UInt16)(object)left * (UInt16)(object)right);
            }
            else if (typeof(T) == typeof(Int16))
            {
                return (T)(object)(Int16)((Int16)(object)left * (Int16)(object)right);
            }
            else if (typeof(T) == typeof(UInt32))
            {
                return (T)(object)(UInt32)((UInt32)(object)left * (UInt32)(object)right);
            }
            else if (typeof(T) == typeof(Int32))
            {
                return (T)(object)(Int32)((Int32)(object)left * (Int32)(object)right);
            }
            else if (typeof(T) == typeof(UInt64))
            {
                return (T)(object)(UInt64)((UInt64)(object)left * (UInt64)(object)right);
            }
            else if (typeof(T) == typeof(Int64))
            {
                return (T)(object)(Int64)((Int64)(object)left * (Int64)(object)right);
            }
            else if (typeof(T) == typeof(Single))
            {
                return (T)(object)(Single)((Single)(object)left * (Single)(object)right);
            }
            else if (typeof(T) == typeof(Double))
            {
                return (T)(object)(Double)((Double)(object)left * (Double)(object)right);
            }
            else
            {
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
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
                throw new NotSupportedException(SR.GetString("Arg_TypeNotSupported", typeof(T)));
            }
        }
        #endregion
    }
}