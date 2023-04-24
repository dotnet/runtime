// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        internal sealed class RawData
        {
            public IntPtr Bounds;
            public uint Count;
#if !TARGET_32BIT
            private uint _Pad;
#endif
            public byte Data;
        }

        public int Length
        {
            [Intrinsic]
            get => Length;
        }

        // This could return a length greater than int.MaxValue
        internal nuint NativeLength
        {
            get => (nuint)Unsafe.As<RawData>(this).Count;
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

        public static unsafe void Clear(Array array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            ref byte ptr = ref MemoryMarshal.GetArrayDataReference(array);
            nuint byteLength = array.NativeLength * (nuint)(uint)array.GetElementSize() /* force zero-extension */;

            if (RuntimeHelpers.ObjectHasReferences(array))
                SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref ptr), byteLength / (uint)sizeof(IntPtr));
            else
                SpanHelpers.ClearWithoutReferences(ref ptr, byteLength);
        }

        public static unsafe void Clear(Array array, int index, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            int lowerBound = array.GetLowerBound(0);
            int elementSize = array.GetElementSize();
            nuint numComponents = array.NativeLength;

            int offset = index - lowerBound;

            if (index < lowerBound || offset < 0 || length < 0 || (uint)(offset + length) > numComponents)
                ThrowHelper.ThrowIndexOutOfRangeException();

            ref byte ptr = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(array), (uint)offset * (nuint)elementSize);
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
            ArgumentNullException.ThrowIfNull(sourceArray);
            ArgumentNullException.ThrowIfNull(destinationArray);

            Copy(sourceArray, sourceArray.GetLowerBound(0), destinationArray,
                destinationArray.GetLowerBound(0), length);
        }

        public static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length, false);
        }

        private static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
        {
            ArgumentNullException.ThrowIfNull(sourceArray);
            ArgumentNullException.ThrowIfNull(destinationArray);

            ArgumentOutOfRangeException.ThrowIfNegative(length);

            if (sourceArray.Rank != destinationArray.Rank)
                throw new RankException(SR.Rank_MultiDimNotSupported);

            if (sourceIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), SR.ArgumentOutOfRange_NeedNonNegNum);

            var src = sourceArray;
            var dst = destinationArray;
            if (FastCopy(ObjectHandleOnStack.Create(ref src), sourceIndex, ObjectHandleOnStack.Create(ref dst), destinationIndex, length))
                return;

            CopySlow(sourceArray, sourceIndex, destinationArray, destinationIndex, length, reliable);
        }

        private static void CopySlow(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
        {
            int source_pos = sourceIndex - sourceArray.GetLowerBound(0);
            int dest_pos = destinationIndex - destinationArray.GetLowerBound(0);

            if (source_pos < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), SR.ArgumentOutOfRange_ArrayLB);

            if (dest_pos < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), SR.ArgumentOutOfRange_ArrayLB);

            // re-ordered to avoid possible integer overflow
            if (source_pos > sourceArray.Length - length)
                throw new ArgumentException(SR.Arg_LongerThanSrcArray, nameof(sourceArray));

            if (dest_pos > destinationArray.Length - length)
                throw new ArgumentException(SR.Arg_LongerThanDestArray, nameof(destinationArray));

            Type src_type = sourceArray.GetType().GetElementType()!;
            Type dst_type = destinationArray.GetType().GetElementType()!;
            Type dst_elem_type = dst_type;
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
                    !(dst_type.IsPrimitive && src_type.IsPrimitive &&
                      CanChangePrimitive(ObjectHandleOnStack.Create(ref dst_type), ObjectHandleOnStack.Create(ref src_type), true)))
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

            Array src = sourceArray;
            ObjectHandleOnStack src_handle = ObjectHandleOnStack.Create(ref src);
            Array dst = destinationArray;
            ObjectHandleOnStack dst_handle = ObjectHandleOnStack.Create(ref dst);
            object? srcval = null;
            ObjectHandleOnStack val_handle = ObjectHandleOnStack.Create(ref srcval);

            if (!ReferenceEquals(sourceArray, destinationArray) || source_pos > dest_pos)
            {
                for (int i = 0; i < length; i++)
                {
                    GetValueImpl(src_handle, val_handle, source_pos + i);

                    if (dst_type_vt && (srcval == null || (src_type == typeof(object) && !dst_elem_type.IsAssignableFrom (srcval.GetType()))))
                        throw new InvalidCastException(SR.InvalidCast_DownCastArrayElement);

                    try
                    {
                        SetValueRelaxedImpl(dst_handle, val_handle, dest_pos + i);
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
                    GetValueImpl(src_handle, val_handle, source_pos + i);

                    try
                    {
                        SetValueRelaxedImpl(dst_handle, val_handle, dest_pos + i);
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
                    return CanChangePrimitive(ObjectHandleOnStack.Create(ref source), ObjectHandleOnStack.Create(ref target), false);
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

        private static unsafe Array InternalCreate(RuntimeType elementType, int rank, int* lengths, int* lowerBounds)
        {
            Array? array = null;
            InternalCreate(ref array, elementType._impl.Value,  rank, lengths, lowerBounds);
            GC.KeepAlive(elementType);
            return array!;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe void InternalCreate(ref Array? result, IntPtr elementType, int rank, int* lengths, int* lowerBounds);

        private unsafe nint GetFlattenedIndex(int rawIndex)
        {
            // Checked by the caller
            Debug.Assert(Rank == 1);

            int index = rawIndex - GetLowerBound(0);
            int length = GetLength(0);

            if ((uint)index >= (uint)length)
                ThrowHelper.ThrowIndexOutOfRangeException();

            Debug.Assert((uint)index < (nuint)LongLength);
            return index;
        }

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

            Array self = this;
            object? res = null;
            GetValueImpl(ObjectHandleOnStack.Create(ref self), ObjectHandleOnStack.Create(ref res), (int)index);
            return res;
        }

        internal void InternalSetValue(object? value, nint index)
        {
            if (GetType().GetElementType()!.IsPointer)
                throw new NotSupportedException(SR.NotSupported_Type);

            Array self = this;
            SetValueImpl(ObjectHandleOnStack.Create(ref self), ObjectHandleOnStack.Create(ref value), (int)index);
        }

        public void Initialize()
        {
            object arr = this;
            InitializeInternal(ObjectHandleOnStack.Create(ref arr));
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

        internal CorElementType GetCorElementTypeOfElementType()
        {
            object arr = this;
            return GetCorElementTypeOfElementTypeInternal(ObjectHandleOnStack.Create(ref arr));
        }

        private bool IsValueOfElementType(object value)
        {
            object arr = this;
            return IsValueOfElementTypeInternal(ObjectHandleOnStack.Create(ref arr), ObjectHandleOnStack.Create(ref value));
        }

        [Intrinsic] // when dimension is `0` constant
        public int GetLength(int dimension)
        {
            object arr = this;
            return GetLengthInternal(ObjectHandleOnStack.Create(ref arr), dimension);
        }

        [Intrinsic] // when dimension is `0` constant
        public int GetLowerBound(int dimension)
        {
            object arr = this;
            return GetLowerBoundInternal(ObjectHandleOnStack.Create(ref arr), dimension);
        }

        [Intrinsic]
        internal int GetElementSize() => GetElementSize();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern CorElementType GetCorElementTypeOfElementTypeInternal(ObjectHandleOnStack arr);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool IsValueOfElementTypeInternal(ObjectHandleOnStack arr, ObjectHandleOnStack obj);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool CanChangePrimitive(ObjectHandleOnStack srcType, ObjectHandleOnStack dstType, bool reliable);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool FastCopy(ObjectHandleOnStack source, int source_idx, ObjectHandleOnStack dest, int dest_idx, int length);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetLengthInternal(ObjectHandleOnStack arr, int dimension);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int GetLowerBoundInternal(ObjectHandleOnStack arr, int dimension);

        // CAUTION! No bounds checking!
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetGenericValue_icall<T>(ObjectHandleOnStack self, int pos, out T value);

        // CAUTION! No bounds checking!
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetValueImpl(ObjectHandleOnStack arr, ObjectHandleOnStack res, int pos);

        // CAUTION! No bounds checking!
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SetGenericValue_icall<T>(ObjectHandleOnStack arr, int pos, ref T value);

        [Intrinsic]
        private void GetGenericValueImpl<T>(int pos, out T value)
        {
            Array self = this;
            GetGenericValue_icall(ObjectHandleOnStack.Create(ref self), pos, out value);
        }

        [Intrinsic]
        private void SetGenericValueImpl<T>(int pos, ref T value)
        {
            Array self = this;
            SetGenericValue_icall(ObjectHandleOnStack.Create(ref self), pos, ref value);
        }

        // CAUTION! No bounds checking!
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SetValueImpl(ObjectHandleOnStack arr, ObjectHandleOnStack value, int pos);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InitializeInternal(ObjectHandleOnStack arr);

        // CAUTION! No bounds checking!
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SetValueRelaxedImpl(ObjectHandleOnStack arr, ObjectHandleOnStack value, int pos);

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

        internal void InternalArray__ICollection_Add<T>(T _)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        internal bool InternalArray__ICollection_Remove<T>(T _)
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
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();

            T value;
            // Do not change this to call GetGenericValue_icall directly, due to special casing in the runtime.
            GetGenericValueImpl(index, out value);
            return value;
        }

        internal int InternalArray__IReadOnlyCollection_get_Count()
        {
            return Length;
        }

        internal void InternalArray__Insert<T>(int _, T _1)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        internal void InternalArray__RemoveAt(int _)
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
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();

            T value;
            // Do not change this to call GetGenericValue_icall directly, due to special casing in the runtime.
            GetGenericValueImpl(index, out value);
            return value;
        }

        internal void InternalArray__set_Item<T>(int index, T item)
        {
            if ((uint)index >= (uint)Length)
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();

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
