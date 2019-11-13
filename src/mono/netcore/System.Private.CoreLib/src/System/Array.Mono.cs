// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono;
#if BIT64
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

namespace System
{
	partial class Array
	{
		[StructLayout(LayoutKind.Sequential)]
		private class RawData
		{
			public IntPtr Bounds;
			public IntPtr Count;
			public byte Data;
		}

		public int Length {
			get {
				int length = GetLength (0);

				for (int i = 1; i < Rank; i++) {
					length *= GetLength (i);
				}
				return length;
			}
		}

		public long LongLength {
			get {
				long length = GetLength (0);

				for (int i = 1; i < Rank; i++) {
					length *= GetLength (i);
				}
				return length;
			}
		}

		public int Rank {
			get {
				return GetRank ();
			}
		}

		public static unsafe void Clear (Array array, int index, int length)
		{
			if (array == null)
				ThrowHelper.ThrowArgumentNullException (ExceptionArgument.array);

			int lowerBound = array.GetLowerBound (0);
			int elementSize = array.GetElementSize ();
			nuint numComponents = (nuint) Unsafe.As<RawData> (array).Count;

			int offset = index - lowerBound;

			if (index < lowerBound || offset < 0 || length < 0 || (uint) (offset + length) > numComponents)
				ThrowHelper.ThrowIndexOutOfRangeException ();

			ref byte ptr = ref Unsafe.AddByteOffset (ref array.GetRawSzArrayData(), (uint) offset * (nuint) elementSize);
			nuint byteLength = (uint) length * (nuint) elementSize;

			if (RuntimeHelpers.ObjectHasReferences (array))
				SpanHelpers.ClearWithReferences (ref Unsafe.As<byte, IntPtr> (ref ptr), byteLength / (uint)sizeof (IntPtr));
			else
				SpanHelpers.ClearWithoutReferences (ref ptr, byteLength);
		}

