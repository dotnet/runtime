// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Reflection;
using Internal.Runtime.CompilerServices;

namespace System
{
    // Note that we make a T[] (single-dimensional w/ zero as the lower bound) implement both
    // IList<U> and IReadOnlyList<U>, where T : U dynamically.  See the SZArrayHelper class for details.
    public abstract partial class Array : ICloneable, IList, IStructuralComparable, IStructuralEquatable
    {
        // Create instance will create an array
        public static unsafe Array CreateInstance(Type elementType, int length)
        {
            if (elementType is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
            if (length < 0)
                ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum();

            RuntimeType? t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);
            return InternalCreate((void*)t.TypeHandle.Value, 1, &length, null);
        }

        public static unsafe Array CreateInstance(Type elementType, int length1, int length2)
        {
            if (elementType is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
            if (length1 < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length1, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (length2 < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length2, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            RuntimeType? t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);
            int* pLengths = stackalloc int[2];
            pLengths[0] = length1;
            pLengths[1] = length2;
            return InternalCreate((void*)t.TypeHandle.Value, 2, pLengths, null);
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

            RuntimeType? t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);
            int* pLengths = stackalloc int[3];
            pLengths[0] = length1;
            pLengths[1] = length2;
            pLengths[2] = length3;
            return InternalCreate((void*)t.TypeHandle.Value, 3, pLengths, null);
        }

        public static unsafe Array CreateInstance(Type elementType, params int[] lengths)
        {
            if (elementType is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
            if (lengths == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lengths);
            if (lengths.Length == 0)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NeedAtLeast1Rank);

            RuntimeType? t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            // Check to make sure the lengths are all positive. Note that we check this here to give
            // a good exception message if they are not; however we check this again inside the execution
            // engine's low level allocation function after having made a copy of the array to prevent a
            // malicious caller from mutating the array after this check.
            for (int i = 0; i < lengths.Length; i++)
                if (lengths[i] < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            fixed (int* pLengths = &lengths[0])
                return InternalCreate((void*)t.TypeHandle.Value, lengths.Length, pLengths, null);
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

            RuntimeType? t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

            // Check to make sure the lenghts are all positive. Note that we check this here to give
            // a good exception message if they are not; however we check this again inside the execution
            // engine's low level allocation function after having made a copy of the array to prevent a
            // malicious caller from mutating the array after this check.
            for (int i = 0; i < lengths.Length; i++)
                if (lengths[i] < 0)
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            fixed (int* pLengths = &lengths[0])
            fixed (int* pLowerBounds = &lowerBounds[0])
                return InternalCreate((void*)t.TypeHandle.Value, lengths.Length, pLengths, pLowerBounds);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern unsafe Array InternalCreate(void* elementType, int rank, int* pLengths, int* pLowerBounds);

        // Copies length elements from sourceArray, starting at index 0, to
        // destinationArray, starting at index 0.
        //
        public static unsafe void Copy(Array sourceArray, Array destinationArray, int length)
        {
            if (sourceArray == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.sourceArray);
            if (destinationArray == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.destinationArray);

            MethodTable* pMT = RuntimeHelpers.GetMethodTable(sourceArray);
            if (pMT == RuntimeHelpers.GetMethodTable(destinationArray) &&
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
            Copy(sourceArray, sourceArray.GetLowerBound(0), destinationArray, destinationArray.GetLowerBound(0), length, reliable: false);
        }

        // Copies length elements from sourceArray, starting at sourceIndex, to
        // destinationArray, starting at destinationIndex.
        //
        public static unsafe void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            if (sourceArray != null && destinationArray != null)
            {
                MethodTable* pMT = RuntimeHelpers.GetMethodTable(sourceArray);
                if (pMT == RuntimeHelpers.GetMethodTable(destinationArray) &&
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
            Copy(sourceArray!, sourceIndex, destinationArray!, destinationIndex, length, reliable: false);
        }

        private static unsafe void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
        {
            if (sourceArray == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.sourceArray);
            if (destinationArray == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.destinationArray);

            if (sourceArray.GetType() != destinationArray.GetType() && sourceArray.Rank != destinationArray.Rank)
                throw new RankException(SR.Rank_MustMatch);

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), SR.ArgumentOutOfRange_NeedNonNegNum);

            int srcLB = sourceArray.GetLowerBound(0);
            if (sourceIndex < srcLB || sourceIndex - srcLB < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), SR.ArgumentOutOfRange_ArrayLB);
            sourceIndex -= srcLB;

            int dstLB = destinationArray.GetLowerBound(0);
            if (destinationIndex < dstLB || destinationIndex - dstLB < 0)
                throw new ArgumentOutOfRangeException(nameof(destinationIndex), SR.ArgumentOutOfRange_ArrayLB);
            destinationIndex -= dstLB;

            if ((uint)(sourceIndex + length) > sourceArray.NativeLength)
                throw new ArgumentException(SR.Arg_LongerThanSrcArray, nameof(sourceArray));
            if ((uint)(destinationIndex + length) > destinationArray.NativeLength)
                throw new ArgumentException(SR.Arg_LongerThanDestArray, nameof(destinationArray));

            if (sourceArray.GetType() == destinationArray.GetType() || IsSimpleCopy(sourceArray, destinationArray))
            {
                MethodTable* pMT = RuntimeHelpers.GetMethodTable(sourceArray);

                nuint elementSize = (nuint)pMT->ComponentSize;
                nuint byteCount = (uint)length * elementSize;
                ref byte src = ref Unsafe.AddByteOffset(ref sourceArray.GetRawArrayData(), (uint)sourceIndex * elementSize);
                ref byte dst = ref Unsafe.AddByteOffset(ref destinationArray.GetRawArrayData(), (uint)destinationIndex * elementSize);

                if (pMT->ContainsGCPointers)
                    Buffer.BulkMoveWithWriteBarrier(ref dst, ref src, byteCount);
                else
                    Buffer.Memmove(ref dst, ref src, byteCount);

                // GC.KeepAlive(sourceArray) not required. pMT kept alive via sourceArray
                return;
            }

            // If we were called from Array.ConstrainedCopy, ensure that the array copy
            // is guaranteed to succeed.
            if (reliable)
                throw new ArrayTypeMismatchException(SR.ArrayTypeMismatch_ConstrainedCopy);

            // Rare
            CopySlow(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool IsSimpleCopy(Array sourceArray, Array destinationArray);

        // Reliability-wise, this method will either possibly corrupt your
        // instance & might fail when called from within a CER, or if the
        // reliable flag is true, it will either always succeed or always
        // throw an exception with no side effects.
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void CopySlow(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length);

        // Provides a strong exception guarantee - either it succeeds, or
        // it throws an exception with no side effects.  The arrays must be
        // compatible array types based on the array element type - this
        // method does not support casting, boxing, or primitive widening.
        // It will up-cast, assuming the array types are correct.
        public static void ConstrainedCopy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length, reliable: true);
        }

        /// <summary>
        /// Clears the contents of an array.
        /// </summary>
        /// <param name="array">The array to clear.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        public static unsafe void Clear(Array array)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            MethodTable* pMT = RuntimeHelpers.GetMethodTable(array);
            nuint totalByteLength = pMT->ComponentSize * array.NativeLength;
            ref byte pStart = ref array.GetRawArrayData();

            if (!pMT->ContainsGCPointers)
            {
                SpanHelpers.ClearWithoutReferences(ref pStart, totalByteLength);
            }
            else
            {
                Debug.Assert(totalByteLength % (nuint)sizeof(IntPtr) == 0);
                SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref pStart), totalByteLength / (nuint)sizeof(IntPtr));
            }

