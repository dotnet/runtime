// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Runtime.CompilerServices;
using System.Threading;

using Internal.IntrinsicSupport;
using Internal.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    public abstract partial class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
    {
        private static EqualityComparer<T> s_default;

        // The AOT compiler can flip this to false under certain circumstances.
        private static bool SupportsGenericIEquatableInterfaces => true;

        [Intrinsic]
        private static EqualityComparer<T> Create()
        {
            // The compiler will overwrite the Create method with optimized
            // instantiation-specific implementation.
            // This body serves as a fallback when instantiation-specific implementation is unavailable.
            // If that happens, the compiler ensures we generate data structures to make the fallback work
            // when this method is compiled.
            Interlocked.CompareExchange(ref s_default,
                SupportsGenericIEquatableInterfaces
                ? Unsafe.As<EqualityComparer<T>>(EqualityComparerHelpers.GetComparer(typeof(T).TypeHandle))
                : new ObjectEqualityComparer<T>(),
                null);
            return s_default;
        }

        public static EqualityComparer<T> Default
        {
            [Intrinsic]
            get
            {
                // Lazy initialization produces smaller code for AOT compilation than initialization in constructor
                return s_default ?? Create();
            }
        }
    }

    public sealed partial class EnumEqualityComparer<T> : EqualityComparer<T> where T : struct, Enum
    {
        public sealed override bool Equals(T x, T y)
        {
            return EqualityComparerHelpers.EnumOnlyEquals(x, y);
        }
    }
}
