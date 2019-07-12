// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
	partial class EqualityComparer<T>
	{
		static volatile EqualityComparer<T> defaultComparer;

		public static EqualityComparer<T> Default {
			[MethodImplAttribute (MethodImplOptions.AggressiveInlining)]
			get {
				EqualityComparer<T> comparer = defaultComparer;
				if (comparer == null) {
					comparer = CreateComparer();
					defaultComparer = comparer;
				}
				return comparer;
			}
		}

		static EqualityComparer<T> CreateComparer ()
		{
			RuntimeType t = (RuntimeType)typeof(T);

			/////////////////////////////////////////////////
			// KEEP THIS IN SYNC WITH THE DEVIRT CODE
			// IN METHOD-TO-IR.C
			/////////////////////////////////////////////////

			if (t == typeof(byte)) {
				return (EqualityComparer<T>)(object)(new ByteEqualityComparer());
			}

			if (typeof(IEquatable<T>).IsAssignableFrom(t)) {
				return (EqualityComparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter (typeof(GenericEqualityComparer<>), t);
			}

			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) {
				RuntimeType u = (RuntimeType)t.GetGenericArguments()[0];
				if (typeof(IEquatable<>).MakeGenericType(u).IsAssignableFrom(u)) {
					return (EqualityComparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter (typeof(NullableEqualityComparer<>), u);
				}
			}

			if (t.IsEnum) {
				return (EqualityComparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter (typeof(EnumEqualityComparer<>), t);
			}
			
			return new ObjectEqualityComparer<T>();
		}

		// MONOTODO: Add specialized versions
		internal virtual int IndexOf (T[] array, T value, int startIndex, int count)
		{
			int endIndex = startIndex + count;
			for (int i = startIndex; i < endIndex; i++) {
				if (Equals (array[i], value))
					return i;
			}
			return -1;
		}

		internal virtual int LastIndexOf (T[] array, T value, int startIndex, int count)
		{
			int endIndex = startIndex - count + 1;
			for (int i = startIndex; i >= endIndex; i--) {
				if (Equals (array[i], value))
					return i;
			}
			return -1;
		}

	}

	partial class EnumEqualityComparer<T>
	{
		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public override bool Equals (T x, T y) => JitHelpers.EnumEquals (x, y);
	}
}
