// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

#if BIT64
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

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

            RuntimeType? t = elementType!.UnderlyingSystemType as RuntimeType; // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);
            return InternalCreate((void*)t!.TypeHandle.Value, 1, &length, null); // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
        }

        public static unsafe Array CreateInstance(Type elementType, int length1, int length2)
        {
            if (elementType is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
            if (length1 < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length1, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
            if (length2 < 0)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.length2, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

            RuntimeType? t = elementType!.UnderlyingSystemType as RuntimeType; // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);
            int* pLengths = stackalloc int[2];
            pLengths[0] = length1;
            pLengths[1] = length2;
            return InternalCreate((void*)t!.TypeHandle.Value, 2, pLengths, null); // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
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

            RuntimeType? t = elementType!.UnderlyingSystemType as RuntimeType; // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
            if (t == null)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);
            int* pLengths = stackalloc int[3];
            pLengths[0] = length1;
            pLengths[1] = length2;
            pLengths[2] = length3;
            return InternalCreate((void*)t!.TypeHandle.Value, 3, pLengths, null); // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
        }

        public static unsafe Array CreateInstance(Type elementType, params int[] lengths)
        {
            if (elementType is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
            if (lengths == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lengths);
            if (lengths!.Length == 0) // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NeedAtLeast1Rank);

            RuntimeType? t = elementType!.UnderlyingSystemType as RuntimeType; // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
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
                return InternalCreate((void*)t!.TypeHandle.Value, lengths.Length, pLengths, null); // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
        }

        public static unsafe Array CreateInstance(Type elementType, int[] lengths, int[] lowerBounds)
        {
            if (elementType == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementType);
            if (lengths == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lengths);
            if (lowerBounds == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.lowerBounds);
            if (lengths!.Length != lowerBounds!.Length) // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RanksAndBounds);
            if (lengths.Length == 0)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_NeedAtLeast1Rank);

            RuntimeType? t = elementType!.UnderlyingSystemType as RuntimeType; // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
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
                return InternalCreate((void*)t!.TypeHandle.Value, lengths.Length, pLengths, pLowerBounds); // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe Array InternalCreate(void* elementType, int rank, int* pLengths, int* pLowerBounds);

        // Copies length elements from sourceArray, starting at index 0, to
        // destinationArray, starting at index 0.
        //
        public static void Copy(Array sourceArray, Array destinationArray, int length)
        {
            if (sourceArray == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.sourceArray);
            if (destinationArray == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.destinationArray);

            Copy(sourceArray!, sourceArray!.GetLowerBound(0), destinationArray!, destinationArray!.GetLowerBound(0), length, false); // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
        }

        // Copies length elements from sourceArray, starting at sourceIndex, to
        // destinationArray, starting at destinationIndex.
        //
        public static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length, false);
        }

        // Reliability-wise, this method will either possibly corrupt your 
        // instance & might fail when called from within a CER, or if the
        // reliable flag is true, it will either always succeed or always
        // throw an exception with no side effects.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable);

        // Provides a strong exception guarantee - either it succeeds, or
        // it throws an exception with no side effects.  The arrays must be
        // compatible array types based on the array element type - this 
        // method does not support casting, boxing, or primitive widening.
        // It will up-cast, assuming the array types are correct.
        public static void ConstrainedCopy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length, true);
        }

        // Sets length elements in array to 0 (or null for Object arrays), starting
        // at index.
        //
        public static unsafe void Clear(Array array, int index, int length)
        {
            if (array == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);

            ref byte p = ref GetRawArrayGeometry(array!, out uint numComponents, out uint elementSize, out int lowerBound, out bool containsGCPointers); // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected

            int offset = index - lowerBound;

            if (index < lowerBound || offset < 0 || length < 0 || (uint)(offset + length) > numComponents)
                ThrowHelper.ThrowIndexOutOfRangeException();

            ref byte ptr = ref Unsafe.AddByteOffset(ref p, (uint)offset * (nuint)elementSize);
            nuint byteLength = (uint)length * (nuint)elementSize;

            if (containsGCPointers)
                SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, IntPtr>(ref ptr), byteLength / (uint)sizeof(IntPtr));
            else
                SpanHelpers.ClearWithoutReferences(ref ptr, byteLength);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ref byte GetRawArrayGeometry(Array array, out uint numComponents, out uint elementSize, out int lowerBound, out bool containsGCPointers);

        // The various Get values...
        public unsafe object? GetValue(params int[] indices)
        {
            if (indices == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            if (Rank != indices!.Length) // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);

            TypedReference elemref = new TypedReference();
            fixed (int* pIndices = &indices[0])
                InternalGetReference(&elemref, indices.Length, pIndices);
            return TypedReference.InternalToObject(&elemref);
        }

        public unsafe object? GetValue(int index)
        {
            if (Rank != 1)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need1DArray);

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 1, &index);
            return TypedReference.InternalToObject(&elemref);
        }

        public unsafe object? GetValue(int index1, int index2)
        {
            if (Rank != 2)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need2DArray);

            int* pIndices = stackalloc int[2];
            pIndices[0] = index1;
            pIndices[1] = index2;

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 2, pIndices);
            return TypedReference.InternalToObject(&elemref);
        }

        public unsafe object? GetValue(int index1, int index2, int index3)
        {
            if (Rank != 3)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need3DArray);

            int* pIndices = stackalloc int[3];
            pIndices[0] = index1;
            pIndices[1] = index2;
            pIndices[2] = index3;

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 3, pIndices);
            return TypedReference.InternalToObject(&elemref);
        }

        public unsafe void SetValue(object? value, int index)
        {
            if (Rank != 1)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need1DArray);

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 1, &index);
            InternalSetValue(&elemref, value);
        }

        public unsafe void SetValue(object? value, int index1, int index2)
        {
            if (Rank != 2)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need2DArray);

            int* pIndices = stackalloc int[2];
            pIndices[0] = index1;
            pIndices[1] = index2;

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 2, pIndices);
            InternalSetValue(&elemref, value);
        }

        public unsafe void SetValue(object? value, int index1, int index2, int index3)
        {
            if (Rank != 3)
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_Need3DArray);

            int* pIndices = stackalloc int[3];
            pIndices[0] = index1;
            pIndices[1] = index2;
            pIndices[2] = index3;

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 3, pIndices);
            InternalSetValue(&elemref, value);
        }

        public unsafe void SetValue(object? value, params int[] indices)
        {
            if (indices == null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.indices);
            if (Rank != indices!.Length) // TODO-NULLABLE: Remove ! when [DoesNotReturn] respected
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankIndices);

            TypedReference elemref = new TypedReference();
            fixed (int* pIndices = &indices[0])
                InternalGetReference(&elemref, indices.Length, pIndices);
            InternalSetValue(&elemref, value);
        }

        private static void SortImpl(Array keys, Array? items, int index, int length, IComparer comparer)
        {
            Debug.Assert(comparer != null);

            if (comparer == Comparer.Default)
            {
                bool r = TrySZSort(keys, items, index, index + length - 1);
                if (r)
                    return;
            }

            object[]? objKeys = keys as object[];
            object[]? objItems = null;
            if (objKeys != null)
                objItems = items as object[];
            if (objKeys != null && (items == null || objItems != null))
            {
                SorterObjectArray sorter = new SorterObjectArray(objKeys, objItems, comparer);
                sorter.Sort(index, length);
            }
            else
            {
                SorterGenericArray sorter = new SorterGenericArray(keys, items, comparer);
                sorter.Sort(index, length);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        // reference to TypedReference is banned, so have to pass result as pointer
        private extern unsafe void InternalGetReference(void* elemRef, int rank, int* pIndices);

        // Ideally, we would like to use TypedReference.SetValue instead. Unfortunately, TypedReference.SetValue
        // always throws not-supported exception
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void InternalSetValue(void* target, object? value);

        public extern int Length
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        public extern long LongLength
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern int GetLength(int dimension);

        public extern int Rank
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern int GetUpperBound(int dimension);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern int GetLowerBound(int dimension);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern ref byte GetRawArrayData();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern int GetElementSize();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool TrySZBinarySearch(Array sourceArray, int sourceIndex, int count, object? value, out int retVal);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool TrySZIndexOf(Array sourceArray, int sourceIndex, int count, object? value, out int retVal);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool TrySZLastIndexOf(Array sourceArray, int sourceIndex, int count, object? value, out int retVal);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool TrySZReverse(Array array, int index, int count);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool TrySZSort(Array keys, Array? items, int left, int right);

        // if this is an array of value classes and that value class has a default constructor 
        // then this calls this default constructor on every element in the value class array.
        // otherwise this is a no-op.  Generally this method is called automatically by the compiler
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern void Initialize();
    }

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
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
            T[] _this = Unsafe.As<T[]>(this);
            return _this.Length == 0 ? SZGenericArrayEnumerator<T>.Empty : new SZGenericArrayEnumerator<T>(_this);
        }

        private void CopyTo<T>(T[] array, int index)
        {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!

            T[] _this = Unsafe.As<T[]>(this);
            Array.Copy(_this, 0, array, index, _this.Length);
        }

        internal int get_Count<T>()
        {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
            T[] _this = Unsafe.As<T[]>(this);
            return _this.Length;
        }

        internal T get_Item<T>(int index)
        {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
            T[] _this = Unsafe.As<T[]>(this);
            if ((uint)index >= (uint)_this.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            }

            return _this[index];
        }

        internal void set_Item<T>(int index, T value)
        {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
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
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
            T[] _this = Unsafe.As<T[]>(this);
            return Array.IndexOf(_this, value, 0, _this.Length) >= 0;
        }

        private bool get_IsReadOnly<T>()
        {
            return true;
        }

        private void Clear<T>()
        {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!

            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
        }

        private int IndexOf<T>(T value)
        {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
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
}
