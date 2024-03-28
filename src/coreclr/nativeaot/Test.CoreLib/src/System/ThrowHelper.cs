// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System
{
    [StackTraceHidden]
    internal static class ThrowHelper
    {
        internal static void ThrowEntryPointNotFoundException()
        {
            throw new EntryPointNotFoundException();
        }

        internal static void ThrowAmbiguousImplementationException()
        {
            throw new AmbiguousImplementationException();
        }

        internal static void ThrowOverflowException()
        {
            throw new OverflowException();
        }

        internal static void ThrowDivideByZeroException()
        {
            throw new DivideByZeroException();
        }

        internal static void ThrowOutOfMemoryException()
        {
            throw new OutOfMemoryException();
        }

        internal static void ThrowNullReferenceException()
        {
            throw new NullReferenceException();
        }

        internal static void ThrowInvalidCastException()
        {
            throw new InvalidCastException();
        }

        internal static void ThrowArrayTypeMismatchException()
        {
            throw new ArrayTypeMismatchException();
        }

        internal static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }
    }
}
