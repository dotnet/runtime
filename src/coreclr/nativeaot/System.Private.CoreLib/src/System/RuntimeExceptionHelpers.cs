// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.DeveloperExperience;
using Internal.Runtime.Augments;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

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
                case RhFailFastReason.EnvironmentFailFast:
                default:
                    return "Unknown reason.";
            }
        }

        [DoesNotReturn]
        public static void FailFast(string message)
        {
            FailFast(message, null, RhFailFastReason.EnvironmentFailFast, IntPtr.Zero, IntPtr.Zero);
        }

        [DoesNotReturn]
        public static void FailFast(string message, Exception? exception)
        {
            FailFast(message, exception, RhFailFastReason.EnvironmentFailFast, IntPtr.Zero, IntPtr.Zero);
        }

        // Used to report exceptions that *logically* go unhandled in the Fx code.  For example, an
        // exception that escapes from a ThreadPool workitem, or from a void-returning async method.
        public static void ReportUnhandledException(Exception exception)
        {
            FailFast(GetStringForFailFastReason(RhFailFastReason.UnhandledException), exception, RhFailFastReason.UnhandledException, IntPtr.Zero, IntPtr.Zero);
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
        internal static unsafe void FailFast(string message, Exception? exception, RhFailFastReason reason, IntPtr pExAddress, IntPtr pExContext)
        {
            // If this a recursive call to FailFast, avoid all unnecessary and complex activity the second time around to avoid the recursion
            // that got us here the first time (Some judgement is required as to what activity is "unnecessary and complex".)
            bool minimalFailFast = InFailFast.Value || (exception == PreallocatedOutOfMemoryException.Instance);
            InFailFast.Value = true;

            _crashInfo.Open(reason, message);

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
                {
                    Internal.Console.Error.Write(outputMessage);
                }
                Internal.Console.Error.Write(Environment.NewLine);

                if (exception != null)
                {
                    _crashInfo.WriteExceptionInfo(exception);
                }
            }

            _crashInfo.Close();

            // Try to map the failure into a HRESULT that makes sense
            int errorCode = exception != null ? exception.HResult : reason switch
            {
                RhFailFastReason.EnvironmentFailFast => HResults.COR_E_APPLICATION,
                RhFailFastReason.InternalError or
                RhFailFastReason.ClassLibDidNotTranslateExceptionID => HResults.COR_E_EXECUTIONENGINE,
                RhFailFastReason.UnhandledException or
                RhFailFastReason.UnhandledExceptionFromPInvoke or
                RhFailFastReason.UnhandledException_ExceptionDispatchNotAllowed or
                RhFailFastReason.UnhandledException_CallerDidNotHandle => HResults.E_ACCESSDENIED,
                _ => HResults.E_FAIL
            };

#if TARGET_WINDOWS
            Interop.Kernel32.RaiseFailFastException(errorCode, pExAddress, pExContext, _crashInfo.GetTriageBuffer(out int size), size);
#else
            Interop.Sys.Abort();
#endif
        }

        private static CrashInfo _crashInfo = new();

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