		public static void ConstrainedCopy (Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
		{
			Copy (sourceArray, sourceIndex, destinationArray, destinationIndex, length, true);
		}

		public static void Copy (Array sourceArray, Array destinationArray, int length)
		{
			if (sourceArray == null)
				throw new ArgumentNullException ("sourceArray");

			if (destinationArray == null)
				throw new ArgumentNullException ("destinationArray");

			Copy (sourceArray, sourceArray.GetLowerBound (0), destinationArray,
				destinationArray.GetLowerBound (0), length);
		}

		public static void Copy (Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
		{
			Copy (sourceArray, sourceIndex, destinationArray, destinationIndex, length, false);
		}

		private static void Copy (Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length, bool reliable)
		{
			if (sourceArray == null)
				throw new ArgumentNullException (nameof (sourceArray));

			if (destinationArray == null)
				throw new ArgumentNullException (nameof (destinationArray));

			if (length < 0)
				throw new ArgumentOutOfRangeException (nameof (length), "Value has to be >= 0.");

			if (sourceArray.Rank != destinationArray.Rank)
				throw new RankException(SR.Rank_MultiDimNotSupported);

			if (sourceIndex < 0)
				throw new ArgumentOutOfRangeException (nameof (sourceIndex), "Value has to be >= 0.");

			if (destinationIndex < 0)
				throw new ArgumentOutOfRangeException (nameof (destinationIndex), "Value has to be >= 0.");

			if (FastCopy (sourceArray, sourceIndex, destinationArray, destinationIndex, length))
				return;

			int source_pos = sourceIndex - sourceArray.GetLowerBound (0);
			int dest_pos = destinationIndex - destinationArray.GetLowerBound (0);

			if (source_pos < 0)
				throw new ArgumentOutOfRangeException (nameof (sourceIndex), "Index was less than the array's lower bound in the first dimension.");

			if (dest_pos < 0)
				throw new ArgumentOutOfRangeException (nameof (destinationIndex), "Index was less than the array's lower bound in the first dimension.");

			// re-ordered to avoid possible integer overflow
			if (source_pos > sourceArray.Length - length)
				throw new ArgumentException (SR.Arg_LongerThanSrcArray, nameof (sourceArray));

			if (dest_pos > destinationArray.Length - length) {
				throw new ArgumentException ("Destination array was not long enough. Check destIndex and length, and the array's lower bounds", nameof (destinationArray));
			}

			Type src_type = sourceArray.GetType ().GetElementType ()!;
			Type dst_type = destinationArray.GetType ().GetElementType ()!;
			var dst_type_vt = dst_type.IsValueType && Nullable.GetUnderlyingType (dst_type) == null;

			bool src_is_enum = src_type.IsEnum;
			bool dst_is_enum = dst_type.IsEnum;
			
			if (src_is_enum)
				src_type = Enum.GetUnderlyingType (src_type);
			if (dst_is_enum)
				dst_type = Enum.GetUnderlyingType (dst_type);

			if (reliable) {
				if (!dst_type.Equals (src_type) &&
					!(dst_type.IsPrimitive && src_type.IsPrimitive && CanChangePrimitive(ref dst_type, ref src_type, true))) {
					throw new ArrayTypeMismatchException (SR.ArrayTypeMismatch_CantAssignType);
				}
			} else {
				if (!CanAssignArrayElement (src_type, dst_type)) {
					throw new ArrayTypeMismatchException (SR.ArrayTypeMismatch_CantAssignType);
				}
			}

			if (!Object.ReferenceEquals (sourceArray, destinationArray) || source_pos > dest_pos) {
				for (int i = 0; i < length; i++) {
					Object srcval = sourceArray.GetValueImpl (source_pos + i);

					if (!src_type.IsValueType && dst_is_enum)
						throw new InvalidCastException (SR.InvalidCast_DownCastArrayElement);

					if (dst_type_vt && (srcval == null || (src_type == typeof (object) && srcval.GetType () != dst_type)))
						throw new InvalidCastException ();

					try {
						destinationArray.SetValueRelaxedImpl (srcval, dest_pos + i);
					} catch (ArgumentException) {
						throw CreateArrayTypeMismatchException ();
					}
				}
			} else {
				for (int i = length - 1; i >= 0; i--) {
					Object srcval = sourceArray.GetValueImpl (source_pos + i);

					try {
						destinationArray.SetValueRelaxedImpl (srcval, dest_pos + i);
					} catch (ArgumentException) {
						throw CreateArrayTypeMismatchException ();
					}
				}
			}
		}

		static ArrayTypeMismatchException CreateArrayTypeMismatchException ()
		{
			return new ArrayTypeMismatchException ();
		}

		static bool CanAssignArrayElement (Type source, Type target)
		{
			if (!target.IsValueType && !target.IsPointer) {
				if (!source.IsValueType && !source.IsPointer) {
					// Reference to reference copy
					return
						source.IsInterface || target.IsInterface ||
						source.IsAssignableFrom (target) || target.IsAssignableFrom (source);
				} else {
					// Value to reference copy
					if (source.IsPointer)
						return false;
					return target.IsAssignableFrom (source);
				}
			} else {
				if (source.IsEquivalentTo (target)) {
					return true;
				} else if (source.IsPointer && target.IsPointer) {
					return true;
				} else if (source.IsPrimitive && target.IsPrimitive) {
					
					// Allow primitive type widening
					return CanChangePrimitive (ref source, ref target, false);
				} else if (!source.IsValueType && !source.IsPointer) {
					// Source is base class or interface of destination type
					if (target.IsPointer)
						return false;
					return source.IsAssignableFrom (target);
				}
			}

			return false;
		}

		public static unsafe Array CreateInstance (Type elementType, int length)
		{
			if (elementType is null)
				ThrowHelper.ThrowArgumentNullException (ExceptionArgument.elementType);
			if (length < 0)
				ThrowHelper.ThrowLengthArgumentOutOfRange_ArgumentOutOfRange_NeedNonNegNum ();

			RuntimeType? runtimeType = elementType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

			Array array = null;
			InternalCreate (ref array, runtimeType._impl.Value, 1, &length, null);
			GC.KeepAlive (runtimeType);
			return array;
		}

		public static unsafe Array CreateInstance (Type elementType, int length1, int length2)
		{
			if (elementType is null)
				ThrowHelper.ThrowArgumentNullException (ExceptionArgument.elementType);
			if (length1 < 0)
				ThrowHelper.ThrowArgumentOutOfRangeException (ExceptionArgument.length1, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			if (length2 < 0)
				ThrowHelper.ThrowArgumentOutOfRangeException (ExceptionArgument.length2, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

			RuntimeType? runtimeType = elementType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

			int* lengths = stackalloc int [] { length1, length2 };
			Array array = null;
			InternalCreate (ref array, runtimeType._impl.Value, 2, lengths, null);
			GC.KeepAlive (runtimeType);
			return array;
		}

		public static unsafe Array CreateInstance (Type elementType, int length1, int length2, int length3)
		{
			if (elementType is null)
				ThrowHelper.ThrowArgumentNullException (ExceptionArgument.elementType);
			if (length1 < 0)
				ThrowHelper.ThrowArgumentOutOfRangeException (ExceptionArgument.length1, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			if (length2 < 0)
				ThrowHelper.ThrowArgumentOutOfRangeException (ExceptionArgument.length2, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			if (length3 < 0)
				ThrowHelper.ThrowArgumentOutOfRangeException (ExceptionArgument.length3, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

			RuntimeType? runtimeType = elementType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

			int* lengths = stackalloc int [] { length1, length2, length3 };
			Array array = null;
			InternalCreate (ref array, runtimeType._impl.Value, 3, lengths, null);
			GC.KeepAlive (runtimeType);
			return array;
		}

		public static unsafe Array CreateInstance (Type elementType, params int[] lengths)
		{
			if (elementType is null)
				ThrowHelper.ThrowArgumentNullException (ExceptionArgument.elementType);
			if (lengths == null)
				ThrowHelper.ThrowArgumentNullException (ExceptionArgument.lengths);
			if (lengths.Length == 0)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_NeedAtLeast1Rank);

			RuntimeType? runtimeType = elementType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

			for (int i = 0; i < lengths.Length; i++)
				if (lengths [i] < 0)
					ThrowHelper.ThrowArgumentOutOfRangeException (ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

			Array array = null;
			fixed (int* pLengths = &lengths [0])
				InternalCreate (ref array, runtimeType._impl.Value, lengths.Length, pLengths, null);
			GC.KeepAlive (runtimeType);
			return array;
		}

		public static unsafe Array CreateInstance (Type elementType, int[] lengths, int[] lowerBounds)
		{
			if (elementType == null)
				ThrowHelper.ThrowArgumentNullException (ExceptionArgument.elementType);
			if (lengths == null)
				ThrowHelper.ThrowArgumentNullException (ExceptionArgument.lengths);
			if (lowerBounds == null)
				ThrowHelper.ThrowArgumentNullException (ExceptionArgument.lowerBounds);
			if (lengths.Length != lowerBounds!.Length)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_RanksAndBounds);
			if (lengths.Length == 0)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_NeedAtLeast1Rank);

			for (int i = 0; i < lengths.Length; i++)
				if (lengths [i] < 0)
					ThrowHelper.ThrowArgumentOutOfRangeException (ExceptionArgument.lengths, i, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);

			RuntimeType? runtimeType = elementType.UnderlyingSystemType as RuntimeType;
			if (runtimeType == null)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_MustBeType, ExceptionArgument.elementType);

			Array array = null;
			fixed (int* pLengths = &lengths [0])
			fixed (int* pLowerBounds = &lowerBounds [0])
				InternalCreate (ref array, runtimeType._impl.Value, lengths.Length, pLengths, pLowerBounds);
			GC.KeepAlive (runtimeType);
			return array;
		}

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern unsafe void InternalCreate (ref Array result, IntPtr elementType, int rank, int* lengths, int* lowerBounds);

		public object GetValue (int index)
		{
			if (Rank != 1)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_Need1DArray);

			var lb  = GetLowerBound (0);
			if (index < lb || index > GetUpperBound (0))
				throw new IndexOutOfRangeException ("Index has to be between upper and lower bound of the array.");

			if (GetType ().GetElementType ()!.IsPointer)
				throw new NotSupportedException (SR.NotSupported_Type);

			return GetValueImpl (index - lb);
		}

		public object GetValue (int index1, int index2)
		{
			if (Rank != 2)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_Need2DArray);

			int[] ind = {index1, index2};
			return GetValue (ind);
		}

		public object GetValue (int index1, int index2, int index3)
		{
			if (Rank != 3)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_Need3DArray);

			int[] ind = {index1, index2, index3};
			return GetValue (ind);
		}

		public void Initialize ()
		{
		}

		static int IndexOfImpl<T>(T[] array, T value, int startIndex, int count)
		{
			return EqualityComparer<T>.Default.IndexOf (array, value, startIndex, count);
		}

		static int LastIndexOfImpl<T>(T[] array, T value, int startIndex, int count)
		{
			return EqualityComparer<T>.Default.LastIndexOf (array, value, startIndex, count);
		}

		public void SetValue (object? value, int index)
		{
			if (Rank != 1)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_Need1DArray);

			var lb  = GetLowerBound (0);
			if (index < lb || index > GetUpperBound (0))
				throw new IndexOutOfRangeException ("Index has to be >= lower bound and <= upper bound of the array.");

			if (GetType ().GetElementType ()!.IsPointer)
				throw new NotSupportedException (SR.NotSupported_Type);

			SetValueImpl (value, index - lb);
		}

