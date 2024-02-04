// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime;

#pragma warning disable 8500 // sizeof of managed types

namespace System
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract partial class Array : ICloneable, IList, IStructuralComparable, IStructuralEquatable
    {
        // This is the threshold where Introspective sort switches to Insertion sort.
        // Empirically, 16 seems to speed up most cases without slowing down others, at least for integers.
        // Large value types may benefit from a smaller number.
        internal const int IntrosortSizeThreshold = 16;

        // This ctor exists solely to prevent C# from generating a protected .ctor that violates the surface area.
        private protected Array() { }

        public static ReadOnlyCollection<T> AsReadOnly<T>(T[] array)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            return array.Length == 0 ?
                ReadOnlyCollection<T>.Empty :
                new ReadOnlyCollection<T>(array);
        }

        public static void Resize<T>([NotNull] ref T[]? array, int newSize)
        {
            if (newSize < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.newSize, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            T[]? larray = array; // local copy
            if (larray == null)
            {
                array = new T[newSize];
                return;
            }

            if (larray.Length != newSize)
            {
                // Due to array variance, it's possible that the incoming array is
                // actually of type U[], where U:T; or that an int[] <-> uint[] or
                // similar cast has occurred. In any case, since it's always legal
                // to reinterpret U as T in this scenario (but not necessarily the
                // other way around), we can use Buffer.Memmove here.

                T[] newArray = new T[newSize];
                Buffer.Memmove(
                    ref MemoryMarshal.GetArrayDataReference(newArray),
                    ref MemoryMarshal.GetArrayDataReference(larray),
                    (uint)Math.Min(newSize, larray.Length));
                array = newArray;
            }

            Debug.Assert(array != null);
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public static unsafe Array CreateInstance(Type elementType, int length)
        {
            ArgumentNullException.ThrowIfNull(elementType);
            ArgumentOutOfRangeException.ThrowIfNegative(length);

            RuntimeType? t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            return InternalCreate(t, 1, &length, null);
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "MDArrays of Rank != 1 can be created because they don't implement generic interfaces.")]
        public static unsafe Array CreateInstance(Type elementType, int length1, int length2)
        {
            ArgumentNullException.ThrowIfNull(elementType);
            ArgumentOutOfRangeException.ThrowIfNegative(length1);
            ArgumentOutOfRangeException.ThrowIfNegative(length2);

            RuntimeType? t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            int* pLengths = stackalloc int[] { length1, length2 };
            return InternalCreate(t, 2, pLengths, null);
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "MDArrays of Rank != 1 can be created because they don't implement generic interfaces.")]
        public static unsafe Array CreateInstance(Type elementType, int length1, int length2, int length3)
        {
            ArgumentNullException.ThrowIfNull(elementType);
            ArgumentOutOfRangeException.ThrowIfNegative(length1);
            ArgumentOutOfRangeException.ThrowIfNegative(length2);
            ArgumentOutOfRangeException.ThrowIfNegative(length3);

            RuntimeType? t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            int* pLengths = stackalloc int[] { length1, length2, length3 };
            return InternalCreate(t, 3, pLengths, null);
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public static unsafe Array CreateInstance(Type elementType, params int[] lengths)
        {
            ArgumentNullException.ThrowIfNull(elementType);
            ArgumentNullException.ThrowIfNull(lengths);

            if (lengths.Length == 0)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NeedAtLeast1Rank);

            RuntimeType? t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            // Check to make sure the lengths are all non-negative. Note that we check this here to give
            // a good exception message if they are not; however we check this again inside the execution
            // engine's low level allocation function after having made a copy of the array to prevent a
            // malicious caller from mutating the array after this check.
            for (int i = 0; i < lengths.Length; i++)
                if (lengths[i] < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            fixed (int* pLengths = &lengths[0])
                return InternalCreate(t, lengths.Length, pLengths, null);
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public static unsafe Array CreateInstance(Type elementType, int[] lengths, int[] lowerBounds)
        {
            ArgumentNullException.ThrowIfNull(elementType);
            ArgumentNullException.ThrowIfNull(lengths);
            ArgumentNullException.ThrowIfNull(lowerBounds);

            if (lengths.Length != lowerBounds.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RanksAndBounds);
            if (lengths.Length == 0)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NeedAtLeast1Rank);

            RuntimeType? t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            // Check to make sure the lengths are all non-negative. Note that we check this here to give
            // a good exception message if they are not; however we check this again inside the execution
            // engine's low level allocation function after having made a copy of the array to prevent a
            // malicious caller from mutating the array after this check.
            for (int i = 0; i < lengths.Length; i++)
                if (lengths[i] < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            fixed (int* pLengths = &lengths[0])
            fixed (int* pLowerBounds = &lowerBounds[0])
                return InternalCreate(t, lengths.Length, pLengths, pLowerBounds);
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        public static Array CreateInstance(Type elementType, params long[] lengths)
        {
            ArgumentNullException.ThrowIfNull(lengths);

            int[] intLengths = new int[lengths.Length];

            for (int i = 0; i < lengths.Length; ++i)
            {
                long len = lengths[i];
                int ilen = (int)len;
                if (len != ilen)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.len, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
                intLengths[i] = ilen;
            }

            return CreateInstance(elementType, intLengths);
        }

        /// <summary>
        /// Creates a one-dimensional <see cref="Array"/> of the specified array type and length, with zero-based indexing.
        /// </summary>
        /// <param name="arrayType">The type of the array (not of the array element type).</param>
        /// <param name="length">The size of the <see cref="Array"/> to create.</param>
        /// <returns>A new one-dimensional <see cref="Array"/> of the specified <see cref="Type"/> with the specified length.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="arrayType"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        /// <exception cref="ArgumentException"><para><paramref name="arrayType"/> is not an array type.</para>
        /// <para>-or-</para>
        /// <para><paramref name="arrayType"/> is not one-dimensional array.</para>
        /// </exception>
        /// <remarks>When the array type is readily available, this method should be preferred over <see cref="CreateInstance(Type, int)"/>, as it has
        /// better performance and it is AOT-friendly.</remarks>
        public static unsafe Array CreateInstanceFromArrayType(Type arrayType, int length)
        {
            ArgumentNullException.ThrowIfNull(arrayType);
            ArgumentOutOfRangeException.ThrowIfNegative(length);

            RuntimeType? t = arrayType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.arrayType);

            if (!t.IsArray)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_HasToBeArrayClass, ExceptionArgument.arrayType);

            if (t.GetArrayRank() != 1)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported, ExceptionArgument.arrayType);

            return InternalCreateFromArrayType(t, 1, &length, null);
        }

        /// <summary>
        /// Creates a multidimensional <see cref="Array"/> of the specified <see cref="Type"/> and dimension lengths, with zero-based indexing.
        /// </summary>
        /// <param name="arrayType">The type of the array (not of the array element type).</param>
        /// <param name="lengths">The dimension lengths, specified in an array of 32-bit integers.</param>
        /// <returns>A new multidimensional <see cref="Array"/> of the specified Type with the specified length for each dimension, using zero-based indexing.</returns>
        /// <exception cref="ArgumentNullException"><para><paramref name="arrayType"/> is null.</para>
        /// <para>-or-</para>
        /// <para><paramref name="lengths"/> is null.</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Any value in <paramref name="lengths"/> is less than zero.</exception>
        /// <exception cref="ArgumentException"><para>The lengths array is empty.</para>
        /// <para>-or-</para>
        /// <para><paramref name="arrayType"/> is not an array type.</para>
        /// <para>-or-</para>
        /// <para><paramref name="arrayType"/> rank does not match <paramref name="lengths"/> length.</para>
        /// </exception>
        /// <remarks>When the array type is readily available, this method should be preferred over <see cref="CreateInstance(Type, int[])"/>, as it has
        /// better performance and it is AOT-friendly.</remarks>
        public static unsafe Array CreateInstanceFromArrayType(Type arrayType, params int[] lengths)
        {
            ArgumentNullException.ThrowIfNull(arrayType);
            ArgumentNullException.ThrowIfNull(lengths);

            RuntimeType? t = arrayType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.arrayType);

            if (!t.IsArray)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_HasToBeArrayClass, ExceptionArgument.arrayType);

            if (t.GetArrayRank() != lengths.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);

            // Check to make sure the lengths are all non-negative. Note that we check this here to give
            // a good exception message if they are not; however we check this again inside the execution
            // engine's low level allocation function after having made a copy of the array to prevent a
            // malicious caller from mutating the array after this check.
            for (int i = 0; i < lengths.Length; i++)
                if (lengths[i] < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            fixed (int* pLengths = &lengths[0])
                return InternalCreateFromArrayType(t, lengths.Length, pLengths, null);
        }

        /// <summary>
        /// Creates a multidimensional <see cref="Array"/> of the specified <see cref="Type"/> and dimension lengths, with the specified lower bounds.
        /// </summary>
        /// <param name="arrayType">The type of the array (not of the array element type).</param>
        /// <param name="lengths">The dimension lengths, specified in an array of 32-bit integers.</param>
        /// <param name="lowerBounds">A one-dimensional array that contains the lower bound (starting index) of each dimension of the <see cref="Array"/> to create.</param>
        /// <returns>A new multidimensional <see cref="Array"/> of the specified <see cref="Type"/> with the specified length and lower bound for each dimension.</returns>
        /// <exception cref="ArgumentNullException"><para><paramref name="arrayType"/> is null.</para>
        /// <para>-or-</para>
        /// <para><paramref name="lengths"/> is null.</para>
        /// <para>-or-</para>
        /// <para><paramref name="lowerBounds"/> is null.</para>
        /// </exception>
        /// <exception cref="ArgumentException"><para>The <paramref name="lengths"/> and <paramref name="lowerBounds"/> arrays do not contain the same number of elements.</para>
        /// <para>-or-</para>
        /// <para>The lengths array is empty.</para>
        /// <para>-or-</para>
        /// <para><paramref name="arrayType"/> is not an array type.</para>
        /// <para>-or-</para>
        /// <para><paramref name="arrayType"/> rank does not match <paramref name="lengths"/> length.</para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">Any value in <paramref name="lengths"/> is less than zero.</exception>
        /// <exception cref="PlatformNotSupportedException">Native AOT: any value in <paramref name="lowerBounds"/> is different than zero.</exception>
        /// <remarks>When the array type is readily available, this method should be preferred over <see cref="CreateInstance(Type, int[], int[])"/>, as it has
        /// better performance and it is AOT-friendly.</remarks>
        public static unsafe Array CreateInstanceFromArrayType(Type arrayType, int[] lengths, int[] lowerBounds)
        {
            ArgumentNullException.ThrowIfNull(arrayType);
            ArgumentNullException.ThrowIfNull(lengths);
            ArgumentNullException.ThrowIfNull(lowerBounds);

            if (lengths.Length != lowerBounds.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RanksAndBounds);

            RuntimeType? t = arrayType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.arrayType);

            if (!t.IsArray)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_HasToBeArrayClass, ExceptionArgument.arrayType);

            if (t.GetArrayRank() != lengths.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);

            if (lowerBounds[0] != 0 && t.IsSZArray)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NonZeroLowerBound);

            // Check to make sure the lengths are all non-negative. Note that we check this here to give
            // a good exception message if they are not; however we check this again inside the execution
            // engine's low level allocation function after having made a copy of the array to prevent a
            // malicious caller from mutating the array after this check.
            for (int i = 0; i < lengths.Length; i++)
                if (lengths[i] < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            fixed (int* pLengths = &lengths[0])
            fixed (int* pLowerBounds = &lowerBounds[0])
                return InternalCreateFromArrayType(t, lengths.Length, pLengths, pLowerBounds);
        }

        public static void Copy(Array sourceArray, Array destinationArray, long length)
        {
            int ilength = (int)length;
            if (length != ilength)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);

            Copy(sourceArray, destinationArray, ilength);
        }

        public static void Copy(Array sourceArray, long sourceIndex, Array destinationArray, long destinationIndex, long length)
        {
            int isourceIndex = (int)sourceIndex;
            int idestinationIndex = (int)destinationIndex;
            int ilength = (int)length;

            if (sourceIndex != isourceIndex)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceIndex, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
            if (destinationIndex != idestinationIndex)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.destinationIndex, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
            if (length != ilength)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);

            Copy(sourceArray, isourceIndex, destinationArray, idestinationIndex, ilength);
        }

