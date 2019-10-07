// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

		public static void Clear (Array array, int index, int length)
		{
			if (array == null)
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
			if (length < 0)
				ThrowHelper.ThrowIndexOutOfRangeException();

			int low = array!.GetLowerBound (0);
			if (index < low)
				ThrowHelper.ThrowIndexOutOfRangeException();

			index = index - low;

			// re-ordered to avoid possible integer overflow
			if (index > array.Length - length)
				ThrowHelper.ThrowIndexOutOfRangeException();

			ClearInternal (array, index, length);
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
					!(dst_type.IsPrimitive && src_type.IsPrimitive && CanChangePrimitive(dst_type, src_type, true))) {
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
					return CanChangePrimitive (source, target, false);
				} else if (!source.IsValueType && !source.IsPointer) {
					// Source is base class or interface of destination type
					if (target.IsPointer)
						return false;
					return source.IsAssignableFrom (target);
				}
			}

			return false;
		}

		public static Array CreateInstance (Type elementType, int length)
		{
			if (length < 0)
				throw new ArgumentOutOfRangeException (nameof (length));

			int[] lengths = {length};

			return CreateInstance (elementType, lengths);
		}

		public static Array CreateInstance (Type elementType, int length1, int length2)
		{
			if (length1 < 0)
				throw new ArgumentOutOfRangeException (nameof (length1));
			if (length2 < 0)
				throw new ArgumentOutOfRangeException (nameof (length2));

			int[] lengths = {length1, length2};

			return CreateInstance (elementType, lengths);
		}

		public static Array CreateInstance (Type elementType, int length1, int length2, int length3)
		{
			if (length1 < 0)
				throw new ArgumentOutOfRangeException (nameof (length1));
			if (length2 < 0)
				throw new ArgumentOutOfRangeException (nameof (length2));
			if (length3 < 0)
				throw new ArgumentOutOfRangeException (nameof (length3));

			int[] lengths = {length1, length2, length3};

			return CreateInstance (elementType, lengths);
		}

		public static Array CreateInstance (Type elementType, params int[] lengths)
		{
			if (elementType == null)
				throw new ArgumentNullException ("elementType");
			if (lengths == null)
				throw new ArgumentNullException ("lengths");
			if (lengths.Length == 0)
				throw new ArgumentException (nameof (lengths));
			if (lengths.Length > 255)
				throw new TypeLoadException ();
			for (int i = 0; i < lengths.Length; ++i) {
				if (lengths [i] < 0)
					throw new ArgumentOutOfRangeException ($"lengths[{i}]", SR.ArgumentOutOfRange_NeedNonNegNum);
			}

			if (!(elementType.UnderlyingSystemType is RuntimeType et))
				throw new ArgumentException ("Type must be a type provided by the runtime.", "elementType");
			if (et.Equals (typeof (void)))
				throw new NotSupportedException ("Array type can not be void");
			if (et.ContainsGenericParameters)
				throw new NotSupportedException ("Array type can not be an open generic type");
			if (et.IsByRef)
				throw new NotSupportedException (SR.NotSupported_Type);
			
			return CreateInstanceImpl (et, lengths, null);
		}

		public static Array CreateInstance (Type elementType, int[] lengths, int [] lowerBounds)
		{
			if (elementType == null)
				throw new ArgumentNullException ("elementType");
			if (lengths == null)
				throw new ArgumentNullException ("lengths");
			if (lowerBounds == null)
				throw new ArgumentNullException ("lowerBounds");

			if (!(elementType.UnderlyingSystemType is RuntimeType rt))
				throw new ArgumentException ("Type must be a type provided by the runtime.", "elementType");
			if (rt.Equals (typeof (void)))
				throw new NotSupportedException ("Array type can not be void");
			if (rt.ContainsGenericParameters)
				throw new NotSupportedException ("Array type can not be an open generic type");
			if (rt.IsByRef)
				throw new NotSupportedException (SR.NotSupported_Type);

			if (lengths.Length < 1)
				throw new ArgumentException ("Arrays must contain >= 1 elements.");

			if (lengths.Length != lowerBounds.Length)
				throw new ArgumentException ("Arrays must be of same size.");

			for (int j = 0; j < lowerBounds.Length; j ++) {
				if (lengths [j] < 0)
					throw new ArgumentOutOfRangeException ($"lengths[{j}]", "Each value has to be >= 0.");
				if ((long)lowerBounds [j] + (long)lengths [j] > (long)Int32.MaxValue)
					throw new ArgumentOutOfRangeException (null, "Length + bound must not exceed Int32.MaxValue.");
			}

			if (lengths.Length > 255)
				throw new TypeLoadException ();

			return CreateInstanceImpl (elementType, lengths, lowerBounds);
		}

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

		static void SortImpl (Array keys, Array? items, int index, int length, IComparer comparer)
		{
			/* TODO: CoreCLR optimizes this case via an internal call
			if (comparer == Comparer.Default)
			{
				bool r = TrySZSort(keys, items, index, index + length - 1);
				if (r)
					return;
			}*/

			object[]? objKeys = keys as object[];
			object[]? objItems = null;
			if (objKeys != null)
				objItems = items as object[];
			if (objKeys != null && (items == null || objItems != null)) {
				SorterObjectArray sorter = new SorterObjectArray (objKeys, objItems, comparer);
				sorter.Sort(index, length);
			} else {
				SorterGenericArray sorter = new SorterGenericArray (keys, items, comparer);
				sorter.Sort(index, length);
			}
		}

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

		internal int GetElementSize ()
		{
			return Marshal.GetArrayElementSize (GetType ());
		}

		//
		// Moved value from instance into target of different type with no checks (JIT intristics)
		//
		// Restrictions:
		//
		// S and R must either:
		// 	 both be blitable valuetypes
		// 	 both be reference types (IOW, an unsafe cast)
		// S and R cannot be float or double
		// S and R must either:
		//	 both be a struct
		// 	 both be a scalar
		// S and R must either:
		// 	 be of same size
		// 	 both be a scalar of size <= 4
		//
		internal static R UnsafeMov<S,R> (S instance)
		{
			return (R)(object) instance;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void ClearInternal (Array a, int index, int count);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static Array CreateInstanceImpl (Type elementType, int[] lengths, int[]? bounds);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static bool CanChangePrimitive (Type srcType, Type dstType, bool reliable);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool FastCopy (Array source, int source_idx, Array dest, int dest_idx, int length);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern int GetRank ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern int GetLength (int dimension);

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
