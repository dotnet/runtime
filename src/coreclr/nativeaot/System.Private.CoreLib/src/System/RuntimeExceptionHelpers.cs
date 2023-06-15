// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;
using Internal.DeveloperExperience;
using Internal.Runtime.Augments;

namespace System
{
    internal static class PreallocatedOutOfMemoryException
    {
        public static OutOfMemoryException Instance { get; private set; }

        // Eagerly preallocate instance of out of memory exception to avoid infinite recursion once we run out of memory
        internal static void Initialize()
        {
            Instance = new OutOfMemoryException(message: null);  // Cannot call the nullary constructor as that triggers non-trivial resource manager logic.
        }
    }

    public class RuntimeExceptionHelpers
    {
        //------------------------------------------------------------------------------------------------------------
        // @TODO: this function is related to throwing exceptions out of Rtm. If we did not have to throw
        // out of Rtm, then we would note have to have the code below to get a classlib exception object given
        // an exception id, or the special functions to back up the MDIL THROW_* instructions, or the allocation
        // failure helper. If we could move to a world where we never throw out of Rtm, perhaps by moving parts
        // of Rtm that do need to throw out to Bartok- or Binder-generated functions, then we could remove all of this.
        //------------------------------------------------------------------------------------------------------------

        [ThreadStatic]
        private static bool t_allocatingOutOfMemoryException;

