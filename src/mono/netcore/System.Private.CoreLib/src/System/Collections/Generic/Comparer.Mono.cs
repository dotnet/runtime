// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
	partial class Comparer<T>
	{
        static volatile Comparer<T> defaultComparer;

		public static Comparer<T> Default {
			get {
                Comparer<T> comparer = defaultComparer;
                if (comparer == null) {
                    comparer = CreateComparer();
                    defaultComparer = comparer;
                }
                return comparer;
			}
		}

        static Comparer<T> CreateComparer() {
            RuntimeType t = (RuntimeType)typeof(T);

                if (typeof(IComparable<T>).IsAssignableFrom(t))
                    return (Comparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter (typeof(GenericComparer<>), t);

				// If T is a Nullable<U> where U implements IComparable<U> return a NullableComparer<U>
				if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>)) {
					RuntimeType u = (RuntimeType)t.GetGenericArguments()[0];
					if (typeof(IComparable<>).MakeGenericType (u).IsAssignableFrom (u))
						return (Comparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter (typeof(NullableComparer<>), u);
				}

				if (t.IsEnum)
					return (Comparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter (typeof(EnumComparer<>), t);

				// Otherwise return an ObjectComparer<T>
				return new ObjectComparer<T> ();
		}
	}

	partial class EnumComparer<T>
	{
		[MethodImpl (MethodImplOptions.AggressiveInlining)]
		public override int Compare (T x, T y) => JitHelpers.EnumCompareTo (x, y);
	}
}
