// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;

namespace System.Runtime.Serialization
{
    internal static class Fx
    {
        public static bool IsFatal(Exception exception)
        {
            while (exception != null)
            {
                // NetFx checked for FatalException and FatalInternalException as well, which were ServiceModel constructs.
                if ((exception is OutOfMemoryException && !(exception is InsufficientMemoryException)) ||
                    exception is ThreadAbortException)
                {
                    return true;
                }

                // These exceptions aren't themselves fatal, but since the CLR uses them to wrap other exceptions,
                // we want to check to see whether they've been used to wrap a fatal exception.  If so, then they
                // count as fatal.
                if (exception is TypeInitializationException ||
                    exception is TargetInvocationException)
                {
                    exception = exception.InnerException!;
                }
                else if (exception is AggregateException)
                {
                    // AggregateExceptions have a collection of inner exceptions, which may themselves be other
                    // wrapping exceptions (including nested AggregateExceptions).  Recursively walk this
                    // hierarchy.  The (singular) InnerException is included in the collection.
                    var innerExceptions = ((AggregateException)exception).InnerExceptions;
                    foreach (Exception innerException in innerExceptions)
                    {
                        if (IsFatal(innerException))
                        {
                            return true;
                        }
                    }

                    break;
                }
                else
                {
                    break;
                }
            }

            return false;
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