        // This is the classlib-provided "get exception" function that will be invoked whenever the runtime
        // needs to throw an exception back to a method in a non-runtime module. The classlib is expected
        // to convert every code in the ExceptionIDs enum to an exception object.
        [RuntimeExport("GetRuntimeException")]
        public static Exception? GetRuntimeException(ExceptionIDs id)
        {
            if (!SafeToPerformRichExceptionSupport)
                return null;

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
                        Exception outOfMemoryException = PreallocatedOutOfMemoryException.Instance;

                        // If possible, try to allocate proper out-of-memory exception with error message and stack trace
                        if (!t_allocatingOutOfMemoryException)
                        {
                            t_allocatingOutOfMemoryException = true;
                            try
                            {
                                outOfMemoryException = new OutOfMemoryException();
                            }
                            catch
                            {
                            }
                            t_allocatingOutOfMemoryException = false;
                        }

                        return outOfMemoryException;

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

                    case ExceptionIDs.AccessViolation:
                        FailFast("Access Violation: Attempted to read or write protected memory. This is often an indication that other memory is corrupt. The application will be terminated since this platform does not support throwing an AccessViolationException.");
                        return null;

                    case ExceptionIDs.DataMisaligned:
                        return new DataMisalignedException();

                    case ExceptionIDs.EntrypointNotFound:
                        return new EntryPointNotFoundException();

                    case ExceptionIDs.AmbiguousImplementation:
                        return new AmbiguousImplementationException();

                    default:
                        FailFast("The runtime requires an exception for a case that this class library does not understand.");
                        return null;
                }
            }
            catch
            {
                return null; // returning null will cause the runtime to FailFast via the class library.
            }
        }

        public enum RhFailFastReason
        {
            Unknown = 0,
            InternalError = 1,                                   // "Runtime internal error"
            UnhandledException_ExceptionDispatchNotAllowed = 2,  // "Unhandled exception: no handler found before escaping a finally clause or other fail-fast scope."
            UnhandledException_CallerDidNotHandle = 3,           // "Unhandled exception: no handler found in calling method."
            ClassLibDidNotTranslateExceptionID = 4,              // "Unable to translate failure into a classlib-specific exception object."
            UnhandledException = 5,                              // "Unhandled exception: a managed exception was not handled before reaching unmanaged code"
            UnhandledExceptionFromPInvoke = 6,                   // "Unhandled exception: an unmanaged exception was thrown out of a managed-to-native transition."
        }

        private static string GetStringForFailFastReason(RhFailFastReason reason)
        {
            switch (reason)
            {
                case RhFailFastReason.InternalError:
                    return "Runtime internal error";
                case RhFailFastReason.UnhandledException_ExceptionDispatchNotAllowed:
                    return "Unhandled exception: no handler found before escaping a finally clause or other fail-fast scope.";
                case RhFailFastReason.UnhandledException_CallerDidNotHandle:
                    return "Unhandled exception: no handler found in calling method.";
                case RhFailFastReason.ClassLibDidNotTranslateExceptionID:
                    return "Unable to translate failure into a classlib-specific exception object.";
                case RhFailFastReason.UnhandledException:
                    return "Unhandled exception: a managed exception was not handled before reaching unmanaged code.";
                case RhFailFastReason.UnhandledExceptionFromPInvoke:
                    return "Unhandled exception: an unmanaged exception was thrown out of a managed-to-native transition.";
                default:
                    return "Unknown reason.";
            }
        }


        [DoesNotReturn]
        public static void FailFast(string message)
        {
            FailFast(message, null, RhFailFastReason.Unknown, IntPtr.Zero, IntPtr.Zero);
        }

        [DoesNotReturn]
        public static unsafe void FailFast(string message, Exception? exception)
        {
            FailFast(message, exception, RhFailFastReason.Unknown, IntPtr.Zero, IntPtr.Zero);
        }

        // Used to report exceptions that *logically* go unhandled in the Fx code.  For example, an
        // exception that escapes from a ThreadPool workitem, or from a void-returning async method.
        public static void ReportUnhandledException(Exception exception)
        {
            FailFast(GetStringForFailFastReason(RhFailFastReason.UnhandledException), exception);
        }

        // This is the classlib-provided fail-fast function that will be invoked whenever the runtime
        // needs to cause the process to exit. It is the classlib's opportunity to customize the
        // termination behavior in whatever way necessary.
        [RuntimeExport("FailFast")]
        public static void RuntimeFailFast(RhFailFastReason reason, Exception? exception, IntPtr pExAddress, IntPtr pExContext)
        {
            if (!SafeToPerformRichExceptionSupport)
                return;

            // This method is called by the runtime's EH dispatch code and is not allowed to leak exceptions
            // back into the dispatcher.
            try
            {
                // Avoid complex processing and allocations if we are already in failfast or recursive out of memory.
                // We do not set InFailFast.Value here, because we want rich diagnostics in the FailFast
                // call below and reentrancy is not possible for this method (all exceptions are ignored).
                bool minimalFailFast = InFailFast.Value || (exception == PreallocatedOutOfMemoryException.Instance);
                string failFastMessage = "";

                if (!minimalFailFast)
                {
                    if ((reason == RhFailFastReason.UnhandledException) && (exception != null))
                    {
                        Debug.WriteLine("Unhandled Exception: " + exception.ToString());
                    }

                    failFastMessage = string.Format("Runtime-generated FailFast: ({0}): {1}{2}",
                        reason.ToString(),  // Explicit call to ToString() to avoid missing metadata exception inside String.Format()
                        GetStringForFailFastReason(reason),
                        exception != null ? " [exception object available]" : "");
                }

                FailFast(failFastMessage, exception, reason, pExAddress, pExContext);
            }
            catch
            {
                // Returning from this callback will cause the runtime to FailFast without involving the class
                // library.
            }
        }

        [DoesNotReturn]
        internal static void FailFast(string message, Exception? exception, RhFailFastReason reason, IntPtr pExAddress, IntPtr pExContext)
        {
            // If this a recursive call to FailFast, avoid all unnecessary and complex activity the second time around to avoid the recursion
            // that got us here the first time (Some judgement is required as to what activity is "unnecessary and complex".)
            bool minimalFailFast = InFailFast.Value || (exception == PreallocatedOutOfMemoryException.Instance);
            InFailFast.Value = true;

            if (!minimalFailFast)
            {
                string prefix;
                string outputMessage;
                if (exception != null)
                {
                    prefix = "Unhandled Exception: ";
                    outputMessage = exception.ToString();
                }
                else
                {
                    prefix = "Process terminated. ";
                    outputMessage = message;
                }


                Internal.Console.Error.Write(prefix);
                if (outputMessage != null)
                    Internal.Console.Error.Write(outputMessage);
                Internal.Console.Error.Write(Environment.NewLine);

                if (outputMessage != null)
                {
                    // Try to save the exception stack trace in a buffer on the stack.  If the exception is too large, we'll truncate it.
                    const int MaxStack = 2048;
                    Span<byte> exceptionStack = stackalloc byte[MaxStack];

                    // Ignore output, as this is best-effort
                    _ = Utf8.FromUtf16(outputMessage, exceptionStack, out _, out int length);
                    // Fill the rest of the buffer with nulls
                    if (length < MaxStack)
                        exceptionStack.Slice(length).Clear();

                    unsafe
                    {
                        byte* stackExceptionRecord = stackalloc byte[sizeof(CrashDumpRecord)];
                        CrashDumpRecord* pExceptionRecord = (CrashDumpRecord*)stackExceptionRecord;
                        var cookieSpan = new Span<byte>(pExceptionRecord->Cookie, CrashDumpRecord.CookieSize);
                        // Random 10 bytes to identify the record
                        ((ReadOnlySpan<byte>)new byte[] { 0x1c, 0x73, 0xd0, 0x2d, 0xda, 0x6b, 0x4c, 0xef, 0xbf, 0xa1 }).CopyTo(cookieSpan);
                        "NETRUNTIME"u8.CopyTo(cookieSpan.Slice(10));
                        pExceptionRecord->Type = 1;
                        pExceptionRecord->Data = (void*)exceptionStack.GetPinnableReference();
                        pExceptionRecord->Length = length;
                    }
                }
            }

#if TARGET_WINDOWS
            uint errorCode = 0x80004005; // E_FAIL
            // To help enable testing to bucket the failures we choose one of the following as errorCode:
            // * hashcode of EETypePtr if it is an unhandled managed exception
            // * HRESULT, if available
            // * RhFailFastReason, if it is one of the known reasons
            if (exception != null)
            {
                if (reason == RhFailFastReason.UnhandledException)
                    errorCode = (uint)(exception.GetEETypePtr().GetHashCode());
                else if (exception.HResult != 0)
                    errorCode = (uint)exception.HResult;
            }
            else if (reason != RhFailFastReason.Unknown)
            {
                errorCode = (uint)reason + 0x1000; // Add something to avoid common low level exit codes
            }

            Interop.Kernel32.RaiseFailFastException(errorCode, pExAddress, pExContext);
#else
            Interop.Sys.Abort();
#endif
        }

        private unsafe struct CrashDumpRecord
        {
            public const int CookieSize = 20;
            public fixed byte Cookie[CookieSize];
            public int Type;
            public void* Data;
            public int Length;
        }

        // Use a nested class to avoid running the class constructor of the outer class when
        // accessing this flag.
        private static class InFailFast
        {
            // This boolean is used to stop runaway FailFast recursions. Though this is technically a concurrently set field, it only gets set during
            // fatal process shutdowns and it's only purpose is a reasonable-case effort to make a bad situation a little less bad.
            // Trying to use locks or other concurrent access apis would actually defeat the purpose of making FailFast as robust as possible.
            public static bool Value;
        }

        // This returns "true" once enough of the framework has been initialized to safely perform operations
        // such as filling in the stack frame and generating diagnostic support.
        public static bool SafeToPerformRichExceptionSupport
        {
            get
            {
                // Reflection needs to work as the exception code calls GetType() and GetType().ToString()
                if (RuntimeAugments.CallbacksIfAvailable == null)
                    return false;
                return true;
            }
        }
    }
}
