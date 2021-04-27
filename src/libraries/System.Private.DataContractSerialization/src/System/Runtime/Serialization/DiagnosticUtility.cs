// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization
{
    internal static class Fx
    {
        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, string message)
        {
            System.Diagnostics.Debug.Assert(condition, message);
        }

        [Conditional("DEBUG")]
        [DoesNotReturn]
        public static void Assert(string message)
        {
            Assert(false, message);
        }
    }

    internal static class DiagnosticUtility
    {
        [Conditional("DEBUG")]
        [DoesNotReturn]
        public static void DebugAssert(string message)
        {
            DebugAssert(false, message);
        }

        [Conditional("DEBUG")]
        public static void DebugAssert([DoesNotReturnIf(false)] bool condition, string message)
        {
            Debug.Assert(condition, message);
        }

        internal static class ExceptionUtility
        {
            public static Exception ThrowHelperArgumentNull(string message)
            {
                return new ArgumentNullException(message);
            }

            public static Exception ThrowHelperError(Exception e)
            {
                return e;
            }

            public static Exception ThrowHelperArgument(string message)
            {
                return new ArgumentException(message);
            }

            internal static Exception ThrowHelperFatal(string message, Exception innerException)
            {
                return ThrowHelperError(new Exception(message, innerException));
            }
            internal static Exception ThrowHelperCallback(Exception e)
            {
                return ThrowHelperError(e);
            }
        }
    }
}
