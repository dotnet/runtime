// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;

using Debug = System.Diagnostics.Debug;

namespace System
{
    // Eagerly preallocate instance of out of memory exception to avoid infinite recursion once we run out of memory
    [EagerStaticClassConstruction]
    internal static class PreallocatedOutOfMemoryException
    {
        public static readonly OutOfMemoryException Instance = new OutOfMemoryException();
    }

    internal static class RuntimeExceptionHelpers
    {
        //------------------------------------------------------------------------------------------------------------
        // @TODO: this function is related to throwing exceptions out of Rtm. If we did not have to throw
        // out of Rtm, then we would note have to have the code below to get a classlib exception object given
        // an exception id, or the special functions to back up the MDIL THROW_* instructions, or the allocation
        // failure helper. If we could move to a world where we never throw out of Rtm, perhaps by moving parts
        // of Rtm that do need to throw out to Bartok- or Binder-generated functions, then we could remove all of this.
        //------------------------------------------------------------------------------------------------------------

        // This is the classlib-provided "get exception" function that will be invoked whenever the runtime
        // needs to throw an exception back to a method in a non-runtime module. The classlib is expected
        // to convert every code in the ExceptionIDs enum to an exception object.
        [RuntimeExport("GetRuntimeException")]
        public static Exception GetRuntimeException(ExceptionIDs id)
        {
            // This method is called by the runtime's EH dispatch code and is not allowed to leak exceptions
            // back into the dispatcher.
            try
            {
                // @TODO: this function should return pre-allocated exception objects, either frozen in the image
                // or preallocated during DllMain(). In particular, this function will be called when out of memory,
                // and failure to create an exception will result in infinite recursion and therefore a stack overflow.
                switch (id)
                {
                    case ExceptionIDs.OutOfMemory:
                        return PreallocatedOutOfMemoryException.Instance;

                    case ExceptionIDs.Arithmetic:
                        return new ArithmeticException();

                    case ExceptionIDs.ArrayTypeMismatch:
                        return new ArrayTypeMismatchException();

                    case ExceptionIDs.DivideByZero:
                        return new DivideByZeroException();

                    case ExceptionIDs.IndexOutOfRange:
                        return new IndexOutOfRangeException();

                    case ExceptionIDs.InvalidCast:
                        return new InvalidCastException();

                    case ExceptionIDs.Overflow:
                        return new OverflowException();

                    case ExceptionIDs.NullReference:
                        return new NullReferenceException();

                    case ExceptionIDs.DataMisaligned:
                        // We don't have this in Test.CoreLib
                        return new PlatformNotSupportedException();

                    default:
                        Debug.Assert(false, "unexpected ExceptionID");
                        RuntimeImports.RhpFallbackFailFast();
                        return null;
                }
            }
            catch
            {
                return null; // returning null will cause the runtime to FailFast via the class library.
            }
        }

        // This is the classlib-provided fail-fast function that will be invoked whenever the runtime
        // needs to cause the process to exit. It is the classlib's opportunity to customize the
        // termination behavior in whatever way necessary.
        [RuntimeExport("FailFast")]
        public static void RuntimeFailFast(RhFailFastReason reason, Exception exception, IntPtr pExAddress, IntPtr pExContext)
        {
            RuntimeImports.RhpFallbackFailFast();
        }

        public static void FailFast(string message)
        {
            RuntimeImports.RhpFallbackFailFast();
        }

        [RuntimeExport("AppendExceptionStackFrame")]
        private static void AppendExceptionStackFrame(object exceptionObj, IntPtr IP, int flags)
        {
            Exception ex = exceptionObj as Exception;
            if (ex == null)
                FailFast("Exceptions must derive from the System.Exception class");
        }

        [RuntimeExport("OnFirstChanceException")]
        internal static void OnFirstChanceException(object e)
        {
        }

        [RuntimeExport("OnUnhandledException")]
        internal static void OnUnhandledException(object e)
        {
        }
    }
}
