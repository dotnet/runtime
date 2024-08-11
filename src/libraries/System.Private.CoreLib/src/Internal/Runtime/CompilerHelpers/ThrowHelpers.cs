// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Internal.Runtime.CompilerHelpers
{
    [StackTraceHidden]
    [DebuggerStepThrough]
    internal static unsafe partial class ThrowHelpers
    {
        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowNullReferenceException()
        {
            throw new NullReferenceException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowArgumentException()
        {
            throw new ArgumentException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowArgumentOutOfRangeException()
        {
            throw new ArgumentOutOfRangeException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowDivideByZeroException()
        {
            throw new DivideByZeroException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowIndexOutOfRangeException()
        {
            throw new IndexOutOfRangeException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowOverflowException()
        {
            throw new OverflowException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowPlatformNotSupportedException()
        {
            throw new PlatformNotSupportedException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowNotImplementedException()
        {
            throw new NotImplementedException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowArrayTypeMismatchException()
        {
            throw new ArrayTypeMismatchException();
        }

        [DoesNotReturn]
        [DebuggerHidden]
        internal static void ThrowNotSupportedException()
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
