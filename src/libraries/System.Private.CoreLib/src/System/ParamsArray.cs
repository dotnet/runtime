// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// These types are temporary workarounds for an inability to stackalloc object references.
// Once we're able to do `stackalloc object[n]`, these can be removed.

// Suppress warnings for unused private fields
#pragma warning disable CS0169, CA1823, IDE0051, IDE0044

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System
{
#if CORECLR || NATIVEAOT

    [InlineArray(Length)]
    internal struct TwoObjects
    {
        private const int Length = 2;
        internal object? Arg0;

        [UnscopedRef]
        private ref object? this[int i] => ref Unsafe.Add(ref Arg0, i);

        public TwoObjects(object? arg0, object? arg1)
        {
            this[0] = arg0;
            this[1] = arg1;
        }
    }

    [InlineArray(Length)]
    internal struct ThreeObjects
    {
        private const int Length = 3;
        internal object? Arg0;

        [UnscopedRef]
        private ref object? this[int i] => ref Unsafe.Add(ref Arg0, i);

        public ThreeObjects(object? arg0, object? arg1, object? arg2)
        {
            this[0] = arg0;
            this[1] = arg1;
            this[2] = arg2;
        }
    }

#else

    internal struct TwoObjects
    {
        internal object? Arg0;
        private object? _arg1;

        public TwoObjects(object? arg0, object? arg1)
        {
            Arg0 = arg0;
            _arg1 = arg1;
        }
    }

    internal struct ThreeObjects
    {
        internal object? Arg0;
        private object? _arg1;
        private object? _arg2;

        public ThreeObjects(object? arg0, object? arg1, object? arg2)
        {
            Arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
        }
    }
#endif
}
