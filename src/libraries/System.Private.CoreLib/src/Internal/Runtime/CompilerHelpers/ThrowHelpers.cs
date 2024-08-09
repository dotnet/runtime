// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// These methods are used to throw exceptions from generated code.
    /// For NativeAOT, the type and methods used need to be public
    ///     as they constitute a public contract.
    /// For CoreCLR, the type and methods are used as JIT helpers.
    /// </summary>
#if NATIVEAOT
    public
#else
    internal
#endif
    static unsafe partial class ThrowHelpers
    {
        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowNullReferenceException()
        {
            throw new NullReferenceException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowArgumentException()
        {
            throw new ArgumentException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowDivideByZeroException()
        {
            throw new DivideByZeroException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowOverflowException()
        {
            throw new OverflowException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowPlatformNotSupportedException()
        {
            throw new PlatformNotSupportedException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowNotImplementedException()
        {
            throw new NotImplementedException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowArrayTypeMismatchException()
        {
            throw new ArrayTypeMismatchException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        public static void ThrowNotSupportedException()
        {
            throw new NotSupportedException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowTypeNotSupportedException()
        {
            throw new NotSupportedException(SR.Arg_TypeNotSupported);
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowVerificationException(int ilOffset)
        {
            throw new System.Security.VerificationException();
        }
    }
}
