// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace FullyGeneralGenericsTest
{
    [GenericParameterSupportsAnyTypeAttribute(0)]
    struct GenericType<T>
    {
        private T _value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetRef(ref T tToSet, T val)
        {
            tToSet = val;
        }
        public T Value
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get
            {
                return _value;
            }
            [MethodImpl(MethodImplOptions.NoInlining)]
            set
            {
                SetRef(ref _value, value);
            }
        }
        public static void UseValue()
        {
            GenericType<T> localValue = new GenericType<T>();
            localValue.Value = default(T);
            return;
        }

        [GenericParameterSupportsOnlyNonByRefLikeAttribute]
        public static void FailOnNonStandardTypeWithException()
        {
            return;
        }
    }

    class FullyGeneralGenericsTest
    {
        static int Main()
        {
            Type fullyGenericType;

            Console.WriteLine("Test Can Create Generic of Int");
            fullyGenericType = typeof(GenericType<>).MakeGenericType(typeof(int));
            Console.WriteLine(fullyGenericType.FullName);
            fullyGenericType.GetMethod("UseValue").Invoke(null, null);
            fullyGenericType.GetMethod("FailOnNonStandardTypeWithException").Invoke(null, null);

#if false
            Console.WriteLine("Test Can Create Generic of ByRef");
            fullyGenericType = typeof(GenericType<>).MakeGenericType(typeof(int).MakeByRefType());
            Console.WriteLine(fullyGenericType.FullName);
            fullyGenericType.GetMethod("UseValue").Invoke(null, null);
            try
            {
                fullyGenericType.GetMethod("FailOnNonStandardTypeWithException").Invoke(null, null);
                Console.WriteLine("FAILED to throw exception calling method with [GenericParameterSupportsOnlyNonByRefLikeAttribute]");
                return 1;
            } catch {}
#endif
            Console.WriteLine("Test Can Create Generic of ByRef to ByRef");
            fullyGenericType = typeof(GenericType<>).MakeGenericType(typeof(int).MakeByRefType().MakeByRefType());
            Console.WriteLine(fullyGenericType.FullName);
            fullyGenericType.GetMethod("UseValue").Invoke(null, null);
#if false
            try
            {
                fullyGenericType.GetMethod("FailOnNonStandardTypeWithException").Invoke(null, null);
                Console.WriteLine("FAILED to throw exception calling method with [GenericParameterSupportsOnlyNonByRefLikeAttribute]");
                return 1;
            } catch {}
#endif

            Console.WriteLine("Test Can Create Generic of TypedReference");
            fullyGenericType = typeof(GenericType<>).MakeGenericType(typeof(TypedReference));
            Console.WriteLine(fullyGenericType.FullName);
            fullyGenericType.GetMethod("UseValue").Invoke(null, null);
#if false
            try
            {
                fullyGenericType.GetMethod("FailOnNonStandardTypeWithException").Invoke(null, null);
                Console.WriteLine("FAILED to throw exception calling method with [GenericParameterSupportsOnlyNonByRefLikeAttribute]");
                return 1;
            } catch {}
#endif

            return 100;
        }
    }
}