		public void SetValue (object? value, int index1, int index2)
		{
			if (Rank != 2)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_Need2DArray);

			int[] ind = {index1, index2};
			SetValue (value, ind);
		}

		public void SetValue (object? value, int index1, int index2, int index3)
		{
			if (Rank != 3)
				ThrowHelper.ThrowArgumentException (ExceptionResource.Arg_Need3DArray);

			int[] ind = {index1, index2, index3};
			SetValue (value, ind);
		}

		static bool TrySZBinarySearch (Array sourceArray, int sourceIndex, int count, object? value, out int retVal)
		{
			retVal = default;
			return false;
		}

		static bool TrySZIndexOf (Array sourceArray, int sourceIndex, int count, object? value, out int retVal)
		{
			retVal = default;
			return false;
		}

		static bool TrySZLastIndexOf (Array sourceArray, int sourceIndex, int count, object? value, out int retVal)
		{
			retVal = default;
			return false;
		}

		static bool TrySZReverse (Array array, int index, int count) => false;

		public int GetUpperBound (int dimension)
		{
			return GetLowerBound (dimension) + GetLength (dimension) - 1;
		}

		[Intrinsic]
		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		internal ref byte GetRawSzArrayData ()
		{
			return ref Unsafe.As<RawData>(this).Data;
		}