#if !MONO // implementation details of MethodTable

        // Copies length elements from sourceArray, starting at index 0, to
        // destinationArray, starting at index 0.
        public static unsafe void Copy(Array sourceArray, Array destinationArray, int length)
        {
            if (sourceArray is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.sourceArray);
            if (destinationArray is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.destinationArray);

            MethodTable* pMT = RuntimeHelpers.GetMethodTable(sourceArray);
            if (MethodTable.AreSameType(pMT, RuntimeHelpers.GetMethodTable(destinationArray)) &&
                !pMT->IsMultiDimensionalArray &&
                (uint)length <= sourceArray.NativeLength &&
                (uint)length <= destinationArray.NativeLength)
            {
                nuint byteCount = (uint)length * (nuint)pMT->ComponentSize;
                ref byte src = ref Unsafe.As<RawArrayData>(sourceArray).Data;
                ref byte dst = ref Unsafe.As<RawArrayData>(destinationArray).Data;

                if (pMT->ContainsGCPointers)
                    Buffer.BulkMoveWithWriteBarrier(ref dst, ref src, byteCount);
                else
                    Buffer.Memmove(ref dst, ref src, byteCount);

                // GC.KeepAlive(sourceArray) not required. pMT kept alive via sourceArray
                return;
            }

            // Less common
            CopyImpl(sourceArray, sourceArray.GetLowerBound(0), destinationArray, destinationArray.GetLowerBound(0), length, reliable: false);
        }

        // Copies length elements from sourceArray, starting at sourceIndex, to
        // destinationArray, starting at destinationIndex.
        public static unsafe void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            if (sourceArray != null && destinationArray != null)
            {
                MethodTable* pMT = RuntimeHelpers.GetMethodTable(sourceArray);
                if (MethodTable.AreSameType(pMT, RuntimeHelpers.GetMethodTable(destinationArray)) &&
                    !pMT->IsMultiDimensionalArray &&
                    length >= 0 && sourceIndex >= 0 && destinationIndex >= 0 &&
                    (uint)(sourceIndex + length) <= sourceArray.NativeLength &&
                    (uint)(destinationIndex + length) <= destinationArray.NativeLength)
                {
                    nuint elementSize = (nuint)pMT->ComponentSize;
                    nuint byteCount = (uint)length * elementSize;
                    ref byte src = ref Unsafe.AddByteOffset(ref Unsafe.As<RawArrayData>(sourceArray).Data, (uint)sourceIndex * elementSize);
                    ref byte dst = ref Unsafe.AddByteOffset(ref Unsafe.As<RawArrayData>(destinationArray).Data, (uint)destinationIndex * elementSize);

                    if (pMT->ContainsGCPointers)
                        Buffer.BulkMoveWithWriteBarrier(ref dst, ref src, byteCount);
                    else
                        Buffer.Memmove(ref dst, ref src, byteCount);

                    // GC.KeepAlive(sourceArray) not required. pMT kept alive via sourceArray
                    return;
                }
            }

            // Less common
            CopyImpl(sourceArray!, sourceIndex, destinationArray!, destinationIndex, length, reliable: false);
        }