            // GC.KeepAlive(array) not required. pMT kept alive via `pStart`
        }

        // Sets length elements in array to 0 (or null for Object arrays), starting
        // at index.
        //
        public static unsafe void Clear(Array array, int index, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            ref byte p = ref Unsafe.As<RawArrayData>(array).Data;
            int lowerBound = 0;

            MethodTable* pMT = RuntimeHelpers.GetMethodTable(array);
            if (pMT->IsMultiDimensionalArray)
            {
                int rank = pMT->MultiDimensionalArrayRank;
                lowerBound = Unsafe.Add(ref Unsafe.As<byte, int>(ref p), rank);
                p = ref Unsafe.Add(ref p, 2 * sizeof(int) * rank); // skip the bounds
            }

            int offset = index - lowerBound;

            if (index < lowerBound || offset < 0 || length < 0 || (uint)(offset + length) > array.NativeLength)
                ThrowHelper.ThrowIndexOutOfRangeException();

            nuint elementSize = pMT->ComponentSize;

            ref byte ptr = ref Unsafe.AddByteOffset(ref p, (uint)offset * elementSize);
            nuint byteLength = (uint)length * elementSize;

            if (pMT->ContainsGCPointers)
                SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref ptr), byteLength / (uint)sizeof(IntPtr));
            else
                SpanHelpers.ClearWithoutReferences(ref ptr, byteLength);

            // GC.KeepAlive(array) not required. pMT kept alive via `ptr`
        }

        private unsafe nint GetFlattenedIndex(ReadOnlySpan<int> indices)
        {
            // Checked by the caller
            Debug.Assert(indices.Length == Rank);

            if (RuntimeHelpers.GetMethodTable(this)->IsMultiDimensionalArray)
            {
                ref int bounds = ref RuntimeHelpers.GetMultiDimensionalArrayBounds(this);
                nint flattenedIndex = 0;
                for (int i = 0; i < indices.Length; i++)
                {
                    int index = indices[i] - Unsafe.Add(ref bounds, indices.Length  + i);
                    int length = Unsafe.Add(ref bounds, i);
                    if ((uint)index >= (uint)length)
                        ThrowHelper.ThrowIndexOutOfRangeException();
                    flattenedIndex = (length * flattenedIndex) + index;
                }
                Debug.Assert((nuint)flattenedIndex < (nuint)LongLength);
                return flattenedIndex;
            }
            else
            {
                int index = indices[0];
                if ((uint)index >= (uint)LongLength)
                    ThrowHelper.ThrowIndexOutOfRangeException();
                return index;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern unsafe object? InternalGetValue(nint flattenedIndex);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern void InternalSetValue(object? value, nint flattenedIndex);

        public int Length => checked((int)Unsafe.As<RawArrayData>(this).Length);

        // This could return a length greater than int.MaxValue
        internal nuint NativeLength => Unsafe.As<RawArrayData>(this).Length;

        public long LongLength => (long)NativeLength;

        public unsafe int Rank
        {
            get
            {
                int rank = RuntimeHelpers.GetMultiDimensionalArrayRank(this);
                return (rank != 0) ? rank : 1;
            }
        }

        public unsafe int GetLength(int dimension)
        {
            int rank = RuntimeHelpers.GetMultiDimensionalArrayRank(this);
            if (rank == 0 && dimension == 0)
                return Length;

            if ((uint)dimension >= (uint)rank)
                throw new IndexOutOfRangeException(SR.IndexOutOfRange_ArrayRankIndex);

            return Unsafe.Add(ref RuntimeHelpers.GetMultiDimensionalArrayBounds(this), dimension);
        }

        public unsafe int GetUpperBound(int dimension)
        {
            int rank = RuntimeHelpers.GetMultiDimensionalArrayRank(this);
            if (rank == 0 && dimension == 0)
                return Length - 1;

            if ((uint)dimension >= (uint)rank)
                throw new IndexOutOfRangeException(SR.IndexOutOfRange_ArrayRankIndex);

            ref int bounds = ref RuntimeHelpers.GetMultiDimensionalArrayBounds(this);
            return Unsafe.Add(ref bounds, dimension) + Unsafe.Add(ref bounds, rank + dimension) - 1;
        }

        public unsafe int GetLowerBound(int dimension)
        {
            int rank = RuntimeHelpers.GetMultiDimensionalArrayRank(this);
            if (rank == 0 && dimension == 0)
                return 0;

            if ((uint)dimension >= (uint)rank)
                throw new IndexOutOfRangeException(SR.IndexOutOfRange_ArrayRankIndex);

            return Unsafe.Add(ref RuntimeHelpers.GetMultiDimensionalArrayBounds(this), rank + dimension);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern CorElementType GetCorElementTypeOfElementType();

        private unsafe bool IsValueOfElementType(object value)
        {
            MethodTable* thisMT = RuntimeHelpers.GetMethodTable(this);
            return (IntPtr)thisMT->ElementType == (IntPtr)RuntimeHelpers.GetMethodTable(value);
        }

        // if this is an array of value classes and that value class has a default constructor
        // then this calls this default constructor on every element in the value class array.
        // otherwise this is a no-op.  Generally this method is called automatically by the compiler
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern void Initialize();
    }

#pragma warning disable CA1822 // Mark members as static
    //----------------------------------------------------------------------------------------
    // ! READ THIS BEFORE YOU WORK ON THIS CLASS.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not SZArrayHelper objects. Rather, they are of type U[]
    // where U[] is castable to T[]. No actual SZArrayHelper object is ever instantiated. Thus, you will
    // see a lot of expressions that cast "this" "T[]".
    //
    // This class is needed to allow an SZ array of type T[] to expose IList<T>,
    // IList<T.BaseType>, etc., etc. all the way up to IList<Object>. When the following call is
    // made:
    //
    //   ((IList<T>) (new U[n])).SomeIListMethod()
    //
    // the interface stub dispatcher treats this as a special case, loads up SZArrayHelper,
    // finds the corresponding generic method (matched simply by method name), instantiates
    // it for type <T> and executes it.
    //
    // The "T" will reflect the interface used to invoke the method. The actual runtime "this" will be
    // array that is castable to "T[]" (i.e. for primitivs and valuetypes, it will be exactly
    // "T[]" - for orefs, it may be a "U[]" where U derives from T.)
    //----------------------------------------------------------------------------------------
    internal sealed class SZArrayHelper
    {
        // It is never legal to instantiate this class.
        private SZArrayHelper()
        {
            Debug.Fail("Hey! How'd I get here?");
        }

        internal IEnumerator<T> GetEnumerator<T>()
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] _this = Unsafe.As<T[]>(this);
            return _this.Length == 0 ? SZGenericArrayEnumerator<T>.Empty : new SZGenericArrayEnumerator<T>(_this);
        }

        private void CopyTo<T>(T[] array, int index)
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!

            T[] _this = Unsafe.As<T[]>(this);
            Array.Copy(_this, 0, array, index, _this.Length);
        }

        internal int get_Count<T>()
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] _this = Unsafe.As<T[]>(this);
            return _this.Length;
        }

        internal T get_Item<T>(int index)
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] _this = Unsafe.As<T[]>(this);
            if ((uint)index >= (uint)_this.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            }

            return _this[index];
        }

        internal void set_Item<T>(int index, T value)
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] _this = Unsafe.As<T[]>(this);
            if ((uint)index >= (uint)_this.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            }

            _this[index] = value;
        }

        private void Add<T>(T value)
        {
            // Not meaningful for arrays.
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        private bool Contains<T>(T value)
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] _this = Unsafe.As<T[]>(this);
            return Array.IndexOf(_this, value, 0, _this.Length) >= 0;
        }

        private bool get_IsReadOnly<T>()
        {
            return true;
        }

        private void Clear<T>()
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!

            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
        }

        private int IndexOf<T>(T value)
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] _this = Unsafe.As<T[]>(this);
            return Array.IndexOf(_this, value, 0, _this.Length);
        }

        private void Insert<T>(int index, T value)
        {
            // Not meaningful for arrays
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        private bool Remove<T>(T value)
        {
            // Not meaningful for arrays
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
            return default;
        }

        private void RemoveAt<T>(int index)
        {
            // Not meaningful for arrays
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }
    }
#pragma warning restore CA1822
}
