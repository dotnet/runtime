// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** Purpose: Base class which can be used to access any array
**
===========================================================*/
namespace System {

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Security;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    // Note that we make a T[] (single-dimensional w/ zero as the lower bound) implement both 
    // IList<U> and IReadOnlyList<U>, where T : U dynamically.  See the SZArrayHelper class for details.
    [Serializable]
    [ComVisible(true)]
    public abstract class Array : ICloneable, IList, IStructuralComparable, IStructuralEquatable 
    {
        // This ctor exists solely to prevent C# from generating a protected .ctor that violates the surface area. I really want this to be a
        // "protected-and-internal" rather than "internal" but C# has no keyword for the former.
        internal Array() {}

        public static ReadOnlyCollection<T> AsReadOnly<T>(T[] array) {
            if (array == null) {
                throw new ArgumentNullException("array");                
            }
            Contract.Ensures(Contract.Result<ReadOnlyCollection<T>>() != null);

            // T[] implements IList<T>.
            return new ReadOnlyCollection<T>(array);
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static void Resize<T>(ref T[] array, int newSize) {
            if (newSize < 0)
                throw new ArgumentOutOfRangeException("newSize", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.ValueAtReturn(out array) != null);
            Contract.Ensures(Contract.ValueAtReturn(out array).Length == newSize);
            Contract.EndContractBlock();

            T[] larray = array;                
            if (larray == null) {
                array = new T[newSize];
                return;
            }
        
            if (larray.Length != newSize) {
                T[] newArray = new T[newSize];
                Array.Copy(larray, 0, newArray, 0,  larray.Length > newSize? newSize : larray.Length);
                array = newArray;
            }
        }

        // Create instance will create an array
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static Array CreateInstance(Type elementType, int length)
        {
            if ((object)elementType == null)
                throw new ArgumentNullException("elementType");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.Result<Array>() != null);
            Contract.Ensures(Contract.Result<Array>().Length == length);
            Contract.Ensures(Contract.Result<Array>().Rank == 1);
            Contract.EndContractBlock();

            RuntimeType t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"elementType");
            return InternalCreate((void*)t.TypeHandle.Value,1,&length,null);
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static Array CreateInstance(Type elementType, int length1, int length2)
        {
            if ((object)elementType == null)
                throw new ArgumentNullException("elementType");
            if (length1 < 0 || length2 < 0)
                throw new ArgumentOutOfRangeException((length1 < 0 ? "length1" : "length2"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.Result<Array>() != null);
            Contract.Ensures(Contract.Result<Array>().Rank == 2);
            Contract.Ensures(Contract.Result<Array>().GetLength(0) == length1);
            Contract.Ensures(Contract.Result<Array>().GetLength(1) == length2);

            RuntimeType t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"),"elementType");
            int* pLengths = stackalloc int[2];
            pLengths[0] = length1;
            pLengths[1] = length2;
            return InternalCreate((void*)t.TypeHandle.Value,2,pLengths,null);
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static Array CreateInstance(Type elementType, int length1, int length2, int length3)
        {
            if ((object)elementType == null)
                throw new ArgumentNullException("elementType");
            if (length1 < 0)
                throw new ArgumentOutOfRangeException("length1", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (length2 < 0)
                throw new ArgumentOutOfRangeException("length2", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (length3 < 0)
                throw new ArgumentOutOfRangeException("length3", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            Contract.Ensures(Contract.Result<Array>() != null);
            Contract.Ensures(Contract.Result<Array>().Rank == 3);
            Contract.Ensures(Contract.Result<Array>().GetLength(0) == length1);
            Contract.Ensures(Contract.Result<Array>().GetLength(1) == length2);
            Contract.Ensures(Contract.Result<Array>().GetLength(2) == length3);

            RuntimeType t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "elementType");
            int* pLengths = stackalloc int[3];
            pLengths[0] = length1;
            pLengths[1] = length2;
            pLengths[2] = length3;
            return InternalCreate((void*)t.TypeHandle.Value,3,pLengths,null);
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static Array CreateInstance(Type elementType, params int[] lengths)
        {
            if ((object)elementType == null)
                throw new ArgumentNullException("elementType");
            if (lengths == null)
                throw new ArgumentNullException("lengths");
            if (lengths.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_NeedAtLeast1Rank"));
            Contract.Ensures(Contract.Result<Array>() != null);
            Contract.Ensures(Contract.Result<Array>().Rank == lengths.Length);
            Contract.EndContractBlock();

            RuntimeType t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "elementType");

            // Check to make sure the lenghts are all positive. Note that we check this here to give
            // a good exception message if they are not; however we check this again inside the execution 
            // engine's low level allocation function after having made a copy of the array to prevent a 
            // malicious caller from mutating the array after this check.           
            for (int i=0;i<lengths.Length;i++)
                if (lengths[i] < 0)
                    throw new ArgumentOutOfRangeException("lengths["+i+']', Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                
            fixed(int* pLengths = lengths)
                return InternalCreate((void*)t.TypeHandle.Value,lengths.Length,pLengths,null);
        }

        public static Array CreateInstance(Type elementType, params long[] lengths)
        {
            if( lengths == null) {
                throw new ArgumentNullException("lengths");                
            }
            if (lengths.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_NeedAtLeast1Rank"));
            Contract.Ensures(Contract.Result<Array>() != null);
            Contract.Ensures(Contract.Result<Array>().Rank == lengths.Length);
            Contract.EndContractBlock();
            
            int[] intLengths = new int[lengths.Length];

            for (int i = 0; i < lengths.Length; ++i) 
            {
                long len = lengths[i];
                if (len > Int32.MaxValue || len < Int32.MinValue) 
                    throw new ArgumentOutOfRangeException("len", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
                intLengths[i] = (int) len;
            }

            return Array.CreateInstance(elementType, intLengths);
        }

        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe static Array CreateInstance(Type elementType, int[] lengths,int[] lowerBounds)
        {
            if (elementType == null)
                throw new ArgumentNullException("elementType");
            if (lengths == null)
                throw new ArgumentNullException("lengths");
            if (lowerBounds == null)
                throw new ArgumentNullException("lowerBounds");
            if (lengths.Length != lowerBounds.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_RanksAndBounds"));
            if (lengths.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_NeedAtLeast1Rank"));
            Contract.Ensures(Contract.Result<Array>() != null);
            Contract.Ensures(Contract.Result<Array>().Rank == lengths.Length);
            Contract.EndContractBlock();

            RuntimeType t = elementType.UnderlyingSystemType as RuntimeType;
            if (t == null)
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeType"), "elementType");

            // Check to make sure the lenghts are all positive. Note that we check this here to give
            // a good exception message if they are not; however we check this again inside the execution 
            // engine's low level allocation function after having made a copy of the array to prevent a 
            // malicious caller from mutating the array after this check.           
            for (int i=0;i<lengths.Length;i++)
                if (lengths[i] < 0)
                    throw new ArgumentOutOfRangeException("lengths["+i+']', Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
                            
            fixed(int* pLengths = lengths)
                fixed(int* pLowerBounds = lowerBounds)
                    return InternalCreate((void*)t.TypeHandle.Value,lengths.Length,pLengths,pLowerBounds);
        }
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private unsafe static extern Array InternalCreate(void* elementType,int rank,int *pLengths,int *pLowerBounds);

        [SecurityCritical]
#if !FEATURE_CORECLR
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        internal static Array UnsafeCreateInstance(Type elementType, int length)
        {
            return CreateInstance(elementType, length);
        }

        [SecurityCritical]
#if !FEATURE_CORECLR
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        internal static Array UnsafeCreateInstance(Type elementType, int length1, int length2)
        {
            return CreateInstance(elementType, length1, length2);
        }

        [SecurityCritical]
#if !FEATURE_CORECLR
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        internal static Array UnsafeCreateInstance(Type elementType, params int[] lengths)
        {
            return CreateInstance(elementType, lengths);
        }

        [SecurityCritical]
#if !FEATURE_CORECLR
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        internal static Array UnsafeCreateInstance(Type elementType, int[] lengths, int[] lowerBounds)
        {
            return CreateInstance(elementType, lengths, lowerBounds);
        }

        // Copies length elements from sourceArray, starting at index 0, to
        // destinationArray, starting at index 0.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Copy(Array sourceArray, Array destinationArray, int length)
        {
            if (sourceArray == null)
                throw new ArgumentNullException("sourceArray");
            if (destinationArray == null)
                throw new ArgumentNullException("destinationArray");
            /*
            Contract.Requires(sourceArray.Rank == destinationArray.Rank);
            Contract.Requires(length >= 0);
            Contract.Requires(length <= sourceArray.GetLowerBound(0) + sourceArray.Length);
            Contract.Requires(length <= destinationArray.GetLowerBound(0) + destinationArray.Length);
             */
            Contract.EndContractBlock();

            Copy(sourceArray, sourceArray.GetLowerBound(0), destinationArray, destinationArray.GetLowerBound(0), length, false);
        }
    
        // Copies length elements from sourceArray, starting at sourceIndex, to
        // destinationArray, starting at destinationIndex.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length) 
        {
            Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length, false);
        }

        // Reliability-wise, this method will either possibly corrupt your 
        // instance & might fail when called from within a CER, or if the
        // reliable flag is true, it will either always succeed or always
        // throw an exception with no side effects.
        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable);

        // Provides a strong exception guarantee - either it succeeds, or
        // it throws an exception with no side effects.  The arrays must be
        // compatible array types based on the array element type - this 
        // method does not support casting, boxing, or primitive widening.
        // It will up-cast, assuming the array types are correct.
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static void ConstrainedCopy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length, true);
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Copy(Array sourceArray, Array destinationArray, long length)
        {
            if (length > Int32.MaxValue || length < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));

            Array.Copy(sourceArray, destinationArray, (int) length);
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Copy(Array sourceArray, long sourceIndex, Array destinationArray, long destinationIndex, long length)
        {
            if (sourceIndex > Int32.MaxValue || sourceIndex < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("sourceIndex", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            if (destinationIndex > Int32.MaxValue || destinationIndex < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("destinationIndex", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            if (length > Int32.MaxValue || length < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("length", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));

            Array.Copy(sourceArray, (int) sourceIndex, destinationArray, (int) destinationIndex, (int) length);
        }

    
        // Sets length elements in array to 0 (or null for Object arrays), starting
        // at index.
        //
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public static extern void Clear(Array array, int index, int length);
        
        // The various Get values...
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe Object GetValue(params int[] indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");
            if (Rank != indices.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankIndices"));
            Contract.EndContractBlock();

            TypedReference elemref = new TypedReference();
            fixed(int* pIndices = indices)
                InternalGetReference(&elemref, indices.Length, pIndices);
            return TypedReference.InternalToObject(&elemref);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe Object GetValue(int index)
        {
            if (Rank != 1)
                throw new ArgumentException(Environment.GetResourceString("Arg_Need1DArray"));
            Contract.EndContractBlock();

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 1, &index);
            return TypedReference.InternalToObject(&elemref);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe Object GetValue(int index1, int index2)
        {
            if (Rank != 2)
                throw new ArgumentException(Environment.GetResourceString("Arg_Need2DArray"));
            Contract.EndContractBlock();

            int* pIndices = stackalloc int[2];
            pIndices[0] = index1;
            pIndices[1] = index2;

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 2, pIndices);
            return TypedReference.InternalToObject(&elemref);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe Object GetValue(int index1, int index2, int index3)
        {
            if (Rank != 3)
                throw new ArgumentException(Environment.GetResourceString("Arg_Need3DArray"));
            Contract.EndContractBlock();

            int* pIndices = stackalloc int[3];
            pIndices[0] = index1;
            pIndices[1] = index2;
            pIndices[2] = index3;

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 3, pIndices);
            return TypedReference.InternalToObject(&elemref);
        }

        [ComVisible(false)]       
        public Object GetValue(long index)
        {
            if (index > Int32.MaxValue || index < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            Contract.EndContractBlock();

            return this.GetValue((int) index);
        }

        [ComVisible(false)]
        public Object GetValue(long index1, long index2)
        {
            if (index1 > Int32.MaxValue || index1 < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index1", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            if (index2 > Int32.MaxValue || index2 < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index2", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            Contract.EndContractBlock();

            return this.GetValue((int) index1, (int) index2);
        }

        [ComVisible(false)]
        public Object GetValue(long index1, long index2, long index3)
        {
            if (index1 > Int32.MaxValue || index1 < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index1", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            if (index2 > Int32.MaxValue || index2 < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index2", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            if (index3 > Int32.MaxValue || index3 < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index3", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            Contract.EndContractBlock();
            
            return this.GetValue((int) index1, (int) index2, (int) index3);
        }

        [ComVisible(false)]
        public Object GetValue(params long[] indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");
            if (Rank != indices.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankIndices"));
            Contract.EndContractBlock();

            int[] intIndices = new int[indices.Length];

            for (int i = 0; i < indices.Length; ++i) 
            {
                long index = indices[i];
                if (index > Int32.MaxValue || index < Int32.MinValue) 
                    throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
                intIndices[i] = (int) index;
            }

            return this.GetValue(intIndices);
        }

        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe void SetValue(Object value,int index)
        {
            if (Rank != 1)
                throw new ArgumentException(Environment.GetResourceString("Arg_Need1DArray"));
            Contract.EndContractBlock();

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 1, &index);
            InternalSetValue(&elemref,value);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe void SetValue(Object value,int index1, int index2)
        {
            if (Rank != 2)
                throw new ArgumentException(Environment.GetResourceString("Arg_Need2DArray"));
            Contract.EndContractBlock();

            int* pIndices = stackalloc int[2];
            pIndices[0] = index1;
            pIndices[1] = index2;

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 2, pIndices);
            InternalSetValue(&elemref,value);
        }
    
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe void SetValue(Object value,int index1, int index2, int index3)
        {
            if (Rank != 3)
                throw new ArgumentException(Environment.GetResourceString("Arg_Need3DArray"));
            Contract.EndContractBlock();

            int* pIndices = stackalloc int[3];
            pIndices[0] = index1;
            pIndices[1] = index2;
            pIndices[2] = index3;

            TypedReference elemref = new TypedReference();
            InternalGetReference(&elemref, 3, pIndices);
            InternalSetValue(&elemref,value);
        }
        
        [System.Security.SecuritySafeCritical]  // auto-generated
        public unsafe void SetValue(Object value,params int[] indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");
            if (Rank != indices.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankIndices"));
            Contract.EndContractBlock();

            TypedReference elemref = new TypedReference();
            fixed(int* pIndices = indices)
                InternalGetReference(&elemref, indices.Length, pIndices);
            InternalSetValue(&elemref,value);
        }

        [ComVisible(false)]
        public void SetValue(Object value, long index)
        {
            if (index > Int32.MaxValue || index < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            Contract.EndContractBlock();

            this.SetValue(value, (int) index);
        }

        [ComVisible(false)]
        public void SetValue(Object value, long index1, long index2)
        {
            if (index1 > Int32.MaxValue || index1 < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index1", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            if (index2 > Int32.MaxValue || index2 < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index2", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            Contract.EndContractBlock();

            this.SetValue(value, (int) index1, (int) index2);
        }

        [ComVisible(false)]
        public void SetValue(Object value, long index1, long index2, long index3)
        {
            if (index1 > Int32.MaxValue || index1 < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index1", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            if (index2 > Int32.MaxValue || index2 < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index2", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            if (index3 > Int32.MaxValue || index3 < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index3", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            Contract.EndContractBlock();

            this.SetValue(value, (int) index1, (int) index2, (int) index3);
        }

        [ComVisible(false)]
        public void SetValue(Object value, params long[] indices)
        {
            if (indices == null)
                throw new ArgumentNullException("indices");
            if (Rank != indices.Length)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankIndices"));
            Contract.EndContractBlock();

            int[] intIndices = new int[indices.Length];

            for (int i = 0; i < indices.Length; ++i) 
            {
                long index = indices[i];
                if (index > Int32.MaxValue || index < Int32.MinValue) 
                    throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
                intIndices[i] = (int) index;
            }

            this.SetValue(value, intIndices);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        // reference to TypedReference is banned, so have to pass result as pointer
        private unsafe extern void InternalGetReference(void * elemRef, int rank, int * pIndices);

        // Ideally, we would like to use TypedReference.SetValue instead. Unfortunately, TypedReference.SetValue
        // always throws not-supported exception
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private unsafe extern static void InternalSetValue(void * target, Object value);

        public extern int Length {
            [Pure]
            [System.Security.SecuritySafeCritical]  // auto-generated
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        private static int GetMedian(int low, int hi) {
            // Note both may be negative, if we are dealing with arrays w/ negative lower bounds.
            Contract.Requires(low <= hi);
            Contract.Assert( hi - low >= 0, "Length overflow!");
            return low + ((hi - low) >> 1);
        }

        // We impose limits on maximum array lenght in each dimension to allow efficient 
        // implementation of advanced range check elimination in future.
        // Keep in sync with vm\gcscan.cpp and HashHelpers.MaxPrimeArrayLength.
        // The constants are defined in this method: inline SIZE_T MaxArrayLength(SIZE_T componentSize) from gcscan
        // We have different max sizes for arrays with elements of size 1 for backwards compatibility
        internal const int MaxArrayLength = 0X7FEFFFFF;
        internal const int MaxByteArrayLength = 0x7FFFFFC7;

        [ComVisible(false)]
        public extern long LongLength {
            [Pure]
            [System.Security.SecuritySafeCritical]  // auto-generated
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern int GetLength(int dimension);

        [Pure]
        [ComVisible(false)]
        public long GetLongLength(int dimension) {
            //This method should throw an IndexOufOfRangeException for compat if dimension < 0 or >= Rank
            return GetLength(dimension);
        }

        public extern int Rank {
            [Pure]
            [System.Security.SecuritySafeCritical]  // auto-generated
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            get;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Pure]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        public extern int GetUpperBound(int dimension);

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public extern int GetLowerBound(int dimension);

        [System.Security.SecurityCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal extern int GetDataPtrOffsetInternal();

        // Number of elements in the Array.
        int ICollection.Count
        { get { return Length; } }

    
        // Returns an object appropriate for synchronizing access to this 
        // Array.
        public Object SyncRoot
        { get { return this; } }
        
        // Is this Array read-only?
        public bool IsReadOnly
        { get { return false; } }

        public bool IsFixedSize {
            get { return true; }
        }
    
        // Is this Array synchronized (i.e., thread-safe)?  If you want a synchronized
        // collection, you can use SyncRoot as an object to synchronize your 
        // collection with.  You could also call GetSynchronized() 
        // to get a synchronized wrapper around the Array.
        public bool IsSynchronized
        { get { return false; } }


        Object IList.this[int index] {
            get { return GetValue(index); }
            set { SetValue(value, index); }
        }

        int IList.Add(Object value)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
        }

        bool IList.Contains(Object value)
        {
            return Array.IndexOf(this, value) >= this.GetLowerBound(0);
        }

        void IList.Clear()
        {
            Array.Clear(this, this.GetLowerBound(0), this.Length);
        }

        int IList.IndexOf(Object value)
        {
            return Array.IndexOf(this, value);
        }

        void IList.Insert(int index, Object value)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
        }

        void IList.Remove(Object value)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
        }

        // Make a new array which is a shallow copy of the original array.
        // 
        public Object Clone()
        {
            return MemberwiseClone();
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer) {
            if (other == null) {
                return 1;
            }

            Array o = other as Array;

            if (o == null || this.Length != o.Length) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_OtherNotArrayOfCorrectLength"), "other");
            }

            int i = 0;
            int c = 0;

            while (i < o.Length && c == 0) {
                object left = GetValue(i);
                object right = o.GetValue(i);

                c = comparer.Compare(left, right);
                i++;
            }
            
            return c;
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer) {

            if (other == null) {
                return false;
            }

            if (Object.ReferenceEquals(this, other)) {
                return true;
            }

            Array o = other as Array;

            if (o == null || o.Length != this.Length) {
                return false;
            }

            int i = 0;
            while (i < o.Length) {
                object left = GetValue(i);
                object right = o.GetValue(i);

                if (!comparer.Equals(left, right)) {
                    return false;
                }
                i++;
            }

            return true;
        }

        // From System.Web.Util.HashCodeCombiner
        internal static int CombineHashCodes(int h1, int h2) {
            return (((h1 << 5) + h1) ^ h2);
        }

        int IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
            if (comparer == null)
                throw new ArgumentNullException("comparer");
            Contract.EndContractBlock();

            int ret = 0;

            for (int i = (this.Length >= 8 ? this.Length - 8 : 0); i < this.Length; i++) {
                ret = CombineHashCodes(ret, comparer.GetHashCode(GetValue(i)));
            }

            return ret;
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
        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int BinarySearch(Array array, Object value) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.Ensures((Contract.Result<int>() >= array.GetLowerBound(0) && Contract.Result<int>() <= array.GetUpperBound(0)) || (Contract.Result<int>() < array.GetLowerBound(0) && ~Contract.Result<int>() <= array.GetUpperBound(0) + 1));
            Contract.EndContractBlock();
            int lb = array.GetLowerBound(0);
            return BinarySearch(array, lb, array.Length, value, null);
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
        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int BinarySearch(Array array, int index, int length, Object value) {
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
        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int BinarySearch(Array array, Object value, IComparer comparer) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.EndContractBlock();
            int lb = array.GetLowerBound(0);
            return BinarySearch(array, lb, array.Length, value, comparer);
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
        [Pure]
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int BinarySearch(Array array, int index, int length, Object value, IComparer comparer) {
            if (array==null) 
                throw new ArgumentNullException("array");
            Contract.EndContractBlock();
            int lb = array.GetLowerBound(0);
            if (index < lb || length < 0)
                throw new ArgumentOutOfRangeException((index<lb ? "index" : "length"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - (index - lb) < length)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            if (array.Rank != 1)
                throw new RankException(Environment.GetResourceString("Rank_MultiDimNotSupported"));
            
            if (comparer == null) comparer = Comparer.Default;
            if (comparer == Comparer.Default) {
                int retval;
                bool r = TrySZBinarySearch(array, index, length, value, out retval);
                if (r)
                    return retval;
            }

            int lo = index;
            int hi = index + length - 1;   
            Object[] objArray = array as Object[];
            if(objArray != null) {
                while (lo <= hi) {
                    // i might overflow if lo and hi are both large positive numbers. 
                    int i = GetMedian(lo, hi);

                    int c;
                    try {
                        c = comparer.Compare(objArray[i], value);
                    }
                    catch (Exception e) {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
                    }
                    if (c == 0) return i;
                    if (c < 0) {
                        lo = i + 1;
                    }
                    else {
                        hi = i - 1;
                    }
                }
            }
            else {
                while (lo <= hi) {
                    int i = GetMedian(lo, hi);                    

                    int c;
                    try {
                        c = comparer.Compare(array.GetValue(i), value);
                    }
                    catch (Exception e) {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
                    }
                    if (c == 0) return i;
                    if (c < 0) {
                        lo = i + 1;
                    }
                    else {
                        hi = i - 1;
                    }
                }
            }
            return ~lo;
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern bool TrySZBinarySearch(Array sourceArray, int sourceIndex, int count, Object value, out int retVal);
        
        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int BinarySearch<T>(T[] array, T value) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.EndContractBlock();
            return BinarySearch<T>(array, 0, array.Length, value, null);
        }

        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int BinarySearch<T>(T[] array, T value, System.Collections.Generic.IComparer<T> comparer) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.EndContractBlock();
            return BinarySearch<T>(array, 0, array.Length, value, comparer);
        }

        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int BinarySearch<T>(T[] array, int index, int length, T value) {
            return BinarySearch<T>(array, index, length, value, null);
        }

        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int BinarySearch<T>(T[] array, int index, int length, T value, System.Collections.Generic.IComparer<T> comparer) {
            if (array==null) 
                throw new ArgumentNullException("array");
            if (index < 0 || length < 0)
                throw new ArgumentOutOfRangeException((index<0 ? "index" : "length"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - index < length)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            return ArraySortHelper<T>.Default.BinarySearch(array, index, length, value, comparer);
        }

        public static TOutput[] ConvertAll<TInput, TOutput>(TInput[] array, Converter<TInput,TOutput> converter) {
            if( array == null) {
                throw new ArgumentNullException("array");
            }

            if( converter == null) {
                throw new ArgumentNullException("converter");
            }
            Contract.Ensures(Contract.Result<TOutput[]>() != null);
            Contract.Ensures(Contract.Result<TOutput[]>().Length == array.Length);
            Contract.EndContractBlock();

            TOutput[] newArray = new TOutput[array.Length];
            for( int i = 0; i< array.Length; i++) {
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
        [Pure]
        public void CopyTo(Array array, int index)
        {
            if (array != null && array.Rank != 1)
                throw new ArgumentException(Environment.GetResourceString("Arg_RankMultiDimNotSupported"));
            Contract.EndContractBlock();
            // Note: Array.Copy throws a RankException and we want a consistent ArgumentException for all the IList CopyTo methods.
            Array.Copy(this, GetLowerBound(0), array, index, Length);
        }

        [Pure]
        [ComVisible(false)]
        public void CopyTo(Array array, long index)
        {
            if (index > Int32.MaxValue || index < Int32.MinValue) 
                throw new ArgumentOutOfRangeException("index", Environment.GetResourceString("ArgumentOutOfRange_HugeArrayNotSupported"));
            Contract.EndContractBlock();

            this.CopyTo(array, (int) index);
        }

        [Pure]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static T[] Empty<T>()
        {
            Contract.Ensures(Contract.Result<T[]>() != null);
            Contract.Ensures(Contract.Result<T[]>().Length == 0);
            Contract.EndContractBlock();

            return EmptyArray<T>.Value;
        }

        public static bool Exists<T>(T[] array, Predicate<T> match) {
            return Array.FindIndex(array, match) != -1;
        }

        public static T Find<T>(T[] array, Predicate<T> match) {
            if( array == null) {
                throw new ArgumentNullException("array");
            }

            if( match == null) {
                throw new ArgumentNullException("match");
            }
            Contract.EndContractBlock();

            for(int i = 0 ; i < array.Length; i++) {
                if(match(array[i])) {
                    return array[i];
                }
            }
            return default(T);
        }

        public static T[] FindAll<T>(T[] array, Predicate<T> match) {
            if( array == null) {
                throw new ArgumentNullException("array");
            }

            if( match == null) {
                throw new ArgumentNullException("match");
            }
            Contract.EndContractBlock();

            List<T> list = new List<T>(); 
            for(int i = 0 ; i < array.Length; i++) {
                if(match(array[i])) {
                    list.Add(array[i]);
                }
            }
            return list.ToArray();
        }

        public static int FindIndex<T>(T[] array, Predicate<T> match) {
            if (array==null) {
                throw new ArgumentNullException("array");
            }
            Contract.Ensures(Contract.Result<int>() < array.Length);
            Contract.EndContractBlock();

            return FindIndex(array, 0, array.Length, match);
        }

        public static int FindIndex<T>(T[] array, int startIndex, Predicate<T> match) {
            if (array==null) {
                throw new ArgumentNullException("array");
            }
            Contract.Ensures(Contract.Result<int>() < array.Length);
            Contract.EndContractBlock();
            
            return FindIndex(array, startIndex, array.Length - startIndex, match);
        }

        public static int FindIndex<T>(T[] array, int startIndex, int count, Predicate<T> match) {
            if (array==null) {
                throw new ArgumentNullException("array");
            }

            if( startIndex < 0 || startIndex > array.Length ) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));                
            }

            if (count < 0 || startIndex > array.Length - count) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            }

            if( match == null) {
                throw new ArgumentNullException("match");
            }
            Contract.Ensures(Contract.Result<int>() < array.Length);
            Contract.EndContractBlock();

            int endIndex = startIndex + count;
            for( int i = startIndex; i < endIndex; i++) {
                if( match(array[i])) return i;
            }
            return -1;
        }

        public static T FindLast<T>(T[] array, Predicate<T> match) {
            if( array == null) {
                throw new ArgumentNullException("array");
            }

            if( match == null) {
                throw new ArgumentNullException("match");
            }
            Contract.EndContractBlock();
            
            for(int i = array.Length - 1 ; i >= 0; i--) {
                if(match(array[i])) {
                    return array[i];
                }
            }
            return default(T);
        }

        public static int FindLastIndex<T>(T[] array, Predicate<T> match) {
            if( array == null) {
                throw new ArgumentNullException("array");
            }
            Contract.EndContractBlock();

            return FindLastIndex(array, array.Length - 1, array.Length, match);
        }

        public static int FindLastIndex<T>(T[] array, int startIndex, Predicate<T> match) {
            if( array == null) {
                throw new ArgumentNullException("array");
            }
            Contract.EndContractBlock();
        
            return FindLastIndex(array, startIndex, startIndex + 1, match);
        }

        public static int FindLastIndex<T>(T[] array, int startIndex, int count, Predicate<T> match) {
            if( array == null) {
                throw new ArgumentNullException("array");
            }

            if( match == null) {
                throw new ArgumentNullException("match");
            }
            Contract.EndContractBlock();

            if(array.Length == 0) {
                // Special case for 0 length List
                if( startIndex != -1) {
                    throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                }
            }
            else {
                // Make sure we're not out of range            
                if ( startIndex < 0 || startIndex >= array.Length) {
                    throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                }
            }
            
            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            }

            int endIndex = startIndex - count;
            for( int i = startIndex; i > endIndex; i--) {
                if( match(array[i])) {
                    return i;
                }
            }
            return -1;
        }

        public static void ForEach<T>(T[] array, Action<T> action) {
            if( array == null) {
                throw new ArgumentNullException("array");
            }

            if( action == null) {
                throw new ArgumentNullException("action");
            }
            Contract.EndContractBlock();

            for(int i = 0 ; i < array.Length; i++) {
                action(array[i]);
            }
        }

        // GetEnumerator returns an IEnumerator over this Array.  
        // 
        // Currently, only one dimensional arrays are supported.
        // 
        public IEnumerator GetEnumerator()
        {
            int lowerBound = GetLowerBound(0);
            if (Rank == 1 && lowerBound == 0)
                return new SZArrayEnumerator(this);
            else
                return new ArrayEnumerator(this, lowerBound, Length);
        }
        
        // Returns the index of the first occurrence of a given value in an array.
        // The array is searched forwards, and the elements of the array are
        // compared to the given value using the Object.Equals method.
        // 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int IndexOf(Array array, Object value) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.Ensures(Contract.Result<int>() < array.GetLowerBound(0) + array.Length);
            Contract.EndContractBlock();
            int lb = array.GetLowerBound(0);
            return IndexOf(array, value, lb, array.Length);
        }
        
        // Returns the index of the first occurrence of a given value in a range of
        // an array. The array is searched forwards, starting at index
        // startIndex and ending at the last element of the array. The
        // elements of the array are compared to the given value using the
        // Object.Equals method.
        // 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int IndexOf(Array array, Object value, int startIndex) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.Ensures(Contract.Result<int>() < array.GetLowerBound(0) + array.Length);
            Contract.EndContractBlock();
            int lb = array.GetLowerBound(0);
            return IndexOf(array, value, startIndex, array.Length - startIndex + lb);
        }
        
        // Returns the index of the first occurrence of a given value in a range of
        // an array. The array is searched forwards, starting at index
        // startIndex and upto count elements. The
        // elements of the array are compared to the given value using the
        // Object.Equals method.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int IndexOf(Array array, Object value, int startIndex, int count) {
            if (array==null)
                throw new ArgumentNullException("array");
            if (array.Rank != 1)
                throw new RankException(Environment.GetResourceString("Rank_MultiDimNotSupported"));
            Contract.Ensures(Contract.Result<int>() < array.GetLowerBound(0) + array.Length);
            Contract.EndContractBlock();

            int lb = array.GetLowerBound(0);
            if (startIndex < lb || startIndex > array.Length + lb)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count < 0 || count > array.Length - startIndex + lb)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));

            // Try calling a quick native method to handle primitive types.
            int retVal;
            bool r = TrySZIndexOf(array, startIndex, count, value, out retVal);
            if (r)
                return retVal;

            Object[] objArray = array as Object[];
            int endIndex = startIndex + count;
            if (objArray != null) {
                if (value == null) {
                    for (int i = startIndex; i < endIndex; i++) {
                        if (objArray[i] == null) return i;
                    }
                }
                else {
                    for (int i = startIndex; i < endIndex; i++) {
                        Object obj = objArray[i];
                        if (obj != null && obj.Equals(value)) return i;
                    }
                }
            }
            else {                
                for (int i = startIndex; i < endIndex; i++) {
                    Object obj = array.GetValue(i);
                    if( obj == null) {
                        if(value == null) return i;
                    }
                    else {
                        if( obj.Equals(value)) return i;
                    }
                }
            }
            // Return one less than the lower bound of the array.  This way,
            // for arrays with a lower bound of -1 we will not return -1 when the
            // item was not found.  And for SZArrays (the vast majority), -1 still
            // works for them.
            return lb-1;
        }

        [Pure]
        public static int IndexOf<T>(T[] array, T value) {
            if (array==null) {
                throw new ArgumentNullException("array");
            }
            Contract.Ensures((Contract.Result<int>() < 0) ||
                (Contract.Result<int>() >= 0 && Contract.Result<int>() < array.Length && EqualityComparer<T>.Default.Equals(value, array[Contract.Result<int>()])));
            Contract.EndContractBlock();

            return IndexOf(array, value, 0, array.Length);
        }

        public static int IndexOf<T>(T[] array, T value, int startIndex) {
            if (array==null) {
                throw new ArgumentNullException("array");
            }
            Contract.Ensures(Contract.Result<int>() < array.Length);
            Contract.EndContractBlock();

            return IndexOf(array, value, startIndex, array.Length - startIndex);
        }

        public static int IndexOf<T>(T[] array, T value, int startIndex, int count) {
            if (array==null) {
                throw new ArgumentNullException("array");
            }

            if (startIndex < 0 || startIndex > array.Length ) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }

            if (count < 0 || count > array.Length - startIndex) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            }
            Contract.Ensures(Contract.Result<int>() < array.Length);
            Contract.EndContractBlock();

            return EqualityComparer<T>.Default.IndexOf(array, value, startIndex, count);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern bool TrySZIndexOf(Array sourceArray, int sourceIndex, int count, Object value, out int retVal);
        

        // Returns the index of the last occurrence of a given value in an array.
        // The array is searched backwards, and the elements of the array are
        // compared to the given value using the Object.Equals method.
        // 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int LastIndexOf(Array array, Object value) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.Ensures(Contract.Result<int>() < array.GetLowerBound(0) + array.Length);
            Contract.EndContractBlock();
            int lb = array.GetLowerBound(0);
            return LastIndexOf(array, value, array.Length - 1 + lb, array.Length);
        }
        
        // Returns the index of the last occurrence of a given value in a range of
        // an array. The array is searched backwards, starting at index
        // startIndex and ending at index 0. The elements of the array are
        // compared to the given value using the Object.Equals method.
        // 
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int LastIndexOf(Array array, Object value, int startIndex) {
            if (array == null)
                throw new ArgumentNullException("array");
            Contract.Ensures(Contract.Result<int>() < array.GetLowerBound(0) + array.Length);
            Contract.EndContractBlock();
            int lb = array.GetLowerBound(0);
            return LastIndexOf(array, value, startIndex, startIndex + 1 - lb);
        }
        
        // Returns the index of the last occurrence of a given value in a range of
        // an array. The array is searched backwards, starting at index
        // startIndex and counting uptocount elements. The elements of
        // the array are compared to the given value using the Object.Equals
        // method.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        public static int LastIndexOf(Array array, Object value, int startIndex, int count) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.Ensures(Contract.Result<int>() < array.GetLowerBound(0) + array.Length);
            Contract.EndContractBlock();
            int lb = array.GetLowerBound(0);
            if (array.Length == 0) {
                return lb-1;
            }

            if (startIndex < lb || startIndex >= array.Length + lb)
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            if (count > startIndex - lb + 1)
                throw new ArgumentOutOfRangeException("endIndex", Environment.GetResourceString("ArgumentOutOfRange_EndIndexStartIndex"));
            if (array.Rank != 1)
                throw new RankException(Environment.GetResourceString("Rank_MultiDimNotSupported"));

            // Try calling a quick native method to handle primitive types.
            int retVal;
            bool r = TrySZLastIndexOf(array, startIndex, count, value, out retVal);
            if (r)
                return retVal;

            Object[] objArray = array as Object[];
            int endIndex = startIndex - count + 1;
            if (objArray!=null) {
                if (value == null) {
                    for (int i = startIndex; i >= endIndex; i--) {
                        if (objArray[i] == null) return i;
                    }
                }
                else {
                    for (int i = startIndex; i >= endIndex; i--) {
                        Object obj = objArray[i];
                        if (obj != null && obj.Equals(value)) return i;
                    }
                }
            }
            else {
                for (int i = startIndex; i >= endIndex; i--) {
                    Object obj = array.GetValue(i);
                    if( obj == null) {
                        if(value == null) return i;
                    }
                    else {
                        if( obj.Equals(value)) return i;
                    }
                }
            }
            return lb-1;  // Return lb-1 for arrays with negative lower bounds.
        }
        
        public static int LastIndexOf<T>(T[] array, T value) {
            if (array==null) {
                throw new ArgumentNullException("array");
            }
            Contract.Ensures(Contract.Result<int>() < array.Length);
            Contract.EndContractBlock();

            return LastIndexOf(array, value, array.Length - 1, array.Length);
        }

        public static int LastIndexOf<T>(T[] array, T value, int startIndex) {
            if (array==null) {
                throw new ArgumentNullException("array");
            }
            Contract.Ensures(Contract.Result<int>() < array.Length);
            Contract.EndContractBlock();
            // if array is empty and startIndex is 0, we need to pass 0 as count
            return LastIndexOf(array, value, startIndex, (array.Length == 0)? 0 : (startIndex + 1));
        }

        public static int LastIndexOf<T>(T[] array, T value, int startIndex, int count) {
            if (array==null) {
                throw new ArgumentNullException("array");
            }
            Contract.Ensures(Contract.Result<int>() < array.Length);
            Contract.EndContractBlock();
        
            if(array.Length == 0) {
                //
                // Special case for 0 length List
                // accept -1 and 0 as valid startIndex for compablility reason.
                //
                if( startIndex != -1 && startIndex != 0) {
                    throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
                }

                // only 0 is a valid value for count if array is empty
                if( count != 0) {
                    throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
                }
                return -1;
            }

            // Make sure we're not out of range            
            if ( startIndex < 0 || startIndex >= array.Length) {
                throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ArgumentOutOfRange_Index"));
            }
            
            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0) {
                throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_Count"));
            }

            return EqualityComparer<T>.Default.LastIndexOf(array, value, startIndex, count);            
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private static extern bool TrySZLastIndexOf(Array sourceArray, int sourceIndex, int count, Object value, out int retVal);


        // Reverses all elements of the given array. Following a call to this
        // method, an element previously located at index i will now be
        // located at index length - i - 1, where length is the
        // length of the array.
        // 
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Reverse(Array array) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.EndContractBlock();
            Reverse(array, array.GetLowerBound(0), array.Length);
        }
        
        // Reverses the elements in a range of an array. Following a call to this
        // method, an element in the range given by index and count
        // which was previously located at index i will now be located at
        // index index + (index + count - i - 1).
        // Reliability note: This may fail because it may have to box objects.
        // 
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Reverse(Array array, int index, int length) {
            if (array==null) 
                throw new ArgumentNullException("array");
            int lowerBound = array.GetLowerBound(0);
            if (index < lowerBound || length < 0)
                throw new ArgumentOutOfRangeException((index<0 ? "index" : "length"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - (index - lowerBound) < length)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            if (array.Rank != 1)
                throw new RankException(Environment.GetResourceString("Rank_MultiDimNotSupported"));
            Contract.EndContractBlock();

            bool r = TrySZReverse(array, index, length);
            if (r)
                return;

            int i = index;
            int j = index + length - 1;
            Object[] objArray = array as Object[];
            if (objArray!=null) {
                while (i < j) {
                    Object temp = objArray[i];
                    objArray[i] = objArray[j];
                    objArray[j] = temp;
                    i++;
                    j--;
                }
            }
            else {
                while (i < j) {
                    Object temp = array.GetValue(i);
                    array.SetValue(array.GetValue(j), i);
                    array.SetValue(temp, j);
                    i++;
                    j--;
                }
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        private static extern bool TrySZReverse(Array array, int index, int count);
        
        // Sorts the elements of an array. The sort compares the elements to each
        // other using the IComparable interface, which must be implemented
        // by all elements of the array.
        // 
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort(Array array) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.EndContractBlock();
            Sort(array, null, array.GetLowerBound(0), array.Length, null);
        }
    
        // Sorts the elements of two arrays based on the keys in the first array.
        // Elements in the keys array specify the sort keys for
        // corresponding elements in the items array. The sort compares the
        // keys to each other using the IComparable interface, which must be
        // implemented by all elements of the keys array.
        // 
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort(Array keys, Array items) {
            if (keys==null)
                throw new ArgumentNullException("keys");
            Contract.EndContractBlock();
            Sort(keys, items, keys.GetLowerBound(0), keys.Length, null);
        }
        
        // Sorts the elements in a section of an array. The sort compares the
        // elements to each other using the IComparable interface, which
        // must be implemented by all elements in the given section of the array.
        // 
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort(Array array, int index, int length) {
            Sort(array, null, index, length, null);
        }
    
        // Sorts the elements in a section of two arrays based on the keys in the
        // first array. Elements in the keys array specify the sort keys for
        // corresponding elements in the items array. The sort compares the
        // keys to each other using the IComparable interface, which must be
        // implemented by all elements of the keys array.
        // 
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort(Array keys, Array items, int index, int length) {
            Sort(keys, items, index, length, null);
        }
        
        // Sorts the elements of an array. The sort compares the elements to each
        // other using the given IComparer interface. If comparer is
        // null, the elements are compared to each other using the
        // IComparable interface, which in that case must be implemented by
        // all elements of the array.
        // 
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort(Array array, IComparer comparer) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.EndContractBlock();
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
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort(Array keys, Array items, IComparer comparer) {
            if (keys==null)
                throw new ArgumentNullException("keys");
            Contract.EndContractBlock();
            Sort(keys, items, keys.GetLowerBound(0), keys.Length, comparer);
        }
        
        // Sorts the elements in a section of an array. The sort compares the
        // elements to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented
        // by all elements in the given section of the array.
        // 
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort(Array array, int index, int length, IComparer comparer) {
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
        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort(Array keys, Array items, int index, int length, IComparer comparer) {
            if (keys==null)
                throw new ArgumentNullException("keys");
            if (keys.Rank != 1 || (items != null && items.Rank != 1))
                throw new RankException(Environment.GetResourceString("Rank_MultiDimNotSupported"));
            int keysLowerBound = keys.GetLowerBound(0);
            if (items != null && keysLowerBound != items.GetLowerBound(0))
                throw new ArgumentException(Environment.GetResourceString("Arg_LowerBoundsMustMatch"));
            if (index < keysLowerBound || length < 0)
                throw new ArgumentOutOfRangeException((length<0 ? "length" : "index"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (keys.Length - (index - keysLowerBound) < length || (items != null && (index - keysLowerBound) > items.Length - length))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

            Contract.EndContractBlock();
            
            if (length > 1) {
                if (comparer == Comparer.Default || comparer == null) {
                    bool r = TrySZSort(keys, items, index, index + length - 1);
                    if (r)
                        return;
                }

                Object[] objKeys = keys as Object[];
                Object[] objItems = null;
                if (objKeys != null)
                    objItems = items as Object[];
                if (objKeys != null && (items==null || objItems != null)) {
                    SorterObjectArray sorter = new SorterObjectArray(objKeys, objItems, comparer);
                    sorter.Sort(index, length);
                }
                else {
                    SorterGenericArray sorter = new SorterGenericArray(keys, items, comparer);
                    sorter.Sort(index, length);
                }
            }
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        private static extern bool TrySZSort(Array keys, Array items, int left, int right);

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort<T>(T[] array) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.EndContractBlock();
            Sort<T>(array, 0, array.Length, null);
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items) {
            if (keys==null)
                throw new ArgumentNullException("keys");
            Contract.EndContractBlock();
            Sort<TKey, TValue>(keys, items, 0, keys.Length, null);
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort<T>(T[] array, int index, int length) {
            Sort<T>(array, index, length, null);
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, int index, int length) {
            Sort<TKey, TValue>(keys, items, index, length, null);
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort<T>(T[] array, System.Collections.Generic.IComparer<T> comparer) {
            if (array==null)
                throw new ArgumentNullException("array");
            Contract.EndContractBlock();
            Sort<T>(array, 0, array.Length, comparer);
        }

        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, System.Collections.Generic.IComparer<TKey> comparer) {
            if (keys==null)
                throw new ArgumentNullException("keys");
            Contract.EndContractBlock();
            Sort<TKey, TValue>(keys, items, 0, keys.Length, comparer);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort<T>(T[] array, int index, int length, System.Collections.Generic.IComparer<T> comparer) {
            if (array==null)
                throw new ArgumentNullException("array");
            if (index < 0 || length < 0)
                throw new ArgumentOutOfRangeException((length<0 ? "length" : "index"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (array.Length - index < length)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            if (length > 1) {
                if ( comparer == null || comparer == Comparer<T>.Default ) {
                    if(TrySZSort(array, null, index, index + length - 1)) {
                        return;
                    }
                }

                ArraySortHelper<T>.Default.Sort(array, index, length, comparer);                
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [ReliabilityContract(Consistency.MayCorruptInstance, Cer.MayFail)]
        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, int index, int length, System.Collections.Generic.IComparer<TKey> comparer) {
            if (keys==null)
                throw new ArgumentNullException("keys");
            if (index < 0 || length < 0)
                throw new ArgumentOutOfRangeException((length<0 ? "length" : "index"), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (keys.Length - index < length || (items != null && index > items.Length - length))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            if (length > 1) {
                if ( comparer == null || comparer == Comparer<TKey>.Default ) {
                    if(TrySZSort(keys, items, index, index + length - 1)) {
                        return;
                    }
                }
                
                if (items == null)
                {
                    Sort<TKey>(keys, index, length, comparer);
                    return;
                }

                ArraySortHelper<TKey, TValue>.Default.Sort(keys, items, index, length, comparer);
            }
        }

        public static void Sort<T>(T[] array, Comparison<T> comparison) {
            if( array == null) {
                throw new ArgumentNullException("array");
            }

            if( comparison == null) {
                throw new ArgumentNullException("comparison");
            }
            Contract.EndContractBlock();

            IComparer<T> comparer = Comparer<T>.Create(comparison);
            Array.Sort(array, comparer);
        }

        public static bool TrueForAll<T>(T[] array, Predicate<T> match) {
            if( array == null) {
                throw new ArgumentNullException("array");
            }

            if( match == null) {
                throw new ArgumentNullException("match");
            }
            Contract.EndContractBlock();

            for(int i = 0 ; i < array.Length; i++) {
                if( !match(array[i])) {
                    return false;
                }
            }
            return true;
        }


        // Private value type used by the Sort methods.
        private struct SorterObjectArray
        {
            private Object[] keys;
            private Object[] items;
            private IComparer comparer;    
    
            internal SorterObjectArray(Object[] keys, Object[] items, IComparer comparer) {
                if (comparer == null) comparer = Comparer.Default;
                this.keys = keys;
                this.items = items;
                this.comparer = comparer;
            }
    
            internal void SwapIfGreaterWithItems(int a, int b)
            {
                if (a != b)
                {
                    if (comparer.Compare(keys[a], keys[b]) > 0)
                    {
                        Object temp = keys[a];
                        keys[a] = keys[b];
                        keys[b] = temp;
                        if (items != null)
                        {
                            Object item = items[a];
                            items[a] = items[b];
                            items[b] = item;
                        }
                    }
                }
            }

            private void Swap(int i, int j)
            {
                Object t = keys[i];
                keys[i] = keys[j];
                keys[j] = t;

                if (items != null)
                {
                    Object item = items[i];
                    items[i] = items[j];
                    items[j] = item;
                }
            }

            internal void Sort(int left, int length)
            {
#if FEATURE_CORECLR
                // Since QuickSort and IntrospectiveSort produce different sorting sequence for equal keys the upgrade 
                // to IntrospectiveSort was quirked. However since the phone builds always shipped with the new sort aka 
                // IntrospectiveSort and we would want to continue using this sort moving forward CoreCLR always uses the new sort.

                IntrospectiveSort(left, length);
#else
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    IntrospectiveSort(left, length);
                }
                else
                {
                    DepthLimitedQuickSort(left, length + left - 1, IntrospectiveSortUtilities.QuickSortDepthThreshold);
                }
#endif
            }

            private void DepthLimitedQuickSort(int left, int right, int depthLimit)
            {
                // Can use the much faster jit helpers for array access.
                do
                {
                    if (depthLimit == 0)
                    {
                        // Add a try block here to detect IComparers (or their
                        // underlying IComparables, etc) that are bogus.
                        try
                        {
                            Heapsort(left, right);
                            return;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            throw new ArgumentException(Environment.GetResourceString("Arg_BogusIComparer", comparer));
                        }
                        catch (Exception e)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
                        }
                    }

                    int i = left;
                    int j = right;

                    // pre-sort the low, middle (pivot), and high values in place.
                    // this improves performance in the face of already sorted data, or 
                    // data that is made up of multiple sorted runs appended together.
                    int middle = GetMedian(i, j);

                    // Add a try block here to detect IComparers (or their
                    // underlying IComparables, etc) that are bogus.
                    try
                    {
                        SwapIfGreaterWithItems(i, middle); // swap the low with the mid point
                        SwapIfGreaterWithItems(i, j);      // swap the low with the high
                        SwapIfGreaterWithItems(middle, j); // swap the middle with the high
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
                    }
                    Object x = keys[middle];
                    do
                    {
                        // Add a try block here to detect IComparers (or their
                        // underlying IComparables, etc) that are bogus.
                        try
                        {
                            while (comparer.Compare(keys[i], x) < 0) i++;
                            while (comparer.Compare(x, keys[j]) < 0) j--;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            throw new ArgumentException(Environment.GetResourceString("Arg_BogusIComparer", comparer));
                        }
                        catch (Exception e)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
                        }
                        Contract.Assert(i >= left && j <= right, "(i>=left && j<=right)  Sort failed - Is your IComparer bogus?");
                        if (i > j) break;
                        if (i < j)
                        {
                            Object key = keys[i];
                            keys[i] = keys[j];
                            keys[j] = key;
                            if (items != null)
                            {
                                Object item = items[i];
                                items[i] = items[j];
                                items[j] = item;
                            }
                        }
                        i++;
                        j--;
                    } while (i <= j);

                    // The next iteration of the while loop is to "recursively" sort the larger half of the array and the
                    // following calls recursively sort the smaller half.  So we subtract one from depthLimit here so
                    // both sorts see the new value.
                    depthLimit--;

                    if (j - left <= right - i)
                    {
                        if (left < j) DepthLimitedQuickSort(left, j, depthLimit);
                        left = i;
                    }
                    else
                    {
                        if (i < right) DepthLimitedQuickSort(i, right, depthLimit);
                        right = j;
                    }
                } while (left < right);
            }

            private void IntrospectiveSort(int left, int length)
            {
                if (length < 2)
                    return;

                try
                {
                    IntroSort(left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Length));
                }
                catch (IndexOutOfRangeException)
                {
                    IntrospectiveSortUtilities.ThrowOrIgnoreBadComparer(comparer);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
                }
            }

            private void IntroSort(int lo, int hi, int depthLimit)
            {
                while (hi > lo)
                {
                    int partitionSize = hi - lo + 1;
                    if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                    {
                        if (partitionSize == 1)
                        {
                            return;
                        }
                        if (partitionSize == 2)
                        {
                            SwapIfGreaterWithItems(lo, hi);
                            return;
                        }
                        if (partitionSize == 3)
                        {
                            SwapIfGreaterWithItems(lo, hi-1);
                            SwapIfGreaterWithItems(lo, hi);
                            SwapIfGreaterWithItems(hi-1, hi);
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
                // Compute median-of-three.  But also partition them, since we've done the comparison.
                int mid = lo + (hi - lo) / 2;
                // Sort lo, mid and hi appropriately, then pick mid as the pivot.
                SwapIfGreaterWithItems(lo, mid);
                SwapIfGreaterWithItems(lo, hi);
                SwapIfGreaterWithItems(mid, hi);

                Object pivot = keys[mid];
                Swap(mid, hi - 1);
                int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

                while (left < right)
                {
                    while (comparer.Compare(keys[++left], pivot) < 0) ;
                    while (comparer.Compare(pivot, keys[--right]) < 0) ;

                    if(left >= right)
                        break;

                    Swap(left, right);
                }

                // Put pivot in the right location.
                Swap(left, (hi - 1));
                return left;
            }

            private void Heapsort(int lo, int hi)
            {
                int n = hi - lo + 1;
                for (int i = n / 2; i >= 1; i = i - 1)
                {
                    DownHeap(i, n, lo);
                }
                for (int i = n; i > 1; i = i - 1)
                {
                    Swap(lo, lo + i - 1);

                    DownHeap(1, i - 1, lo);
                }
            }

            private void DownHeap(int i, int n, int lo)
            {
                Object d = keys[lo + i - 1];
                Object dt = (items != null) ? items[lo + i - 1] : null;
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
                    if(items != null)
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
                Object t, ti;
                for (i = lo; i < hi; i++)
                {
                    j = i;
                    t = keys[i + 1];
                    ti = (items != null) ? items[i + 1] : null;
                    while (j >= lo && comparer.Compare(t, keys[j]) < 0)
                    {
                        keys[j + 1] = keys[j];
                        if(items != null)
                            items[j + 1] = items[j];
                        j--;
                    }
                    keys[j + 1] = t;
                    if(items != null)
                        items[j + 1] = ti;
                }
            }
        }
    
        // Private value used by the Sort methods for instances of Array.
        // This is slower than the one for Object[], since we can't use the JIT helpers
        // to access the elements.  We must use GetValue & SetValue.
        private struct SorterGenericArray
        {
            private Array keys;
            private Array items;
            private IComparer comparer;

            internal SorterGenericArray(Array keys, Array items, IComparer comparer)
            {
                if (comparer == null) comparer = Comparer.Default;
                this.keys = keys;
                this.items = items;
                this.comparer = comparer;
            }

            internal void SwapIfGreaterWithItems(int a, int b)
            {
                if (a != b)
                {
                    if (comparer.Compare(keys.GetValue(a), keys.GetValue(b)) > 0)
                    {
                        Object key = keys.GetValue(a);
                        keys.SetValue(keys.GetValue(b), a);
                        keys.SetValue(key, b);
                        if (items != null)
                        {
                            Object item = items.GetValue(a);
                            items.SetValue(items.GetValue(b), a);
                            items.SetValue(item, b);
                        }
                    }
                }
            }

            private void Swap(int i, int j)
            {
                Object t1 = keys.GetValue(i);
                keys.SetValue(keys.GetValue(j), i);
                keys.SetValue(t1, j);
                
                if(items != null)
                {
                    Object t2 = items.GetValue(i);
                    items.SetValue(items.GetValue(j), i);
                    items.SetValue(t2, j);
                }
            }

            internal void Sort(int left, int length)
            {
#if FEATURE_CORECLR
                // Since QuickSort and IntrospectiveSort produce different sorting sequence for equal keys the upgrade 
                // to IntrospectiveSort was quirked. However since the phone builds always shipped with the new sort aka 
                // IntrospectiveSort and we would want to continue using this sort moving forward CoreCLR always uses the new sort.

                IntrospectiveSort(left, length);
#else
                if (BinaryCompatibility.TargetsAtLeast_Desktop_V4_5)
                {
                    IntrospectiveSort(left, length);
                }
                else
                {
                    DepthLimitedQuickSort(left, length + left - 1, IntrospectiveSortUtilities.QuickSortDepthThreshold);
                }
#endif
            }

            private void DepthLimitedQuickSort(int left, int right, int depthLimit)
            {
                // Must use slow Array accessors (GetValue & SetValue)
                do
                {
                    if (depthLimit == 0)
                    {
                        // Add a try block here to detect IComparers (or their
                        // underlying IComparables, etc) that are bogus.
                        try
                        {
                            Heapsort(left, right);
                            return;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            throw new ArgumentException(Environment.GetResourceString("Arg_BogusIComparer", comparer));
                        }
                        catch (Exception e)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
                        }
                    }

                    int i = left;
                    int j = right;

                    // pre-sort the low, middle (pivot), and high values in place.
                    // this improves performance in the face of already sorted data, or 
                    // data that is made up of multiple sorted runs appended together.
                    int middle = GetMedian(i, j);
                    try
                    {
                        SwapIfGreaterWithItems(i, middle); // swap the low with the mid point
                        SwapIfGreaterWithItems(i, j);      // swap the low with the high
                        SwapIfGreaterWithItems(middle, j); // swap the middle with the high
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
                    }

                    Object x = keys.GetValue(middle);
                    do
                    {
                        // Add a try block here to detect IComparers (or their
                        // underlying IComparables, etc) that are bogus.
                        try
                        {
                            while (comparer.Compare(keys.GetValue(i), x) < 0) i++;
                            while (comparer.Compare(x, keys.GetValue(j)) < 0) j--;
                        }
                        catch (IndexOutOfRangeException)
                        {
                            throw new ArgumentException(Environment.GetResourceString("Arg_BogusIComparer", comparer));
                        }
                        catch (Exception e)
                        {
                            throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
                        }
                        Contract.Assert(i >= left && j <= right, "(i>=left && j<=right)  Sort failed - Is your IComparer bogus?");
                        if (i > j) break;
                        if (i < j)
                        {
                            Object key = keys.GetValue(i);
                            keys.SetValue(keys.GetValue(j), i);
                            keys.SetValue(key, j);
                            if (items != null)
                            {
                                Object item = items.GetValue(i);
                                items.SetValue(items.GetValue(j), i);
                                items.SetValue(item, j);
                            }
                        }
                        if (i != Int32.MaxValue) ++i;
                        if (j != Int32.MinValue) --j;
                    } while (i <= j);

                    // The next iteration of the while loop is to "recursively" sort the larger half of the array and the
                    // following calls recursively sort the smaller half.  So we subtract one from depthLimit here so
                    // both sorts see the new value.
                    depthLimit--;

                    if (j - left <= right - i)
                    {
                        if (left < j) DepthLimitedQuickSort(left, j, depthLimit);
                        left = i;
                    }
                    else
                    {
                        if (i < right) DepthLimitedQuickSort(i, right, depthLimit);
                        right = j;
                    }
                } while (left < right);
            }

            private void IntrospectiveSort(int left, int length)
            {
                if (length < 2)
                    return;

                try
                {
                    IntroSort(left, length + left - 1, 2 * IntrospectiveSortUtilities.FloorLog2(keys.Length));
                }
                catch (IndexOutOfRangeException)
                {
                    IntrospectiveSortUtilities.ThrowOrIgnoreBadComparer(comparer);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_IComparerFailed"), e);
                }
            }

            private void IntroSort(int lo, int hi, int depthLimit)
            {
                while (hi > lo)
                {
                    int partitionSize = hi - lo + 1;
                    if (partitionSize <= IntrospectiveSortUtilities.IntrosortSizeThreshold)
                    {
                        if (partitionSize == 1)
                        {
                            return;
                        }
                        if (partitionSize == 2)
                        {
                            SwapIfGreaterWithItems(lo, hi);
                            return;
                        }
                        if (partitionSize == 3)
                        {
                            SwapIfGreaterWithItems(lo, hi-1);
                            SwapIfGreaterWithItems(lo, hi);
                            SwapIfGreaterWithItems(hi-1, hi);
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
                // Compute median-of-three.  But also partition them, since we've done the comparison.
                int mid = lo + (hi - lo) / 2;

                SwapIfGreaterWithItems(lo, mid);
                SwapIfGreaterWithItems(lo, hi);
                SwapIfGreaterWithItems(mid, hi);

                Object pivot = keys.GetValue(mid);
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
                Swap(left, (hi - 1));
                return left;
            }

            private void Heapsort(int lo, int hi)
            {
                int n = hi - lo + 1;
                for (int i = n / 2; i >= 1; i = i - 1)
                {
                    DownHeap(i, n, lo);
                }
                for (int i = n; i > 1; i = i - 1)
                {
                    Swap(lo, lo + i - 1);

                    DownHeap(1, i - 1, lo);
                }
            }

            private void DownHeap(int i, int n, int lo)
            {
                Object d = keys.GetValue(lo + i - 1);
                Object dt = (items != null)? items.GetValue(lo + i - 1) : null;
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
                    if(items != null)
                        items.SetValue(items.GetValue(lo + child - 1), lo + i - 1);
                    i = child;
                }
                keys.SetValue(d, lo + i - 1);
                if(items != null)
                    items.SetValue(dt, lo + i - 1);
            }

            private void InsertionSort(int lo, int hi)
            {
                int i, j;
                Object t, dt;
                for (i = lo; i < hi; i++)
                {
                    j = i;
                    t = keys.GetValue(i + 1);
                    dt = (items != null)? items.GetValue(i + 1) : null;

                    while (j >= lo && comparer.Compare(t, keys.GetValue(j)) < 0)
                    {
                        keys.SetValue(keys.GetValue(j), j + 1);
                        if(items != null)
                            items.SetValue(items.GetValue(j), j + 1);
                        j--;
                    }

                    keys.SetValue(t, j + 1);
                    if(items != null)
                        items.SetValue(dt, j + 1);
                }
            }
        }

        [Serializable] private sealed class SZArrayEnumerator : IEnumerator, ICloneable
        {
            private Array _array;
            private int _index;
            private int _endIndex; // cache array length, since it's a little slow.

            internal SZArrayEnumerator(Array array) {
                Contract.Assert(array.Rank == 1 && array.GetLowerBound(0) == 0, "SZArrayEnumerator only works on single dimension arrays w/ a lower bound of zero.");
                _array = array;
                _index = -1;
                _endIndex = array.Length;
            }

            public Object Clone()
            {
                return MemberwiseClone();
            }

            public bool MoveNext() {
                if (_index < _endIndex) {
                    _index++;
                    return (_index < _endIndex);
                }
                return false;
            }
    
            public Object Current {
                get {
                    if (_index < 0) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    if (_index >= _endIndex) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                    return _array.GetValue(_index);
                }
            }
    
            public void Reset() {
                _index = -1;
            }
        }
        
        [Serializable] private sealed class ArrayEnumerator : IEnumerator, ICloneable
        {
            private Array array;
            private int index;
            private int endIndex;
            private int startIndex;    // Save for Reset.
            private int[] _indices;    // The current position in a multidim array
            private bool _complete;

            internal ArrayEnumerator(Array array, int index, int count) {
                this.array = array;
                this.index = index - 1;
                startIndex = index;
                endIndex = index + count;
                _indices = new int[array.Rank];
                int checkForZero = 1;  // Check for dimensions of size 0.
                for(int i=0; i<array.Rank; i++) {
                    _indices[i] = array.GetLowerBound(i);
                    checkForZero *= array.GetLength(i);
                }
                // To make MoveNext simpler, decrement least significant index.
                _indices[_indices.Length-1]--;
                _complete = (checkForZero == 0);
            }

            private void IncArray() {
                // This method advances us to the next valid array index,
                // handling all the multiple dimension & bounds correctly.
                // Think of it like an odometer in your car - we start with
                // the last digit, increment it, and check for rollover.  If
                // it rolls over, we set all digits to the right and including 
                // the current to the appropriate lower bound.  Do these overflow
                // checks for each dimension, and if the most significant digit 
                // has rolled over it's upper bound, we're done.
                //
                int rank = array.Rank;
                _indices[rank-1]++;
                for(int dim=rank-1; dim>=0; dim--) {
                    if (_indices[dim] > array.GetUpperBound(dim)) {
                        if (dim==0) {
                            _complete = true;
                            break;
                        }
                        for(int j=dim; j<rank; j++)
                            _indices[j] = array.GetLowerBound(j);
                        _indices[dim-1]++;
                    }
                }
            }

            public Object Clone()
            {
                return MemberwiseClone();
            }

            public bool MoveNext() {
                if (_complete) {
                    index = endIndex;
                    return false;
                }
                index++;
                IncArray();
                return !_complete;
            }
    
            public Object Current {
                get {
                    if (index < startIndex) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    if (_complete) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                    return array.GetValue(_indices);
                }
            }
    
            public void Reset() {
                index = startIndex - 1;
                int checkForZero = 1;
                for(int i=0; i<array.Rank; i++) {
                    _indices[i] = array.GetLowerBound(i);
                    checkForZero *= array.GetLength(i);
                }
                _complete = (checkForZero == 0);
                // To make MoveNext simpler, decrement least significant index.
                _indices[_indices.Length-1]--;
            }
        }


        // if this is an array of value classes and that value class has a default constructor 
        // then this calls this default constructor on every element in the value class array.
        // otherwise this is a no-op.  Generally this method is called automatically by the compiler
        [System.Security.SecuritySafeCritical]  // auto-generated
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
    sealed class SZArrayHelper {
        // It is never legal to instantiate this class.
        private SZArrayHelper() {
            Contract.Assert(false, "Hey! How'd I get here?");
        }

        // -----------------------------------------------------------
        // ------- Implement IEnumerable<T> interface methods --------
        // -----------------------------------------------------------
        [SecuritySafeCritical]
        internal IEnumerator<T> GetEnumerator<T>() {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
            T[] _this = JitHelpers.UnsafeCast<T[]>(this);
            int length = _this.Length;
            return length == 0 ? SZGenericArrayEnumerator<T>.Empty : new SZGenericArrayEnumerator<T>(_this, length);
        }

        // -----------------------------------------------------------
        // ------- Implement ICollection<T> interface methods --------
        // -----------------------------------------------------------
        [SecuritySafeCritical]
        void CopyTo<T>(T[] array, int index) {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!

            T[] _this = JitHelpers.UnsafeCast<T[]>(this);
            Array.Copy(_this, 0, array, index, _this.Length);
        }

        [SecuritySafeCritical]
        internal int get_Count<T>() {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
            T[] _this = JitHelpers.UnsafeCast<T[]>(this);
            return _this.Length;
        }

        // -----------------------------------------------------------
        // ---------- Implement IList<T> interface methods -----------
        // -----------------------------------------------------------
        [SecuritySafeCritical]
        internal T get_Item<T>(int index) {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
            T[] _this = JitHelpers.UnsafeCast<T[]>(this);
            if ((uint)index >= (uint)_this.Length) {
                ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            return _this[index];
        }

        [SecuritySafeCritical]
        internal void set_Item<T>(int index, T value) {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
            T[] _this = JitHelpers.UnsafeCast<T[]>(this);
            if ((uint)index >= (uint)_this.Length) {
                ThrowHelper.ThrowArgumentOutOfRangeException();
            }

            _this[index] = value;
        }

        void Add<T>(T value) {
            // Not meaningful for arrays.
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
        }

        [SecuritySafeCritical]
        bool Contains<T>(T value) {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
            T[] _this = JitHelpers.UnsafeCast<T[]>(this);
            return Array.IndexOf(_this, value) != -1;
        }
        
        bool get_IsReadOnly<T>() {
            return true; 
        }
        
        void Clear<T>() {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_ReadOnlyCollection"));                        
        }

        [SecuritySafeCritical]
        int IndexOf<T>(T value) {
            //! Warning: "this" is an array, not an SZArrayHelper. See comments above
            //! or you may introduce a security hole!
            T[] _this = JitHelpers.UnsafeCast<T[]>(this);
            return Array.IndexOf(_this, value);
        }
        
        void Insert<T>(int index, T value) {
            // Not meaningful for arrays
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
        }

        bool Remove<T>(T value) {
            // Not meaningful for arrays
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
        }

        void RemoveAt<T>(int index) {
            // Not meaningful for arrays
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_FixedSizeCollection"));
        }

        // This is a normal generic Enumerator for SZ arrays. It doesn't have any of the "this" stuff
        // that SZArrayHelper does.
        //
        [Serializable] private sealed class SZGenericArrayEnumerator<T> : IEnumerator<T> {
            private T[] _array;
            private int _index;
            private int _endIndex; // cache array length, since it's a little slow.

            // Passing -1 for endIndex so that MoveNext always returns false without mutating _index
            internal static readonly SZGenericArrayEnumerator<T> Empty = new SZGenericArrayEnumerator<T>(null, -1);

            internal SZGenericArrayEnumerator(T[] array, int endIndex) {
                // We allow passing null array in case of empty enumerator. 
                Contract.Assert(array != null || endIndex == -1, "endIndex should be -1 in the case of a null array (for the empty enumerator).");
                _array = array;
                _index = -1;
                _endIndex = endIndex;
            }
    
            public bool MoveNext() {
                if (_index < _endIndex) {
                    _index++;
                    return (_index < _endIndex);
                }
                return false;
            }
    
            public T Current {
                get {
                    if (_index < 0) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumNotStarted));
                    if (_index >= _endIndex) throw new InvalidOperationException(Environment.GetResourceString(ResId.InvalidOperation_EnumEnded));
                    return _array[_index];
                }
            }
    
            object IEnumerator.Current {
                get {
                    return Current;
                }
            }

            void IEnumerator.Reset() {
                _index = -1;
            }

            public void Dispose()
            {
            }
        }
    }
}

// Useful in number of places that return an empty byte array to avoid unnecessary memory allocation.
internal static class EmptyArray<T>
{
    public static readonly T[] Value = new T[0];
}