#endif

        // The various Get values...
        public object? GetValue(params int[] indices)
        {
            if (indices == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            if (Rank != indices.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);

            return InternalGetValue(GetFlattenedIndex(indices));
        }

        public object? GetValue(int index)
        {
            if (Rank != 1)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need1DArray);

            return InternalGetValue(GetFlattenedIndex(index));
        }

        public object? GetValue(int index1, int index2)
        {
            if (Rank != 2)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need2DArray);

            return InternalGetValue(GetFlattenedIndex([index1, index2]));
        }

        public object? GetValue(int index1, int index2, int index3)
        {
            if (Rank != 3)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need3DArray);

            return InternalGetValue(GetFlattenedIndex([index1, index2, index3]));
        }

        public void SetValue(object? value, int index)
        {
            if (Rank != 1)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need1DArray);

            InternalSetValue(value, GetFlattenedIndex(index));
        }

        public void SetValue(object? value, int index1, int index2)
        {
            if (Rank != 2)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need2DArray);

            InternalSetValue(value, GetFlattenedIndex([index1, index2]));
        }

        public void SetValue(object? value, int index1, int index2, int index3)
        {
            if (Rank != 3)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need3DArray);

            InternalSetValue(value, GetFlattenedIndex([index1, index2, index3]));
        }

        public void SetValue(object? value, params int[] indices)
        {
            if (indices == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            if (Rank != indices.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);

            InternalSetValue(value, GetFlattenedIndex(indices));
        }

        public object? GetValue(long index)
        {
            int iindex = (int)index;
            if (index != iindex)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);

            return this.GetValue(iindex);
        }

        public object? GetValue(long index1, long index2)
        {
            int iindex1 = (int)index1;
            int iindex2 = (int)index2;

            if (index1 != iindex1)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index1, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
            if (index2 != iindex2)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index2, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);

            return this.GetValue(iindex1, iindex2);
        }

        public object? GetValue(long index1, long index2, long index3)
        {
            int iindex1 = (int)index1;
            int iindex2 = (int)index2;
            int iindex3 = (int)index3;

            if (index1 != iindex1)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index1, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
            if (index2 != iindex2)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index2, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
            if (index3 != iindex3)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index3, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);

            return this.GetValue(iindex1, iindex2, iindex3);
        }

        public object? GetValue(params long[] indices)
        {
            if (indices == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            if (Rank != indices.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);

            int[] intIndices = new int[indices.Length];

            for (int i = 0; i < indices.Length; ++i)
            {
                long index = indices[i];
                int iindex = (int)index;
                if (index != iindex)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
                intIndices[i] = iindex;
            }

            return this.GetValue(intIndices);
        }

        public void SetValue(object? value, long index)
        {
            int iindex = (int)index;

            if (index != iindex)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);

            this.SetValue(value, iindex);
        }

        public void SetValue(object? value, long index1, long index2)
        {
            int iindex1 = (int)index1;
            int iindex2 = (int)index2;

            if (index1 != iindex1)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index1, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
            if (index2 != iindex2)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index2, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);

            this.SetValue(value, iindex1, iindex2);
        }

        public void SetValue(object? value, long index1, long index2, long index3)
        {
            int iindex1 = (int)index1;
            int iindex2 = (int)index2;
            int iindex3 = (int)index3;

            if (index1 != iindex1)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index1, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
            if (index2 != iindex2)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index2, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
            if (index3 != iindex3)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index3, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);

            this.SetValue(value, iindex1, iindex2, iindex3);
        }

        public void SetValue(object? value, params long[] indices)
        {
            if (indices == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            if (Rank != indices.Length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);

            int[] intIndices = new int[indices.Length];

            for (int i = 0; i < indices.Length; ++i)
            {
                long index = indices[i];
                int iindex = (int)index;
                if (index != iindex)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);
                intIndices[i] = iindex;
            }

            this.SetValue(value, intIndices);
        }

        private static int GetMedian(int low, int hi)
        {
            // Note both may be negative, if we are dealing with arrays w/ negative lower bounds.
            Debug.Assert(low <= hi);
            Debug.Assert(hi - low >= 0, "Length overflow!");
            return low + ((hi - low) >> 1);
        }

        public long GetLongLength(int dimension)
        {
            // This method should throw an IndexOufOfRangeException for compat if dimension < 0 or >= Rank
            return GetLength(dimension);
        }

        // Number of elements in the Array.
        int ICollection.Count => Length;

        // Returns an object appropriate for synchronizing access to this
        // Array.
        public object SyncRoot => this;

        // Is this Array read-only?
        public bool IsReadOnly => false;

        public bool IsFixedSize => true;

        // Is this Array synchronized (i.e., thread-safe)?  If you want a synchronized
        // collection, you can use SyncRoot as an object to synchronize your
        // collection with.  You could also call GetSynchronized()
        // to get a synchronized wrapper around the Array.
        public bool IsSynchronized => false;

        object? IList.this[int index]
        {
            get => GetValue(index);
            set => SetValue(value, index);
        }

        int IList.Add(object? value)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
            return default;
        }

        bool IList.Contains(object? value)
        {
            return IndexOf(this, value) >= this.GetLowerBound(0);
        }

        void IList.Clear()
        {
            Clear(this);
        }

        int IList.IndexOf(object? value)
        {
            return IndexOf(this, value);
        }

        void IList.Insert(int index, object? value)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        void IList.Remove(object? value)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        void IList.RemoveAt(int index)
        {
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        // Make a new array which is a shallow copy of the original array.
        //
        [Intrinsic]
        public object Clone()
        {
            return MemberwiseClone();
        }

        int IStructuralComparable.CompareTo(object? other, IComparer comparer)
        {
            if (other == null)
            {
                return 1;
            }

            Array? o = other as Array;

            if (o == null || this.Length != o.Length)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.ArgumentException_OtherNotArrayOfCorrectLength, ExceptionArgument.other);
            }

            int i = 0;
            int c = 0;

            while (i < o.Length && c == 0)
            {
                object? left = GetValue(i);
                object? right = o.GetValue(i);

                c = comparer.Compare(left, right);
                i++;
            }

            return c;
        }

        bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer)
        {
            if (other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!(other is Array o) || o.Length != this.Length)
            {
                return false;
            }

            int i = 0;
            while (i < o.Length)
            {
                object? left = GetValue(i);
                object? right = o.GetValue(i);

                if (!comparer.Equals(left, right))
                {
                    return false;
                }
                i++;
            }

            return true;
        }

        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer)
        {
            if (comparer == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparer);

            HashCode hashCode = default;

            for (int i = (this.Length >= 8 ? this.Length - 8 : 0); i < this.Length; i++)
            {
                hashCode.Add(comparer.GetHashCode(GetValue(i)!));
            }

            return hashCode.ToHashCode();
        }

        // Searches an array for a given element using a binary search algorithm.
        // Elements of the array are compared to the search value using the
        // IComparable interface, which must be implemented by all elements
        // of the array and the given search value. This method assumes that the
        // array is already sorted according to the IComparable interface;
        // if this is not the case, the result will be incorrect.
        //
        // The method returns the index of the given value in the array. If the
        // array does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value.
        //
        public static int BinarySearch(Array array, object? value)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            return BinarySearch(array, array.GetLowerBound(0), array.Length, value, null);
        }

        // Searches a section of an array for a given element using a binary search
        // algorithm. Elements of the array are compared to the search value using
        // the IComparable interface, which must be implemented by all
        // elements of the array and the given search value. This method assumes
        // that the array is already sorted according to the IComparable
        // interface; if this is not the case, the result will be incorrect.
        //
        // The method returns the index of the given value in the array. If the
        // array does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value.
        //
        public static int BinarySearch(Array array, int index, int length, object? value)
        {
            return BinarySearch(array, index, length, value, null);
        }

        // Searches an array for a given element using a binary search algorithm.
        // Elements of the array are compared to the search value using the given
        // IComparer interface. If comparer is null, elements of the
        // array are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the
        // array and the given search value. This method assumes that the array is
        // already sorted; if this is not the case, the result will be incorrect.
        //
        // The method returns the index of the given value in the array. If the
        // array does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value.
        //
        public static int BinarySearch(Array array, object? value, IComparer? comparer)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            return BinarySearch(array, array.GetLowerBound(0), array.Length, value, comparer);
        }

        // Searches a section of an array for a given element using a binary search
        // algorithm. Elements of the array are compared to the search value using
        // the given IComparer interface. If comparer is null,
        // elements of the array are compared to the search value using the
        // IComparable interface, which in that case must be implemented by
        // all elements of the array and the given search value. This method
        // assumes that the array is already sorted; if this is not the case, the
        // result will be incorrect.
        //
        // The method returns the index of the given value in the array. If the
        // array does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value.
        //
        public static int BinarySearch(Array array, int index, int length, object? value, IComparer? comparer)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            int lb = array.GetLowerBound(0);
            if (index < lb)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            if (array.Length - (index - lb) < length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            if (array.Rank != 1)
                ThrowHelper.ThrowRankException(ExceptionResource.Rank_MultiDimNotSupported);

            comparer ??= Comparer.Default;

            int lo = index;
            int hi = index + length - 1;
            if (array is object[] objArray)
            {
                while (lo <= hi)
                {
                    // i might overflow if lo and hi are both large positive numbers.
                    int i = GetMedian(lo, hi);

                    int c;
                    try
                    {
                        c = comparer.Compare(objArray[i], value);
                    }
                    catch (Exception e)
                    {
                        ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
                        return default;
                    }
                    if (c == 0) return i;
                    if (c < 0)
                    {
                        lo = i + 1;
                    }
                    else
                    {
                        hi = i - 1;
                    }
                }
                return ~lo;
            }

            if (comparer == Comparer.Default)
            {
                CorElementType et = array.GetCorElementTypeOfElementType();
                if (et.IsPrimitiveType())
                {
                    if (value == null)
                        return ~index;

                    if (array.IsValueOfElementType(value))
                    {
                        int adjustedIndex = index - lb;
                        int result = -1;
                        switch (et)
                        {
                            case CorElementType.ELEMENT_TYPE_I1:
                                result = GenericBinarySearch<sbyte>(array, adjustedIndex, length, value);
                                break;
                            case CorElementType.ELEMENT_TYPE_U1:
                            case CorElementType.ELEMENT_TYPE_BOOLEAN:
                                result = GenericBinarySearch<byte>(array, adjustedIndex, length, value);
                                break;
                            case CorElementType.ELEMENT_TYPE_I2:
                                result = GenericBinarySearch<short>(array, adjustedIndex, length, value);
                                break;
                            case CorElementType.ELEMENT_TYPE_U2:
                            case CorElementType.ELEMENT_TYPE_CHAR:
                                result = GenericBinarySearch<ushort>(array, adjustedIndex, length, value);
                                break;
                            case CorElementType.ELEMENT_TYPE_I4:
#if TARGET_32BIT
                            case CorElementType.ELEMENT_TYPE_I:
#endif
                                result = GenericBinarySearch<int>(array, adjustedIndex, length, value);
                                break;
                            case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                            case CorElementType.ELEMENT_TYPE_U:
#endif
                                result = GenericBinarySearch<uint>(array, adjustedIndex, length, value);
                                break;
                            case CorElementType.ELEMENT_TYPE_I8:
#if TARGET_64BIT
                            case CorElementType.ELEMENT_TYPE_I:
#endif
                                result = GenericBinarySearch<long>(array, adjustedIndex, length, value);
                                break;
                            case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                            case CorElementType.ELEMENT_TYPE_U:
#endif
                                result = GenericBinarySearch<ulong>(array, adjustedIndex, length, value);
                                break;
                            case CorElementType.ELEMENT_TYPE_R4:
                                result = GenericBinarySearch<float>(array, adjustedIndex, length, value);
                                break;
                            case CorElementType.ELEMENT_TYPE_R8:
                                result = GenericBinarySearch<double>(array, adjustedIndex, length, value);
                                break;
                            default:
                                Debug.Fail("All primitive types should be handled above");
                                break;
                        }

                        return (result >= 0) ? (index + result) : ~(index + ~result);

                        static int GenericBinarySearch<T>(Array array, int adjustedIndex, int length, object value) where T : struct, IComparable<T>
                            => UnsafeArrayAsSpan<T>(array, adjustedIndex, length).BinarySearch(Unsafe.As<byte, T>(ref value.GetRawData()));
                    }
                }
            }

            while (lo <= hi)
            {
                int i = GetMedian(lo, hi);

                int c;
                try
                {
                    c = comparer.Compare(array.GetValue(i), value);
                }
                catch (Exception e)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
                    return default;
                }
                if (c == 0) return i;
                if (c < 0)
                {
                    lo = i + 1;
                }
                else
                {
                    hi = i - 1;
                }
            }
            return ~lo;
        }

        public static int BinarySearch<T>(T[] array, T value)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            return BinarySearch(array, 0, array.Length, value, null);
        }

        public static int BinarySearch<T>(T[] array, T value, IComparer<T>? comparer)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            return BinarySearch(array, 0, array.Length, value, comparer);
        }

        public static int BinarySearch<T>(T[] array, int index, int length, T value)
        {
            return BinarySearch(array, index, length, value, null);
        }

        public static int BinarySearch<T>(T[] array, int index, int length, T value, IComparer<T>? comparer)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (index < 0)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();

            if (array.Length - index < length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            return ArraySortHelper<T>.Default.BinarySearch(array, index, length, value, comparer);
        }

        public static TOutput[] ConvertAll<TInput, TOutput>(TInput[] array, Converter<TInput, TOutput> converter)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (converter == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.converter);
            }

            TOutput[] newArray = new TOutput[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                newArray[i] = converter(array[i]);
            }
            return newArray;
        }

        // CopyTo copies a collection into an Array, starting at a particular
        // index into the array.
        //
        // This method is to support the ICollection interface, and calls
        // Array.Copy internally.  If you aren't using ICollection explicitly,
        // call Array.Copy to avoid an extra indirection.
        //
        public void CopyTo(Array array, int index)
        {
            if (array != null && array.Rank != 1)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
            // Note: Array.Copy throws a RankException and we want a consistent ArgumentException for all the IList CopyTo methods.
            Copy(this, GetLowerBound(0), array!, index, Length);
        }

        public void CopyTo(Array array, long index)
        {
            int iindex = (int)index;
            if (index != iindex)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index, ExceptionResource.ArgumentOutOfRange_HugeArrayNotSupported);

            this.CopyTo(array, iindex);
        }

        private static class EmptyArray<T>
        {
#pragma warning disable CA1825 // this is the implementation of Array.Empty<T>()
            internal static readonly T[] Value = new T[0];
#pragma warning restore CA1825
        }

        public static T[] Empty<T>()
        {
            return EmptyArray<T>.Value;
        }

        public static bool Exists<T>(T[] array, Predicate<T> match)
        {
            return FindIndex(array, match) != -1;
        }

        public static void Fill<T>(T[] array, T value)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = value;
                }
            }
            else
            {
                new Span<T>(array).Fill(value);
            }
        }

        public static void Fill<T>(T[] array, T value, int startIndex, int count)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if ((uint)startIndex > (uint)array.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            if ((uint)count > (uint)(array.Length - startIndex))
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            if (!typeof(T).IsValueType && array.GetType() != typeof(T[]))
            {
                for (int i = startIndex; i < startIndex + count; i++)
                {
                    array[i] = value;
                }
            }
            else
            {
                ref T first = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), (nint)(uint)startIndex);
                new Span<T>(ref first, count).Fill(value);
            }
        }

        public static T? Find<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (match(array[i]))
                {
                    return array[i];
                }
            }
            return default;
        }

        public static T[] FindAll<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            List<T> list = new List<T>();
            for (int i = 0; i < array.Length; i++)
            {
                if (match(array[i]))
                {
                    list.Add(array[i]);
                }
            }
            return list.ToArray();
        }

        public static int FindIndex<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            return FindIndex(array, 0, array.Length, match);
        }

        public static int FindIndex<T>(T[] array, int startIndex, Predicate<T> match)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            return FindIndex(array, startIndex, array.Length - startIndex, match);
        }

        public static int FindIndex<T>(T[] array, int startIndex, int count, Predicate<T> match)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (startIndex < 0 || startIndex > array.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            if (count < 0 || startIndex > array.Length - count)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(array[i]))
                    return i;
            }
            return -1;
        }

        public static T? FindLast<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            for (int i = array.Length - 1; i >= 0; i--)
            {
                if (match(array[i]))
                {
                    return array[i];
                }
            }
            return default;
        }

        public static int FindLastIndex<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            return FindLastIndex(array, array.Length - 1, array.Length, match);
        }

        public static int FindLastIndex<T>(T[] array, int startIndex, Predicate<T> match)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            return FindLastIndex(array, startIndex, startIndex + 1, match);
        }

        public static int FindLastIndex<T>(T[] array, int startIndex, int count, Predicate<T> match)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            if (array.Length == 0)
            {
                // Special case for 0 length List
                if (startIndex != -1)
                {
                    ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
                }
            }
            else
            {
                // Make sure we're not out of range
                if (startIndex < 0 || startIndex >= array.Length)
                {
                    ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
                }
            }

            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            int endIndex = startIndex - count;
            for (int i = startIndex; i > endIndex; i--)
            {
                if (match(array[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public static void ForEach<T>(T[] array, Action<T> action)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (action == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.action);
            }

            for (int i = 0; i < array.Length; i++)
            {
                action(array[i]);
            }
        }

        // Returns the index of the first occurrence of a given value in an array.
        // The array is searched forwards, and the elements of the array are
        // compared to the given value using the Object.Equals method.
        //
        public static int IndexOf(Array array, object? value)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            return IndexOf(array, value, array.GetLowerBound(0), array.Length);
        }

        // Returns the index of the first occurrence of a given value in a range of
        // an array. The array is searched forwards, starting at index
        // startIndex and ending at the last element of the array. The
        // elements of the array are compared to the given value using the
        // Object.Equals method.
        //
        public static int IndexOf(Array array, object? value, int startIndex)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            int lb = array.GetLowerBound(0);
            return IndexOf(array, value, startIndex, array.Length - startIndex + lb);
        }

        // Returns the index of the first occurrence of a given value in a range of
        // an array. The array is searched forwards, starting at index
        // startIndex and upto count elements. The
        // elements of the array are compared to the given value using the
        // Object.Equals method.
        //
        public static int IndexOf(Array array, object? value, int startIndex, int count)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (array.Rank != 1)
                ThrowHelper.ThrowRankException(ExceptionResource.Rank_MultiDimNotSupported);

            int lb = array.GetLowerBound(0);
            if (startIndex < lb || startIndex > array.Length + lb)
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            if (count < 0 || count > array.Length - startIndex + lb)
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();

            int endIndex = startIndex + count;
            if (array is object[] objArray)
            {
                if (value == null)
                {
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        if (objArray[i] == null)
                            return i;
                    }
                }
                else
                {
                    for (int i = startIndex; i < endIndex; i++)
                    {
                        object obj = objArray[i];
                        if (obj != null && obj.Equals(value))
                            return i;
                    }
                }
                return -1;
            }

            CorElementType et = array.GetCorElementTypeOfElementType();
            if (et.IsPrimitiveType())
            {
                if (value == null)
                    return lb - 1;

                if (array.IsValueOfElementType(value))
                {
                    int adjustedIndex = startIndex - lb;
                    int result = -1;
                    switch (et)
                    {
                        case CorElementType.ELEMENT_TYPE_I1:
                        case CorElementType.ELEMENT_TYPE_U1:
                        case CorElementType.ELEMENT_TYPE_BOOLEAN:
                            result = GenericIndexOf<byte>(array, value, adjustedIndex, count);
                            break;
                        case CorElementType.ELEMENT_TYPE_I2:
                        case CorElementType.ELEMENT_TYPE_U2:
                        case CorElementType.ELEMENT_TYPE_CHAR:
                            result = GenericIndexOf<char>(array, value, adjustedIndex, count);
                            break;
                        case CorElementType.ELEMENT_TYPE_I4:
                        case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                        case CorElementType.ELEMENT_TYPE_I:
                        case CorElementType.ELEMENT_TYPE_U:
#endif
                            result = GenericIndexOf<int>(array, value, adjustedIndex, count);
                            break;
                        case CorElementType.ELEMENT_TYPE_I8:
                        case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                        case CorElementType.ELEMENT_TYPE_I:
                        case CorElementType.ELEMENT_TYPE_U:
#endif
                            result = GenericIndexOf<long>(array, value, adjustedIndex, count);
                            break;
                        case CorElementType.ELEMENT_TYPE_R4:
                            result = GenericIndexOf<float>(array, value, adjustedIndex, count);
                            break;
                        case CorElementType.ELEMENT_TYPE_R8:
                            result = GenericIndexOf<double>(array, value, adjustedIndex, count);
                            break;
                        default:
                            Debug.Fail("All primitive types should be handled above");
                            break;
                    }

                    return (result >= 0 ? startIndex : lb) + result;

                    static int GenericIndexOf<T>(Array array, object value, int adjustedIndex, int length) where T : struct, IEquatable<T>
                        => UnsafeArrayAsSpan<T>(array, adjustedIndex, length).IndexOf(Unsafe.As<byte, T>(ref value.GetRawData()));
                }
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                object? obj = array.GetValue(i);
                if (obj == null)
                {
                    if (value == null)
                        return i;
                }
                else
                {
                    if (obj.Equals(value))
                        return i;
                }
            }
            // Return one less than the lower bound of the array.  This way,
            // for arrays with a lower bound of -1 we will not return -1 when the
            // item was not found.  And for SZArrays (the vast majority), -1 still
            // works for them.
            return lb - 1;
        }

        public static int IndexOf<T>(T[] array, T value)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            return IndexOf(array, value, 0, array.Length);
        }

        public static int IndexOf<T>(T[] array, T value, int startIndex)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            return IndexOf(array, value, startIndex, array.Length - startIndex);
        }

        public static unsafe int IndexOf<T>(T[] array, T value, int startIndex, int count)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if ((uint)startIndex > (uint)array.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLessOrEqual();
            }

            if ((uint)count > (uint)(array.Length - startIndex))
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    int result = SpanHelpers.IndexOfValueType(
                        ref Unsafe.Add(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(array)), startIndex),
                        Unsafe.As<T, byte>(ref value),
                        count);
                    return (result >= 0 ? startIndex : 0) + result;
                }
                else if (sizeof(T) == sizeof(short))
                {
                    int result = SpanHelpers.IndexOfValueType(
                        ref Unsafe.Add(ref Unsafe.As<T, short>(ref MemoryMarshal.GetArrayDataReference(array)), startIndex),
                        Unsafe.As<T, short>(ref value),
                        count);
                    return (result >= 0 ? startIndex : 0) + result;
                }
                else if (sizeof(T) == sizeof(int))
                {
                    int result = SpanHelpers.IndexOfValueType(
                        ref Unsafe.Add(ref Unsafe.As<T, int>(ref MemoryMarshal.GetArrayDataReference(array)), startIndex),
                        Unsafe.As<T, int>(ref value),
                        count);
                    return (result >= 0 ? startIndex : 0) + result;
                }
                else if (sizeof(T) == sizeof(long))
                {
                    int result = SpanHelpers.IndexOfValueType(
                        ref Unsafe.Add(ref Unsafe.As<T, long>(ref MemoryMarshal.GetArrayDataReference(array)), startIndex),
                        Unsafe.As<T, long>(ref value),
                        count);
                    return (result >= 0 ? startIndex : 0) + result;
                }
            }