		[Intrinsic]
		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		internal ref byte GetRawArrayData ()
		{
			return ref Unsafe.As<RawData>(this).Data;
		}

		[Intrinsic]
		internal int GetElementSize ()
		{
			ThrowHelper.ThrowNotSupportedException ();
			return 0;
		}

		[Intrinsic]
		public bool IsPrimitive ()
		{
			ThrowHelper.ThrowNotSupportedException ();
			return false;
		}

		[MethodImpl(MethodImplOptions.InternalCall)]
		internal extern CorElementType GetCorElementTypeOfElementType();

		[MethodImpl(MethodImplOptions.InternalCall)]
		extern bool IsValueOfElementType(object value);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static bool CanChangePrimitive (ref Type srcType, ref Type dstType, bool reliable);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool FastCopy (Array source, int source_idx, Array dest, int dest_idx, int length);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern int GetRank ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern int GetLength (int dimension);

		[Intrinsic] // when dimension is `0` constant
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern int GetLowerBound (int dimension);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern object GetValue (params int[] indices);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern void SetValue (object? value, params int[] indices);

		// CAUTION! No bounds checking!
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void GetGenericValue_icall<T> (ref Array self, int pos, out T value);

		// CAUTION! No bounds checking!
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern object GetValueImpl (int pos);

