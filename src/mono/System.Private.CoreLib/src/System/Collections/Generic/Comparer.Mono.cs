// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Generic
{
    public partial class Comparer<T>
    {
        private static volatile Comparer<T>? defaultComparer;

        public static Comparer<T> Default
        {
            get
            {
                Comparer<T>? comparer = defaultComparer;
                if (comparer == null)
                {
                    // Do not use static constructor. Generic static constructors are problematic for Mono AOT.
                    Interlocked.CompareExchange(ref defaultComparer, CreateComparer(), null);
                    comparer = defaultComparer;
                }
                return comparer;
            }
        }

        private static Comparer<T> CreateComparer()
        {
            RuntimeType t = (RuntimeType)typeof(T);

            if (typeof(IComparable<T>).IsAssignableFrom(t))
                return (Comparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter(typeof(GenericComparer<>), t);

            // If T is a Nullable<U> return a NullableComparer<U>
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                RuntimeType u = (RuntimeType)t.GetGenericArguments()[0];
                return (Comparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter(typeof(NullableComparer<>), u);
            }

            if (t.IsEnum)
                return (Comparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter(typeof(EnumComparer<>), t);

            // Otherwise return an ObjectComparer<T>
            return new ObjectComparer<T>();
        }
    }

    internal partial class EnumComparer<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int Compare(T x, T y) => JitHelpers.EnumCompareTo(x, y);
    }
}