#if !NATIVEAOT
            return EqualityComparer<T>.Default.IndexOf(array, value, startIndex, count);
#else
            return IndexOfImpl(array, value, startIndex, count);
#endif
        }

        // Returns the index of the last occurrence of a given value in an array.
        // The array is searched backwards, and the elements of the array are
        // compared to the given value using the Object.Equals method.
        //
        public static int LastIndexOf(Array array, object? value)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            int lb = array.GetLowerBound(0);
            return LastIndexOf(array, value, array.Length - 1 + lb, array.Length);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // an array. The array is searched backwards, starting at index
        // startIndex and ending at index 0. The elements of the array are
        // compared to the given value using the Object.Equals method.
        //
        public static int LastIndexOf(Array array, object? value, int startIndex)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            int lb = array.GetLowerBound(0);
            return LastIndexOf(array, value, startIndex, startIndex + 1 - lb);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // an array. The array is searched backwards, starting at index
        // startIndex and counting uptocount elements. The elements of
        // the array are compared to the given value using the Object.Equals
        // method.
        //
        public static int LastIndexOf(Array array, object? value, int startIndex, int count)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            int lb = array.GetLowerBound(0);
            if (array.Length == 0)
            {
                return lb - 1;
            }

            if (startIndex < lb || startIndex >= array.Length + lb)
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
            if (count < 0)
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            if (count > startIndex - lb + 1)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.endIndex, ExceptionResource.ArgumentOutOfRange_EndIndexStartIndex);
            if (array.Rank != 1)
                ThrowHelper.ThrowRankException(ExceptionResource.Rank_MultiDimNotSupported);

            int endIndex = startIndex - count + 1;
            if (array is object[] objArray)
            {
                if (value == null)
                {
                    for (int i = startIndex; i >= endIndex; i--)
                    {
                        if (objArray[i] == null)
                            return i;
                    }
                }
                else
                {
                    for (int i = startIndex; i >= endIndex; i--)
                    {
                        object obj = objArray[i];
                        if (obj != null && obj.Equals(value))
                            return i;
                    }
                }
                return -1;
            }

            CorElementType et = array.GetCorElementTypeOfElementType();
            if (et.IsPrimitiveType())
            {
                if (value == null)
                    return lb - 1;

                if (array.IsValueOfElementType(value))
                {
                    int adjustedIndex = endIndex - lb;
                    int result = -1;
                    switch (et)
                    {
                        case CorElementType.ELEMENT_TYPE_I1:
                        case CorElementType.ELEMENT_TYPE_U1:
                        case CorElementType.ELEMENT_TYPE_BOOLEAN:
                            result = GenericLastIndexOf<byte>(array, value, adjustedIndex, count);
                            break;
                        case CorElementType.ELEMENT_TYPE_I2:
                        case CorElementType.ELEMENT_TYPE_U2:
                        case CorElementType.ELEMENT_TYPE_CHAR:
                            result = GenericLastIndexOf<char>(array, value, adjustedIndex, count);
                            break;
                        case CorElementType.ELEMENT_TYPE_I4:
                        case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                        case CorElementType.ELEMENT_TYPE_I:
                        case CorElementType.ELEMENT_TYPE_U:
#endif
                            result = GenericLastIndexOf<int>(array, value, adjustedIndex, count);
                            break;
                        case CorElementType.ELEMENT_TYPE_I8:
                        case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                        case CorElementType.ELEMENT_TYPE_I:
                        case CorElementType.ELEMENT_TYPE_U:
#endif
                            result = GenericLastIndexOf<long>(array, value, adjustedIndex, count);
                            break;
                        case CorElementType.ELEMENT_TYPE_R4:
                            result = GenericLastIndexOf<float>(array, value, adjustedIndex, count);
                            break;
                        case CorElementType.ELEMENT_TYPE_R8:
                            result = GenericLastIndexOf<double>(array, value, adjustedIndex, count);
                            break;
                        default:
                            Debug.Fail("All primitive types should be handled above");
                            break;
                    }

                    return (result >= 0 ? endIndex : lb) + result;

                    static int GenericLastIndexOf<T>(Array array, object value, int adjustedIndex, int length) where T : struct, IEquatable<T>
                        => UnsafeArrayAsSpan<T>(array, adjustedIndex, length).LastIndexOf(Unsafe.As<byte, T>(ref value.GetRawData()));
                }
            }

            for (int i = startIndex; i >= endIndex; i--)
            {
                object? obj = array.GetValue(i);
                if (obj == null)
                {
                    if (value == null)
                        return i;
                }
                else
                {
                    if (obj.Equals(value))
                        return i;
                }
            }
            return lb - 1;  // Return lb-1 for arrays with negative lower bounds.
        }

        public static int LastIndexOf<T>(T[] array, T value)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            return LastIndexOf(array, value, array.Length - 1, array.Length);
        }

        public static int LastIndexOf<T>(T[] array, T value, int startIndex)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }
            // if array is empty and startIndex is 0, we need to pass 0 as count
            return LastIndexOf(array, value, startIndex, (array.Length == 0) ? 0 : (startIndex + 1));
        }

        public static unsafe int LastIndexOf<T>(T[] array, T value, int startIndex, int count)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (array.Length == 0)
            {
                //
                // Special case for 0 length List
                // accept -1 and 0 as valid startIndex for compablility reason.
                //
                if (startIndex != -1 && startIndex != 0)
                {
                    ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
                }

                // only 0 is a valid value for count if array is empty
                if (count != 0)
                {
                    ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
                }
                return -1;
            }

            // Make sure we're not out of range
            if ((uint)startIndex >= (uint)array.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
            }

            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
            {
                ThrowHelper.ThrowCountArgumentOutOfRange_ArgumentOutOfRange_Count();
            }

            if (RuntimeHelpers.IsBitwiseEquatable<T>())
            {
                if (sizeof(T) == sizeof(byte))
                {
                    int endIndex = startIndex - count + 1;
                    int result = SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.Add(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(array)), endIndex),
                        Unsafe.As<T, byte>(ref value),
                        count);

                    return (result >= 0 ? endIndex : 0) + result;
                }
                else if (sizeof(T) == sizeof(short))
                {
                    int endIndex = startIndex - count + 1;
                    int result = SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.Add(ref Unsafe.As<T, short>(ref MemoryMarshal.GetArrayDataReference(array)), endIndex),
                        Unsafe.As<T, short>(ref value),
                        count);

                    return (result >= 0 ? endIndex : 0) + result;
                }
                else if (sizeof(T) == sizeof(int))
                {
                    int endIndex = startIndex - count + 1;
                    int result = SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.Add(ref Unsafe.As<T, int>(ref MemoryMarshal.GetArrayDataReference(array)), endIndex),
                        Unsafe.As<T, int>(ref value),
                        count);

                    return (result >= 0 ? endIndex : 0) + result;
                }
                else if (sizeof(T) == sizeof(long))
                {
                    int endIndex = startIndex - count + 1;
                    int result = SpanHelpers.LastIndexOfValueType(
                        ref Unsafe.Add(ref Unsafe.As<T, long>(ref MemoryMarshal.GetArrayDataReference(array)), endIndex),
                        Unsafe.As<T, long>(ref value),
                        count);

                    return (result >= 0 ? endIndex : 0) + result;
                }
            }