		// CAUTION! No bounds checking!
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void SetGenericValue_icall<T> (ref Array self, int pos, ref T value);

		// This is a special case in the runtime.
		void GetGenericValueImpl<T> (int pos, out T value)
		{
			var self = this;
			GetGenericValue_icall (ref self, pos, out value);
		}

		// This is a special case in the runtime.
		void SetGenericValueImpl<T> (int pos, ref T value)
		{
			var self = this;
			SetGenericValue_icall (ref self, pos, ref value);
		}

		// CAUTION! No bounds checking!
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern void SetValueImpl (object? value, int pos);

		// CAUTION! No bounds checking!
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern void SetValueRelaxedImpl (object? value, int pos);

		/*
		 * These methods are used to implement the implicit generic interfaces 
		 * implemented by arrays in NET 2.0.
		 * Only make those methods generic which really need it, to avoid
		 * creating useless instantiations.
		 */
		internal int InternalArray__ICollection_get_Count ()
		{
			return Length;
		}

		internal bool InternalArray__ICollection_get_IsReadOnly ()
		{
			return true;
		}

		internal IEnumerator<T> InternalArray__IEnumerable_GetEnumerator<T> ()
		{
			return Length == 0 ? SZGenericArrayEnumerator<T>.Empty : new SZGenericArrayEnumerator<T> (Unsafe.As<T[]> (this));
		}

		internal void InternalArray__ICollection_Clear ()
		{
			ThrowHelper.ThrowNotSupportedException (ExceptionResource.NotSupported_ReadOnlyCollection);
		}

		internal void InternalArray__ICollection_Add<T> (T item)
		{
			ThrowHelper.ThrowNotSupportedException (ExceptionResource.NotSupported_FixedSizeCollection);
		}

		internal bool InternalArray__ICollection_Remove<T> (T item)
		{
			ThrowHelper.ThrowNotSupportedException (ExceptionResource.NotSupported_FixedSizeCollection);
			return default;
		}

		internal bool InternalArray__ICollection_Contains<T> (T item)
		{
			return IndexOf ((T[])this, item, 0, Length) >= 0;
		}

		internal void InternalArray__ICollection_CopyTo<T> (T[] array, int arrayIndex)
		{
			Copy (this, GetLowerBound (0), array, arrayIndex, Length);
		}

		internal T InternalArray__IReadOnlyList_get_Item<T> (int index)
		{
			if ((uint)index >= (uint)Length)
				ThrowHelper.ThrowArgumentOutOfRange_IndexException ();

			T value;
			// Do not change this to call GetGenericValue_icall directly, due to special casing in the runtime.
			GetGenericValueImpl (index, out value);
			return value;
		}

		internal int InternalArray__IReadOnlyCollection_get_Count ()
		{
			return Length;
		}

		internal void InternalArray__Insert<T> (int index, T item)
		{
			ThrowHelper.ThrowNotSupportedException (ExceptionResource.NotSupported_FixedSizeCollection);
		}

		internal void InternalArray__RemoveAt (int index)
		{
			ThrowHelper.ThrowNotSupportedException (ExceptionResource.NotSupported_FixedSizeCollection);
		}

		internal int InternalArray__IndexOf<T> (T item)
		{
			return IndexOf ((T[])this, item, 0, Length);
		}

		internal T InternalArray__get_Item<T> (int index)
		{
			if ((uint)index >= (uint)Length)
				ThrowHelper.ThrowArgumentOutOfRange_IndexException ();

			T value;
			// Do not change this to call GetGenericValue_icall directly, due to special casing in the runtime.
			GetGenericValueImpl (index, out value);
			return value;
		}

		internal void InternalArray__set_Item<T> (int index, T item)
		{
			if ((uint)index >= (uint)Length)
				ThrowHelper.ThrowArgumentOutOfRange_IndexException();

			if (this is object[] oarray) {
				oarray! [index] = (object)item;
				return;
			}

			// Do not change this to call SetGenericValue_icall directly, due to special casing in the runtime.
			SetGenericValueImpl (index, ref item);
		}
	}
}
