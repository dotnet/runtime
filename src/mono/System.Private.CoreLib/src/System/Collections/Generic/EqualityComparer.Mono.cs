// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Collections.Generic
{
    public partial class EqualityComparer<T>
    {
        private static volatile EqualityComparer<T>? defaultComparer;

        public static EqualityComparer<T> Default
        {
            [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
            get
            {
                EqualityComparer<T>? comparer = defaultComparer;
                if (comparer == null)
                {
                    // Do not use static constructor. Generic static constructors are problematic for Mono AOT.
                    Interlocked.CompareExchange(ref defaultComparer, CreateComparer(), null);
                    comparer = defaultComparer;
                }
                return comparer;
            }
        }

        private static EqualityComparer<T> CreateComparer()
        {
            RuntimeType t = (RuntimeType)typeof(T);

            /////////////////////////////////////////////////
            // KEEP THIS IN SYNC WITH THE DEVIRT CODE
            // IN mini_handle_call_res_devirt
            /////////////////////////////////////////////////

            if (t == typeof(byte))
            {
                return (EqualityComparer<T>)(object)(new ByteEqualityComparer());
            }
            else if (t == typeof(string))
            {
                // Specialize for string, as EqualityComparer<string>.Default is on the startup path
                return (EqualityComparer<T>)(object)(new GenericEqualityComparer<string>());
            }

            if (typeof(IEquatable<T>).IsAssignableFrom(t))
            {
                return (EqualityComparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter(typeof(GenericEqualityComparer<>), t);
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                RuntimeType u = (RuntimeType)t.GetGenericArguments()[0];
                return (EqualityComparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter(typeof(NullableEqualityComparer<>), u);
            }

            if (t.IsEnum)
            {
                return (EqualityComparer<T>)RuntimeType.CreateInstanceForAnotherGenericParameter(typeof(EnumEqualityComparer<>), t);
            }

            return new ObjectEqualityComparer<T>();
        }
    }

    public partial class EnumEqualityComparer<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T x, T y) => JitHelpers.EnumEquals(x, y);
    }
}