#if !NATIVEAOT
            return EqualityComparer<T>.Default.LastIndexOf(array, value, startIndex, count);
#else
            return LastIndexOfImpl(array, value, startIndex, count);
#endif
        }

        // Reverses all elements of the given array. Following a call to this
        // method, an element previously located at index i will now be
        // located at index length - i - 1, where length is the
        // length of the array.
        //
        public static void Reverse(Array array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            Reverse(array, array.GetLowerBound(0), array.Length);
        }

        // Reverses the elements in a range of an array. Following a call to this
        // method, an element in the range given by index and count
        // which was previously located at index i will now be located at
        // index index + (index + count - i - 1).
        // Reliability note: This may fail because it may have to box objects.
        //
        public static void Reverse(Array array, int index, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            int lowerBound = array.GetLowerBound(0);
            if (index < lowerBound)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();

            if (array.Length - (index - lowerBound) < length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);
            if (array.Rank != 1)
                ThrowHelper.ThrowRankException(ExceptionResource.Rank_MultiDimNotSupported);

            if (length <= 1)
                return;

            int adjustedIndex = index - lowerBound;
            switch (array.GetCorElementTypeOfElementType())
            {
                case CorElementType.ELEMENT_TYPE_I1:
                case CorElementType.ELEMENT_TYPE_U1:
                case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    UnsafeArrayAsSpan<byte>(array, adjustedIndex, length).Reverse();
                    return;
                case CorElementType.ELEMENT_TYPE_I2:
                case CorElementType.ELEMENT_TYPE_U2:
                case CorElementType.ELEMENT_TYPE_CHAR:
                    UnsafeArrayAsSpan<short>(array, adjustedIndex, length).Reverse();
                    return;
                case CorElementType.ELEMENT_TYPE_I4:
                case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R4:
                    UnsafeArrayAsSpan<int>(array, adjustedIndex, length).Reverse();
                    return;
                case CorElementType.ELEMENT_TYPE_I8:
                case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                case CorElementType.ELEMENT_TYPE_I:
                case CorElementType.ELEMENT_TYPE_U:
#endif
                case CorElementType.ELEMENT_TYPE_R8:
                    UnsafeArrayAsSpan<long>(array, adjustedIndex, length).Reverse();
                    return;
                case CorElementType.ELEMENT_TYPE_OBJECT:
                case CorElementType.ELEMENT_TYPE_ARRAY:
                case CorElementType.ELEMENT_TYPE_SZARRAY:
                    UnsafeArrayAsSpan<object>(array, adjustedIndex, length).Reverse();
                    return;
            }

            int i = index;
            int j = index + length - 1;
            while (i < j)
            {
                object? temp = array.GetValue(i);
                array.SetValue(array.GetValue(j), i);
                array.SetValue(temp, j);
                i++;
                j--;
            }
        }

        public static void Reverse<T>(T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (array.Length > 1)
                SpanHelpers.Reverse(ref MemoryMarshal.GetArrayDataReference(array), (nuint)array.Length);
        }

        public static void Reverse<T>(T[] array, int index, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (index < 0)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            if (array.Length - index < length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            if (length <= 1)
                return;

            SpanHelpers.Reverse(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index), (nuint)length);
        }

        // Sorts the elements of an array. The sort compares the elements to each
        // other using the IComparable interface, which must be implemented
        // by all elements of the array.
        //
        public static void Sort(Array array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            Sort(array, null, array.GetLowerBound(0), array.Length, null);
        }

        // Sorts the elements of two arrays based on the keys in the first array.
        // Elements in the keys array specify the sort keys for
        // corresponding elements in the items array. The sort compares the
        // keys to each other using the IComparable interface, which must be
        // implemented by all elements of the keys array.
        //
        public static void Sort(Array keys, Array? items)
        {
            if (keys == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
            Sort(keys, items, keys.GetLowerBound(0), keys.Length, null);
        }

        // Sorts the elements in a section of an array. The sort compares the
        // elements to each other using the IComparable interface, which
        // must be implemented by all elements in the given section of the array.
        //
        public static void Sort(Array array, int index, int length)
        {
            Sort(array, null, index, length, null);
        }

        // Sorts the elements in a section of two arrays based on the keys in the
        // first array. Elements in the keys array specify the sort keys for
        // corresponding elements in the items array. The sort compares the
        // keys to each other using the IComparable interface, which must be
        // implemented by all elements of the keys array.
        //
        public static void Sort(Array keys, Array? items, int index, int length)
        {
            Sort(keys, items, index, length, null);
        }

        // Sorts the elements of an array. The sort compares the elements to each
        // other using the given IComparer interface. If comparer is
        // null, the elements are compared to each other using the
        // IComparable interface, which in that case must be implemented by
        // all elements of the array.
        //
        public static void Sort(Array array, IComparer? comparer)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            Sort(array, null, array.GetLowerBound(0), array.Length, comparer);
        }

        // Sorts the elements of two arrays based on the keys in the first array.
        // Elements in the keys array specify the sort keys for
        // corresponding elements in the items array. The sort compares the
        // keys to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented
        // by all elements of the keys array.
        //
        public static void Sort(Array keys, Array? items, IComparer? comparer)
        {
            if (keys == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
            Sort(keys, items, keys.GetLowerBound(0), keys.Length, comparer);
        }

        // Sorts the elements in a section of an array. The sort compares the
        // elements to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented
        // by all elements in the given section of the array.
        //
        public static void Sort(Array array, int index, int length, IComparer? comparer)
        {
            Sort(array, null, index, length, comparer);
        }

        // Sorts the elements in a section of two arrays based on the keys in the
        // first array. Elements in the keys array specify the sort keys for
        // corresponding elements in the items array. The sort compares the
        // keys to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented
        // by all elements of the given section of the keys array.
        //
        public static void Sort(Array keys, Array? items, int index, int length, IComparer? comparer)
        {
            if (keys == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
            if (keys.Rank != 1 || (items != null && items.Rank != 1))
                ThrowHelper.ThrowRankException(ExceptionResource.Rank_MultiDimNotSupported);
            int keysLowerBound = keys.GetLowerBound(0);
            if (items != null && keysLowerBound != items.GetLowerBound(0))
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_LowerBoundsMustMatch);
            if (index < keysLowerBound)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();

            if (keys.Length - (index - keysLowerBound) < length || (items != null && (index - keysLowerBound) > items.Length - length))
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            if (length <= 1)
                return;

            comparer ??= Comparer.Default;

            if (keys is object[] objKeys)
            {
                object[]? objItems = items as object[];
                if (items == null || objItems != null)
                {
                    new SorterObjectArray(objKeys, objItems, comparer).Sort(index, length);
                    return;
                }
            }

            if (comparer == Comparer.Default)
            {
                CorElementType et = keys.GetCorElementTypeOfElementType();
                if (items == null || items.GetCorElementTypeOfElementType() == et)
                {
                    int adjustedIndex = index - keysLowerBound;
                    switch (et)
                    {
                        case CorElementType.ELEMENT_TYPE_I1:
                            GenericSort<sbyte>(keys, items, adjustedIndex, length);
                            return;
                        case CorElementType.ELEMENT_TYPE_U1:
                        case CorElementType.ELEMENT_TYPE_BOOLEAN:
                            GenericSort<byte>(keys, items, adjustedIndex, length);
                            return;
                        case CorElementType.ELEMENT_TYPE_I2:
                            GenericSort<short>(keys, items, adjustedIndex, length);
                            return;
                        case CorElementType.ELEMENT_TYPE_U2:
                        case CorElementType.ELEMENT_TYPE_CHAR:
                            GenericSort<ushort>(keys, items, adjustedIndex, length);
                            return;
                        case CorElementType.ELEMENT_TYPE_I4:
#if TARGET_32BIT
                        case CorElementType.ELEMENT_TYPE_I:
#endif
                            GenericSort<int>(keys, items, adjustedIndex, length);
                            return;
                        case CorElementType.ELEMENT_TYPE_U4:
#if TARGET_32BIT
                        case CorElementType.ELEMENT_TYPE_U:
#endif
                            GenericSort<uint>(keys, items, adjustedIndex, length);
                            return;
                        case CorElementType.ELEMENT_TYPE_I8:
#if TARGET_64BIT
                        case CorElementType.ELEMENT_TYPE_I:
#endif
                            GenericSort<long>(keys, items, adjustedIndex, length);
                            return;
                        case CorElementType.ELEMENT_TYPE_U8:
#if TARGET_64BIT
                        case CorElementType.ELEMENT_TYPE_U:
#endif
                            GenericSort<ulong>(keys, items, adjustedIndex, length);
                            return;
                        case CorElementType.ELEMENT_TYPE_R4:
                            GenericSort<float>(keys, items, adjustedIndex, length);
                            return;
                        case CorElementType.ELEMENT_TYPE_R8:
                            GenericSort<double>(keys, items, adjustedIndex, length);
                            return;
                    }

                    static void GenericSort<T>(Array keys, Array? items, int adjustedIndex, int length) where T : struct
                    {
                        Span<T> keysSpan = UnsafeArrayAsSpan<T>(keys, adjustedIndex, length);
                        if (items != null)
                        {
                            keysSpan.Sort<T, T>(UnsafeArrayAsSpan<T>(items, adjustedIndex, length));
                        }
                        else
                        {
                            keysSpan.Sort<T>();
                        }
                    }
                }
            }

            new SorterGenericArray(keys, items, comparer).Sort(index, length);
        }

        public static void Sort<T>(T[] array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            if (array.Length > 1)
            {
                var span = new Span<T>(ref MemoryMarshal.GetArrayDataReference(array), array.Length);
                ArraySortHelper<T>.Default.Sort(span, null);
            }
        }

        public static void Sort<TKey, TValue>(TKey[] keys, TValue[]? items)
        {
            if (keys == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
            Sort(keys, items, 0, keys.Length, null);
        }

        public static void Sort<T>(T[] array, int index, int length)
        {
            Sort(array, index, length, null);
        }

        public static void Sort<TKey, TValue>(TKey[] keys, TValue[]? items, int index, int length)
        {
            Sort(keys, items, index, length, null);
        }

        public static void Sort<T>(T[] array, IComparer<T>? comparer)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            Sort(array, 0, array.Length, comparer);
        }

        public static void Sort<TKey, TValue>(TKey[] keys, TValue[]? items, IComparer<TKey>? comparer)
        {
            if (keys == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
            Sort(keys, items, 0, keys.Length, comparer);
        }

        public static void Sort<T>(T[] array, int index, int length, IComparer<T>? comparer)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (index < 0)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            if (array.Length - index < length)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            if (length > 1)
            {
                var span = new Span<T>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index), length);
                ArraySortHelper<T>.Default.Sort(span, comparer);
            }
        }

        public static void Sort<TKey, TValue>(TKey[] keys, TValue[]? items, int index, int length, IComparer<TKey>? comparer)
        {
            if (keys == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keys);
            if (index < 0)
                ThrowHelper.ThrowIndexArgumentOutOfRange_NeedNonNegNumException();
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();
            if (keys.Length - index < length || (items != null && index > items.Length - length))
                ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidOffLen);

            if (length > 1)
            {
                if (items == null)
                {
                    Sort(keys, index, length, comparer);
                    return;
                }

                var spanKeys = new Span<TKey>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(keys), index), length);
                var spanItems = new Span<TValue>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(items), index), length);
                ArraySortHelper<TKey, TValue>.Default.Sort(spanKeys, spanItems, comparer);
            }
        }

        public static void Sort<T>(T[] array, Comparison<T> comparison)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (comparison == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.comparison);
            }

            var span = new Span<T>(ref MemoryMarshal.GetArrayDataReference(array), array.Length);
            ArraySortHelper<T>.Sort(span, comparison);
        }

        public static bool TrueForAll<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            for (int i = 0; i < array.Length; i++)
            {
                if (!match(array[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Gets the maximum number of elements that may be contained in an array.</summary>
        /// <returns>The maximum count of elements allowed in any array.</returns>
        /// <remarks>
        /// <para>This property represents a runtime limitation, the maximum number of elements (not bytes)
        /// the runtime will allow in an array. There is no guarantee that an allocation under this length
        /// will succeed, but all attempts to allocate a larger array will fail.</para>
        /// <para>This property only applies to single-dimension, zero-bound (SZ) arrays.
        /// <see cref="Length"/> property may return larger value than this property for multi-dimensional arrays.</para>
        /// </remarks>
        public static int MaxLength =>
            // Keep in sync with `inline SIZE_T MaxArrayLength()` from gchelpers and HashHelpers.MaxPrimeArrayLength.
            0X7FFFFFC7;

        // Private value type used by the Sort methods.
        private readonly struct SorterObjectArray
        {
            private readonly object[] keys;
            private readonly object?[]? items;
            private readonly IComparer comparer;

            internal SorterObjectArray(object[] keys, object?[]? items, IComparer comparer)
            {
                this.keys = keys;
                this.items = items;
                this.comparer = comparer;
            }

            internal void SwapIfGreater(int a, int b)
            {
                if (a != b)
                {
                    if (comparer.Compare(keys[a], keys[b]) > 0)
                    {
                        object temp = keys[a];
                        keys[a] = keys[b];
                        keys[b] = temp;
                        if (items != null)
                        {
                            object? item = items[a];
                            items[a] = items[b];
                            items[b] = item;
                        }
                    }
                }
            }

            private void Swap(int i, int j)
            {
                object t = keys[i];
                keys[i] = keys[j];
                keys[j] = t;

                if (items != null)
                {
                    object? item = items[i];
                    items[i] = items[j];
                    items[j] = item;
                }
            }

            internal void Sort(int left, int length)
            {
                IntrospectiveSort(left, length);
            }

            private void IntrospectiveSort(int left, int length)
            {
                if (length < 2)
                    return;

                try
                {
                    IntroSort(left, length + left - 1, 2 * (BitOperations.Log2((uint)length) + 1));
                }
                catch (IndexOutOfRangeException)
                {
                    ThrowHelper.ThrowArgumentException_BadComparer(comparer);
                }
                catch (Exception e)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
                }
            }

            // IntroSort is recursive; block it from being inlined into itself as
            // this is currenly not profitable.
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void IntroSort(int lo, int hi, int depthLimit)
            {
                Debug.Assert(hi >= lo);
                Debug.Assert(depthLimit >= 0);

                while (hi > lo)
                {
                    int partitionSize = hi - lo + 1;
                    if (partitionSize <= IntrosortSizeThreshold)
                    {
                        Debug.Assert(partitionSize >= 2);

                        if (partitionSize == 2)
                        {
                            SwapIfGreater(lo, hi);
                            return;
                        }

                        if (partitionSize == 3)
                        {
                            SwapIfGreater(lo, hi - 1);
                            SwapIfGreater(lo, hi);
                            SwapIfGreater(hi - 1, hi);
                            return;
                        }

                        InsertionSort(lo, hi);
                        return;
                    }

                    if (depthLimit == 0)
                    {
                        Heapsort(lo, hi);
                        return;
                    }
                    depthLimit--;

                    int p = PickPivotAndPartition(lo, hi);
                    IntroSort(p + 1, hi, depthLimit);
                    hi = p - 1;
                }
            }

            private int PickPivotAndPartition(int lo, int hi)
            {
                Debug.Assert(hi - lo >= IntrosortSizeThreshold);

                // Compute median-of-three.  But also partition them, since we've done the comparison.
                int mid = lo + (hi - lo) / 2;

                // Sort lo, mid and hi appropriately, then pick mid as the pivot.
                SwapIfGreater(lo, mid);
                SwapIfGreater(lo, hi);
                SwapIfGreater(mid, hi);

                object pivot = keys[mid];
                Swap(mid, hi - 1);
                int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

                while (left < right)
                {
                    while (comparer.Compare(keys[++left], pivot) < 0) ;
                    while (comparer.Compare(pivot, keys[--right]) < 0) ;

                    if (left >= right)
                        break;

                    Swap(left, right);
                }

                // Put pivot in the right location.
                if (left != hi - 1)
                {
                    Swap(left, hi - 1);
                }
                return left;
            }

            private void Heapsort(int lo, int hi)
            {
                int n = hi - lo + 1;
                for (int i = n / 2; i >= 1; i--)
                {
                    DownHeap(i, n, lo);
                }
                for (int i = n; i > 1; i--)
                {
                    Swap(lo, lo + i - 1);

                    DownHeap(1, i - 1, lo);
                }
            }

            private void DownHeap(int i, int n, int lo)
            {
                object d = keys[lo + i - 1];
                object? dt = items?[lo + i - 1];
                int child;
                while (i <= n / 2)
                {
                    child = 2 * i;
                    if (child < n && comparer.Compare(keys[lo + child - 1], keys[lo + child]) < 0)
                    {
                        child++;
                    }
                    if (!(comparer.Compare(d, keys[lo + child - 1]) < 0))
                        break;
                    keys[lo + i - 1] = keys[lo + child - 1];
                    if (items != null)
                        items[lo + i - 1] = items[lo + child - 1];
                    i = child;
                }
                keys[lo + i - 1] = d;
                if (items != null)
                    items[lo + i - 1] = dt;
            }

            private void InsertionSort(int lo, int hi)
            {
                int i, j;
                object t;
                object? ti;
                for (i = lo; i < hi; i++)
                {
                    j = i;
                    t = keys[i + 1];
                    ti = items?[i + 1];
                    while (j >= lo && comparer.Compare(t, keys[j]) < 0)
                    {
                        keys[j + 1] = keys[j];
                        if (items != null)
                            items[j + 1] = items[j];
                        j--;
                    }
                    keys[j + 1] = t;
                    if (items != null)
                        items[j + 1] = ti;
                }
            }
        }

        // Private value used by the Sort methods for instances of Array.
        // This is slower than the one for Object[], since we can't use the JIT helpers
        // to access the elements.  We must use GetValue & SetValue.
        private readonly struct SorterGenericArray
        {
            private readonly Array keys;
            private readonly Array? items;
            private readonly IComparer comparer;

            internal SorterGenericArray(Array keys, Array? items, IComparer comparer)
            {
                this.keys = keys;
                this.items = items;
                this.comparer = comparer;
            }

            internal void SwapIfGreater(int a, int b)
            {
                if (a != b)
                {
                    if (comparer.Compare(keys.GetValue(a), keys.GetValue(b)) > 0)
                    {
                        object? key = keys.GetValue(a);
                        keys.SetValue(keys.GetValue(b), a);
                        keys.SetValue(key, b);
                        if (items != null)
                        {
                            object? item = items.GetValue(a);
                            items.SetValue(items.GetValue(b), a);
                            items.SetValue(item, b);
                        }
                    }
                }
            }

            private void Swap(int i, int j)
            {
                object? t1 = keys.GetValue(i);
                keys.SetValue(keys.GetValue(j), i);
                keys.SetValue(t1, j);

                if (items != null)
                {
                    object? t2 = items.GetValue(i);
                    items.SetValue(items.GetValue(j), i);
                    items.SetValue(t2, j);
                }
            }

            internal void Sort(int left, int length)
            {
                IntrospectiveSort(left, length);
            }

            private void IntrospectiveSort(int left, int length)
            {
                if (length < 2)
                    return;

                try
                {
                    IntroSort(left, length + left - 1, 2 * (BitOperations.Log2((uint)length) + 1));
                }
                catch (IndexOutOfRangeException)
                {
                    ThrowHelper.ThrowArgumentException_BadComparer(comparer);
                }
                catch (Exception e)
                {
                    ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IComparerFailed, e);
                }
            }

            // IntroSort is recursive; block it from being inlined into itself as
            // this is currenly not profitable.
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void IntroSort(int lo, int hi, int depthLimit)
            {
                Debug.Assert(hi >= lo);
                Debug.Assert(depthLimit >= 0);

                while (hi > lo)
                {
                    int partitionSize = hi - lo + 1;
                    if (partitionSize <= IntrosortSizeThreshold)
                    {
                        Debug.Assert(partitionSize >= 2);

                        if (partitionSize == 2)
                        {
                            SwapIfGreater(lo, hi);
                            return;
                        }

                        if (partitionSize == 3)
                        {
                            SwapIfGreater(lo, hi - 1);
                            SwapIfGreater(lo, hi);
                            SwapIfGreater(hi - 1, hi);
                            return;
                        }

                        InsertionSort(lo, hi);
                        return;
                    }

                    if (depthLimit == 0)
                    {
                        Heapsort(lo, hi);
                        return;
                    }
                    depthLimit--;

                    int p = PickPivotAndPartition(lo, hi);
                    IntroSort(p + 1, hi, depthLimit);
                    hi = p - 1;
                }
            }

            private int PickPivotAndPartition(int lo, int hi)
            {
                Debug.Assert(hi - lo >= IntrosortSizeThreshold);

                // Compute median-of-three.  But also partition them, since we've done the comparison.
                int mid = lo + (hi - lo) / 2;

                SwapIfGreater(lo, mid);
                SwapIfGreater(lo, hi);
                SwapIfGreater(mid, hi);

                object? pivot = keys.GetValue(mid);
                Swap(mid, hi - 1);
                int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

                while (left < right)
                {
                    while (comparer.Compare(keys.GetValue(++left), pivot) < 0) ;
                    while (comparer.Compare(pivot, keys.GetValue(--right)) < 0) ;

                    if (left >= right)
                        break;

                    Swap(left, right);
                }

                // Put pivot in the right location.
                if (left != hi - 1)
                {
                    Swap(left, hi - 1);
                }
                return left;
            }

            private void Heapsort(int lo, int hi)
            {
                int n = hi - lo + 1;
                for (int i = n / 2; i >= 1; i--)
                {
                    DownHeap(i, n, lo);
                }
                for (int i = n; i > 1; i--)
                {
                    Swap(lo, lo + i - 1);

                    DownHeap(1, i - 1, lo);
                }
            }

            private void DownHeap(int i, int n, int lo)
            {
                object? d = keys.GetValue(lo + i - 1);
                object? dt = items?.GetValue(lo + i - 1);
                int child;
                while (i <= n / 2)
                {
                    child = 2 * i;
                    if (child < n && comparer.Compare(keys.GetValue(lo + child - 1), keys.GetValue(lo + child)) < 0)
                    {
                        child++;
                    }

                    if (!(comparer.Compare(d, keys.GetValue(lo + child - 1)) < 0))
                        break;

                    keys.SetValue(keys.GetValue(lo + child - 1), lo + i - 1);
                    items?.SetValue(items.GetValue(lo + child - 1), lo + i - 1);
                    i = child;
                }
                keys.SetValue(d, lo + i - 1);
                items?.SetValue(dt, lo + i - 1);
            }

            private void InsertionSort(int lo, int hi)
            {
                int i, j;
                object? t;
                object? dt;
                for (i = lo; i < hi; i++)
                {
                    j = i;
                    t = keys.GetValue(i + 1);
                    dt = items?.GetValue(i + 1);

                    while (j >= lo && comparer.Compare(t, keys.GetValue(j)) < 0)
                    {
                        keys.SetValue(keys.GetValue(j), j + 1);
                        items?.SetValue(items.GetValue(j), j + 1);
                        j--;
                    }

                    keys.SetValue(t, j + 1);
                    items?.SetValue(dt, j + 1);
                }
            }
        }

        private static Span<T> UnsafeArrayAsSpan<T>(Array array, int adjustedIndex, int length) =>
            new Span<T>(ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array)), array.Length).Slice(adjustedIndex, length);

        public IEnumerator GetEnumerator()
        {
            return new ArrayEnumerator(this);
        }
    }
}
