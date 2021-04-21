// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public partial class Array
    {
        [StructLayout(LayoutKind.Sequential)]
        private sealed class RawData
        {
            public IntPtr Bounds;
            // The following is to prevent a mismatch between the managed and runtime
            // layouts where MONO_BIG_ARRAYS is false on 64-bit big endian systems
#if MONO_BIG_ARRAYS
            public ulong Count;
#else
            public uint Count;
#if !TARGET_32BIT
            private uint _Pad;
#endif
#endif
            public byte Data;
        }

        public int Length
        {
            [Intrinsic]
            get => Length;
        }

        public long LongLength
        {
            get
            {
                long length = GetLength(0);

                for (int i = 1; i < Rank; i++)
                {
                    length *= GetLength(i);
                }
                return length;
            }
        }

        public int Rank
        {
            [Intrinsic]
            get => Rank;
        }

        public static unsafe void Clear(Array array, int index, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            int lowerBound = array.GetLowerBound(0);
            int elementSize = array.GetElementSize();
            nuint numComponents = (nuint)(nint)Unsafe.As<RawData>(array).Count;

            int offset = index - lowerBound;

            if (index < lowerBound || offset < 0 || length < 0 || (uint)(offset + length) > numComponents)
                ThrowHelper.ThrowIndexOutOfRangeException();

            ref byte ptr = ref Unsafe.AddByteOffset(ref array.GetRawSzArrayData(), (uint)offset * (nuint)elementSize);
            nuint byteLength = (uint)length * (nuint)elementSize;

            if (RuntimeHelpers.ObjectHasReferences(array))
                SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref ptr), byteLength / (uint)sizeof(IntPtr));
            else
                SpanHelpers.ClearWithoutReferences(ref ptr, byteLength);
        }

        public static void ConstrainedCopy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length, true);
        }

        public static void Copy(Array sourceArray, Array destinationArray, int length)
        {
            if (sourceArray == null)
                throw new ArgumentNullException(nameof(sourceArray));

            if (destinationArray == null)
                throw new ArgumentNullException(nameof(destinationArray));

            Copy(sourceArray, sourceArray.GetLowerBound(0), destinationArray,
                destinationArray.GetLowerBound(0), length);
        }

        public static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length, false);
        }

        private static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
        {
            if (sourceArray == null)
                throw new ArgumentNullException(nameof(sourceArray));

            if (destinationArray == null)
                throw new ArgumentNullException(nameof(destinationArray));

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Value has to be >= 0.");

            if (sourceArray.Rank != destinationArray.Rank)
                throw new RankException(SR.Rank_MultiDimNotSupported);

            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), "Value has to be >= 0.");

            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), "Value has to be >= 0.");

            if (FastCopy(sourceArray, sourceIndex, destinationArray, destinationIndex, length))
                return;

            int source_pos = sourceIndex - sourceArray.GetLowerBound(0);
            int dest_pos = destinationIndex - destinationArray.GetLowerBound(0);

            if (source_pos < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), "Index was less than the array's lower bound in the first dimension.");

            if (dest_pos < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), "Index was less than the array's lower bound in the first dimension.");

            // re-ordered to avoid possible integer overflow
            if (source_pos > sourceArray.Length - length)
                throw new ArgumentException(SR.Arg_LongerThanSrcArray, nameof(sourceArray));

            if (dest_pos > destinationArray.Length - length)
            {
                throw new ArgumentException("Destination array was not long enough. Check destIndex and length, and the array's lower bounds", nameof(destinationArray));
            }

            Type src_type = sourceArray.GetType().GetElementType()!;
            Type dst_type = destinationArray.GetType().GetElementType()!;
            bool dst_type_vt = dst_type.IsValueType && Nullable.GetUnderlyingType(dst_type) == null;

            bool src_is_enum = src_type.IsEnum;
            bool dst_is_enum = dst_type.IsEnum;

            if (src_is_enum)
                src_type = Enum.GetUnderlyingType(src_type);
            if (dst_is_enum)
                dst_type = Enum.GetUnderlyingType(dst_type);

            if (reliable)
            {
                if (!dst_type.Equals(src_type) &&
                    !(dst_type.IsPrimitive && src_type.IsPrimitive && CanChangePrimitive(ref dst_type, ref src_type, true)))
                {
                    throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_CantAssignType);
                }
            }
            else
            {
                if (!CanAssignArrayElement(src_type, dst_type))
                {
                    throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_CantAssignType);
                }
            }

            if (!ReferenceEquals(sourceArray, destinationArray) || source_pos > dest_pos)
            {
                for (int i = 0; i < length; i++)
                {
                    object srcval = sourceArray.GetValueImpl(source_pos + i);

                    if (!src_type.IsValueType && dst_is_enum)
                        throw new InvalidCastException(SR.InvalidCast_DownCastArrayElement);

                    if (dst_type_vt && (srcval == null || (src_type == typeof(object) && srcval.GetType() != dst_type)))
                        throw new InvalidCastException();

                    try
                    {
                        destinationArray.SetValueRelaxedImpl(srcval, dest_pos + i);
                    }
                    catch (ArgumentException)
                    {
                        throw CreateArrayTypeMismatchException();
                    }
                }
            }
            else
            {
                for (int i = length - 1; i >= 0; i--)
                {
                    object srcval = sourceArray.GetValueImpl(source_pos + i);

                    try
                    {
                        destinationArray.SetValueRelaxedImpl(srcval, dest_pos + i);
                    }
                    catch (ArgumentException)
                    {
                        throw CreateArrayTypeMismatchException();
                    }
                }
            }
        }

        private static ArrayTypeMismatchException CreateArrayTypeMismatchException()
        {
            return new ArrayTypeMismatchException();
        }

        private static bool CanAssignArrayElement(Type source, Type target)
        {
            if (!target.IsValueType && !target.IsPointer)
            {
                if (!source.IsValueType && !source.IsPointer)
                {
                    // Reference to reference copy
                    return
                        source.IsInterface || target.IsInterface ||
                        source.IsAssignableFrom(target) || target.IsAssignableFrom(source);
                }
                else
                {
                    // Value to reference copy
                    if (source.IsPointer)
                        return false;
                    return target.IsAssignableFrom(source);
                }
            }
            else
            {
                if (source.IsEquivalentTo(target))
                {
                    return true;
                }
                else if (source.IsPointer && target.IsPointer)
                {
                    return true;
                }
                else if (source.IsPrimitive && target.IsPrimitive)
                {

                    // Allow primitive type widening
                    return CanChangePrimitive(ref source, ref target, false);
                }
                else if (!source.IsValueType && !source.IsPointer)
                {
                    // Source is base class or interface of destination type
                    if (target.IsPointer)
                        return false;
                    return source.IsAssignableFrom(target);
                }
            }

            return false;
        }

        public static unsafe Array CreateInstance(Type elementType, int length)
        {
            if (elementType is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();

            RuntimeType? runtimeType = elementType.UnderlyingSystemType as RuntimeType;
            if (runtimeType == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            Array? array = null;
            InternalCreate(ref array, runtimeType._impl.Value, 1, &length, null);
            GC.KeepAlive(runtimeType);
            return array;
        }

        public static unsafe Array CreateInstance(Type elementType, int length1, int length2)
        {
            if (elementType is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
            if (length1 < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length1, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (length2 < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length2, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            RuntimeType? runtimeType = elementType.UnderlyingSystemType as RuntimeType;
            if (runtimeType == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            int* lengths = stackalloc int[] { length1, length2 };
            Array? array = null;
            InternalCreate(ref array, runtimeType._impl.Value, 2, lengths, null);
            GC.KeepAlive(runtimeType);
            return array;
        }

        public static unsafe Array CreateInstance(Type elementType, int length1, int length2, int length3)
        {
            if (elementType is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
            if (length1 < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length1, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (length2 < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length2, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (length3 < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length3, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            RuntimeType? runtimeType = elementType.UnderlyingSystemType as RuntimeType;
            if (runtimeType == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            int* lengths = stackalloc int[] { length1, length2, length3 };
            Array? array = null;
            InternalCreate(ref array, runtimeType._impl.Value, 3, lengths, null);
            GC.KeepAlive(runtimeType);
            return array;
        }

        public static unsafe Array CreateInstance(Type elementType, params int[] lengths)
        {
            if (elementType is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
            if (lengths == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lengths);
            if (lengths.Length == 0)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NeedAtLeast1Rank);

            RuntimeType? runtimeType = elementType.UnderlyingSystemType as RuntimeType;
            if (runtimeType == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            for (int i = 0; i < lengths.Length; i++)
                if (lengths[i] < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            Array? array = null;
            fixed (int* pLengths = &lengths[0])
                InternalCreate(ref array, runtimeType._impl.Value, lengths.Length, pLengths, null);
            GC.KeepAlive(runtimeType);
            return array;
        }

        public static unsafe Array CreateInstance(Type elementType, int[] lengths, int[] lowerBounds)
        {
            if (elementType == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
            if (lengths == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lengths);
            if (lowerBounds == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lowerBounds);
            if (lengths.Length != lowerBounds!.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RanksAndBounds);
            if (lengths.Length == 0)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NeedAtLeast1Rank);

            for (int i = 0; i < lengths.Length; i++)
                if (lengths[i] < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            RuntimeType? runtimeType = elementType.UnderlyingSystemType as RuntimeType;
            if (runtimeType == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            Array? array = null;
            fixed (int* pLengths = &lengths[0])
            fixed (int* pLowerBounds = &lowerBounds[0])
                InternalCreate(ref array, runtimeType._impl.Value, lengths.Length, pLengths, pLowerBounds);
            GC.KeepAlive(runtimeType);
            return array;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void InternalCreate([NotNull] ref Array? result, IntPtr elementType, int rank, int* lengths, int* lowerBounds);

        private unsafe nint GetFlattenedIndex(ReadOnlySpan<int> indices)
        {
            // Checked by the caller
            Debug.Assert(indices.Length == Rank);

            nint flattenedIndex = 0;
            for (int i = 0; i < indices.Length; i++)
            {
                int index = indices[i] - GetLowerBound(i);
                int length = GetLength(i);
                if ((uint)index >= (uint)length)
                    ThrowHelper.ThrowIndexOutOfRangeException();
                flattenedIndex = (length * flattenedIndex) + index;
            }
            Debug.Assert((nuint)flattenedIndex < (nuint)LongLength);
            return flattenedIndex;
        }

        internal object? InternalGetValue(nint index)
        {
            if (GetType().GetElementType()!.IsPointer)
                throw new NotSupportedException(SR.NotSupported_Type);

            return GetValueImpl((int)index);
        }

        internal void InternalSetValue(object? value, nint index)
        {
            if (GetType().GetElementType()!.IsPointer)
                throw new NotSupportedException(SR.NotSupported_Type);

            SetValueImpl(value, (int)index);
        }

        public void Initialize()
        {
        }

        private static int IndexOfImpl<T>(T[] array, T value, int startIndex, int count)
        {
            return EqualityComparer<T>.Default.IndexOf(array, value, startIndex, count);
        }

        private static int LastIndexOfImpl<T>(T[] array, T value, int startIndex, int count)
        {
            return EqualityComparer<T>.Default.LastIndexOf(array, value, startIndex, count);
        }

        public int GetUpperBound(int dimension)
        {
            return GetLowerBound(dimension) + GetLength(dimension) - 1;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte GetRawSzArrayData()
        {
            // TODO: Missing intrinsic in interpreter
            return ref Unsafe.As<RawData>(this).Data;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ref byte GetRawArrayData()
        {
            // TODO: Missing intrinsic in interpreter
            return ref Unsafe.As<RawData>(this).Data;
        }

        [Intrinsic]
        internal int GetElementSize() => GetElementSize();

        [Intrinsic]
        internal bool IsPrimitive() => IsPrimitive();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern CorElementType GetCorElementTypeOfElementType();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool IsValueOfElementType(object value);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool CanChangePrimitive(ref Type srcType, ref Type dstType, bool reliable);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool FastCopy(Array source, int source_idx, Array dest, int dest_idx, int length);

        [Intrinsic] // when dimension is `0` constant
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern int GetLength(int dimension);

        [Intrinsic] // when dimension is `0` constant
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern int GetLowerBound(int dimension);

        // CAUTION! No bounds checking!
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetGenericValue_icall<T>(ref Array self, int pos, out T value);

        // CAUTION! No bounds checking!
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern object GetValueImpl(int pos);

        // CAUTION! No bounds checking!
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SetGenericValue_icall<T>(ref Array self, int pos, ref T value);

        [Intrinsic]
        private void GetGenericValueImpl<T>(int pos, out T value)
        {
            Array self = this;
            GetGenericValue_icall(ref self, pos, out value);
        }

        [Intrinsic]
        private void SetGenericValueImpl<T>(int pos, ref T value)
        {
            Array self = this;
            SetGenericValue_icall(ref self, pos, ref value);
        }

        // CAUTION! No bounds checking!
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void SetValueImpl(object? value, int pos);

        // CAUTION! No bounds checking!
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern void SetValueRelaxedImpl(object? value, int pos);

#pragma warning disable CA1822
        /*
         * These methods are used to implement the implicit generic interfaces
         * implemented by arrays in NET 2.0.
         * Only make those methods generic which really need it, to avoid
         * creating useless instantiations.
         */
        internal int InternalArray__ICollection_get_Count()
        {
            return Length;
        }

        internal bool InternalArray__ICollection_get_IsReadOnly()
        {
            return true;
        }

        internal IEnumerator<T> InternalArray__IEnumerable_GetEnumerator<T>()
        {
            return Length == 0 ? SZGenericArrayEnumerator<T>.Empty : new SZGenericArrayEnumerator<T>(Unsafe.As<T[]>(this));
        }

        internal void InternalArray__ICollection_Clear()
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
        }

        internal void InternalArray__ICollection_Add<T>(T item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        internal bool InternalArray__ICollection_Remove<T>(T item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
            return default;
        }

        internal bool InternalArray__ICollection_Contains<T>(T item)
        {
            return IndexOf((T[])this, item, 0, Length) >= 0;
        }

        internal void InternalArray__ICollection_CopyTo<T>(T[] array, int arrayIndex)
        {
            Copy(this, GetLowerBound(0), array, arrayIndex, Length);
        }

        internal T InternalArray__IReadOnlyList_get_Item<T>(int index)
        {
            if ((uint)index >= (uint)Length)
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();

            T value;
            // Do not change this to call GetGenericValue_icall directly, due to special casing in the runtime.
            GetGenericValueImpl(index, out value);
            return value;
        }

        internal int InternalArray__IReadOnlyCollection_get_Count()
        {
            return Length;
        }

        internal void InternalArray__Insert<T>(int index, T item)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        internal void InternalArray__RemoveAt(int index)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        internal int InternalArray__IndexOf<T>(T item)
        {
            return IndexOf((T[])this, item, 0, Length);
        }

        internal T InternalArray__get_Item<T>(int index)
        {
            if ((uint)index >= (uint)Length)
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();

            T value;
            // Do not change this to call GetGenericValue_icall directly, due to special casing in the runtime.
            GetGenericValueImpl(index, out value);
            return value;
        }

        internal void InternalArray__set_Item<T>(int index, T item)
        {
            if ((uint)index >= (uint)Length)
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();

            if (this is object?[] oarray)
            {
                oarray[index] = item;
                return;
            }

            // Do not change this to call SetGenericValue_icall directly, due to special casing in the runtime.
            SetGenericValueImpl(index, ref item);
        }
#pragma warning restore CA1822
    }
}
