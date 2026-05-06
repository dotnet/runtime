// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// These methods are used to throw exceptions from generated code.
    /// </summary>
    internal static class ThrowHelpers
    {
        private static void ThrowOverflowException()
        {
            throw new OverflowException();
        }

        private static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }

        private static void ThrowNullReferenceException()
        {
            throw new NullReferenceException();
        }

        private static void ThrowDivideByZeroException()
        {
            throw new DivideByZeroException();
        }

        private static void ThrowArrayTypeMismatchException()
        {
            throw new ArrayTypeMismatchException();
        }

        private static void ThrowPlatformNotSupportedException()
        {
            throw new PlatformNotSupportedException();
        }

        private static void ThrowTypeLoadException()
        {
            // exception doesn't exist in MRT: throw PlatformNotSupportedException() instead
            throw new PlatformNotSupportedException();
        }

        private static void ThrowArgumentException()
        {
            // exception doesn't exist in MRT: throw PlatformNotSupportedException() instead
            throw new PlatformNotSupportedException();
        }

        private static void ThrowArgumentOutOfRangeException()
        {
            // exception doesn't exist in MRT: throw PlatformNotSupportedException() instead
            throw new PlatformNotSupportedException();
        }
    }
}
