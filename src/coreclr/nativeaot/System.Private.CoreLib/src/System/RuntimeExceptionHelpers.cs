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
                        FailFastInternal("Access Violation: Attempted to read or write protected memory. This is often an indication that other memory is corrupt. The application will be terminated since this platform does not support throwing an AccessViolationException.");
                        return null;

                    case ExceptionIDs.DataMisaligned:
                        return new DataMisalignedException();

                    case ExceptionIDs.EntrypointNotFound:
                        return new EntryPointNotFoundException();

                    case ExceptionIDs.AmbiguousImplementation:
                        return new AmbiguousImplementationException();

                    default:
                        FailFastInternal("The runtime requires an exception for a case that this class library does not understand.");
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
            EnvironmentFailFast = 7,
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
        public static void FailFast(string message, Exception? exception)
        {
            FailFast(message, exception, RhFailFastReason.EnvironmentFailFast, IntPtr.Zero, IntPtr.Zero);
        }

        [DoesNotReturn]
        public static void FailFastInternal(string message)
        {
            FailFast(message, null, RhFailFastReason.InternalError, IntPtr.Zero, IntPtr.Zero);
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

            _comma = false;
            _currentBufferIndex = 0;
            _reservedBuffer = 1;            // Reserve 1 byte for closing }

            // Write the opening bracket and basic header which should never fail
            bool success = WriteChar('{');
            Debug.Assert(success);
            success = WriteHeader(reason, message);
            Debug.Assert(success);

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

                if (success && exception != null)
                {
                    if (!WriteBlock("exception", '{', '}', () => WriteException(exception, int.MaxValue, 500, int.MaxValue)))
                    {
                        // If the buffer isn't big enough to fit 500 stack frames, try limiting to 10
                        if (!WriteBlock("exception", '{', '}', () => WriteException(exception, int.MaxValue, 10, int.MaxValue)))
                        {
                            // If that fails, try limiting the size of the stack frame method names to 100 bytes
                            WriteBlock("exception", '{', '}', () => WriteException(exception, int.MaxValue, 10, 100));
                        }
                    }
                }
            }

            // Write the closing bracket which should never fail since it was reserved
            _reservedBuffer = 0;
            success = WriteChar('}');
            Debug.Assert(success);

            // Try to map the failure into a HRESULT that makes sense
            uint errorCode = exception != null ? (uint)exception.HResult : reason switch
            {
                RhFailFastReason.EnvironmentFailFast => COR_E_APPLICATION,
                RhFailFastReason.InternalError or
                RhFailFastReason.ClassLibDidNotTranslateExceptionID => COR_E_EXECUTIONENGINE,
                RhFailFastReason.UnhandledException or
                RhFailFastReason.UnhandledExceptionFromPInvoke or
                RhFailFastReason.UnhandledException_ExceptionDispatchNotAllowed or
                RhFailFastReason.UnhandledException_CallerDidNotHandle => E_ACCESSDENIED,
                _ => E_FAIL
            };

#if TARGET_WINDOWS
            fixed (void* triageBufferPtr = &_triageBuffer)
            {
                Interop.Kernel32.RaiseFailFastException(errorCode, pExAddress, pExContext, triageBufferPtr, _currentBufferIndex);
            }
#else
            Interop.Sys.Abort();
#endif
        }

        /// <summary>
        /// The kind or reason of crash for the triage JSON
        /// </summary>
        private enum CrashReason
        {
            Unknown = 0,
            UnhandledException = 1,
            EnvironmentFailFast = 2,
            InternalFailFast = 3,
        }

        /// <summary>
        /// Type of runtime (currently only NativeAOT is used)
        /// </summary>
        private enum RuntimeType
        {
            Unknown = 0,
            Desktop = 1,
            NetCore = 2,
            SingleFile = 3,
            NativeAOT = 4,
        }

        /// <summary>
        /// This block is passed to the Watson DOTNET CLRMA provider and must not have any GC references
        /// </summary>
        private unsafe struct TriageBlockBuffer
        {
            public const int BufferSize = 8192;
            public fixed byte Buffer[BufferSize];
        }

        private const uint E_FAIL = 0x80004005;
        private const uint E_ACCESSDENIED = 0x80070005;
        private const uint COR_E_APPLICATION = 0x80131600;
        private const uint COR_E_EXECUTIONENGINE = 0x80131506;

        private static bool _comma;
        private static int _currentBufferIndex;
        private static int _reservedBuffer;
        private static TriageBlockBuffer _triageBuffer;

        /// <summary>
        /// Writes the basic triage information header. Assumes there is always enough
        /// room in the buffer for this header.
        /// </summary>
        /// <param name="reason">kind of crash</param>
        /// <param name="message">fail fast message, limited to 1024 chars</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private static unsafe bool WriteHeader(RhFailFastReason reason, string message)
        {
            if (!WriteValue("version", "1.0.0"))
                return false;

            if (!WriteValue("runtime", new ReadOnlySpan<byte>(RuntimeImports.RhGetRuntimeVersion(out int cb), cb)))
                return false;

            if (!WriteValue("runtime_type", (int)RuntimeType.NativeAOT))
                return false;

            CrashReason crashReason = reason switch
            {
                RhFailFastReason.EnvironmentFailFast => CrashReason.EnvironmentFailFast,
                RhFailFastReason.InternalError or
                RhFailFastReason.ClassLibDidNotTranslateExceptionID => CrashReason.InternalFailFast,
                RhFailFastReason.UnhandledException or
                RhFailFastReason.UnhandledExceptionFromPInvoke or
                RhFailFastReason.UnhandledException_ExceptionDispatchNotAllowed or
                RhFailFastReason.UnhandledException_CallerDidNotHandle => CrashReason.UnhandledException,
                _ => CrashReason.Unknown,
            };

            if (!WriteValue("reason", (int)crashReason))
                return false;

            if (!WriteValue("message", message, max: 1024))
                return false;

            return true;
        }

        /// <summary>
        /// Adds the exception info to the JSON buffer
        /// </summary>
        /// <param name="exception">exception build triage block from</param>
        /// <param name="maxMessageSize">limits the size of the exception message strings</param>
        /// <param name="maxNumberStackFrames">limits the number of stack frames written to the triage buffer</param>
        /// <param name="maxMethodNameSize">limits the size of the stack frame method name strings</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private static unsafe bool WriteException(Exception exception, int maxMessageSize, int maxNumberStackFrames, int maxMethodNameSize)
        {
            if (!WriteHexValue("address", (ulong)Unsafe.AsPointer(ref exception)))
                return false;

            if (!WriteHexValue("hr", exception.HResult))
                return false;

            if (!WriteValue("message", exception.Message, maxMessageSize))
                return false;

            // Exception type names are not truncated because the full name is important to bucketing and usually not that long
            if (!WriteValue("type", exception.GetType().ToString()))
                return false;

            bool success = WriteBlock("stack", '[', ']', () =>
            {
                StackTrace stackTrace = new(exception);
                int count = 0;
                foreach (StackFrame frame in stackTrace.GetFrames())
                {
                    // Check if the stack frame limit has been hit
                    if (++count > maxNumberStackFrames)
                        break;

                    // Write as many stack frames that will fit
                    if (!WriteStackFrame(frame, maxMethodNameSize))
                        break;
                }
                return true;
            });

            if (!success)
                return false;

            AggregateException? aggregate = exception as AggregateException;
            if (aggregate is not null || exception.InnerException is not null)
            {
                success = WriteBlock("inner", '[', ']', () =>
                {
                    // Write as many inner exceptions that will fit
                    if (aggregate is not null)
                    {
                        foreach (Exception ex in aggregate.InnerExceptions)
                        {
                            if (!WriteBlock(null, '{', '}', () => WriteException(ex, maxMessageSize, maxNumberStackFrames, maxMethodNameSize)))
                                break;
                        }
                    }
                    else
                    {
                        WriteBlock(null, '{', '}', () => WriteException(exception.InnerException, maxMessageSize, maxNumberStackFrames, maxMethodNameSize));
                    }
                    return true;
                });

                if (!success)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Write an exception stack frame to the triage buffer
        /// </summary>
        /// <param name="frame">the stack frame instance to write</param>
        /// <param name="maxNameSize">limits the size of the frame type name</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private static bool WriteStackFrame(StackFrame frame, int maxNameSize)
        {
            return WriteBlock(null, '{', '}', () =>
            {
                nint ip = frame.GetNativeIPAddress();
                if (!WriteHexValue("ip", (ulong)ip))
                    return false;

                if (!WriteHexValue("offset", frame.GetNativeOffset()))
                    return false;

                string method = DeveloperExperience.GetMethodName(ip, out IntPtr _);
                if (method != null)
                {
                    if (!WriteValue("name", method, maxNameSize))
                        return false;
                }
                return true;
            });
        }

        private static bool WriteHexValue(ReadOnlySpan<char> key, ulong value) => WriteValue(key, string.Format($"0x{value:X}"));

        private static bool WriteHexValue(ReadOnlySpan<char> key, int value) => WriteValue(key, string.Format($"0x{value:X}"));

        private static bool WriteValue(ReadOnlySpan<char> key, int value) => WriteValue(key, value.ToString());

        private static bool WriteValue(ReadOnlySpan<char> key, string value, int max = int.MaxValue) => WriteBlock(key, '"', '"', () => WriteChars(value, max));

        /// <summary>
        /// Opens and closes an object or array. If the block can not fit in the triage buffer
        /// the allocations are backed out and the block is skipped.
        /// </summary>
        /// <param name="key">the name of the block</param>
        /// <param name="opening">opening char of the block { or [</param>
        /// <param name="closing">closing char of the block } or ]</param>
        /// <param name="callback">called to fill in the object or array values</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private static bool WriteBlock(ReadOnlySpan<char> key, char opening, char closing, Func<bool> callback)
        {
            int savedIndex = _currentBufferIndex;
            if (!OpenValue(key, opening))
                goto error;
            if (!callback())
                goto error;
            if (!CloseValue(closing))
                goto error;
            return true;
        error:
            _currentBufferIndex = savedIndex;
            return false;
        }

        /// <summary>
        /// Write raw bytes or already converted to UTF8 string to the triage buffer. If the block can not
        /// fit in the triage buffer the allocations are backed out and the value is skipped.
        /// </summary>
        /// <param name="key">the name of the value</param>
        /// <param name="bytes">value</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private static bool WriteValue(ReadOnlySpan<char> key, ReadOnlySpan<byte> bytes)
        {
            int savedIndex = _currentBufferIndex;
            if (!OpenValue(key, '"'))
                goto error;
            if (!WriteSpan(bytes))
                goto error;
            if (!CloseValue('"'))
                goto error;
            return true;
        error:
            _currentBufferIndex = savedIndex;
            return false;
        }

        private static bool OpenValue(ReadOnlySpan<char> key, char marker)
        {
            if (!WriteSeparator())
                return false;
            if (!key.IsEmpty)
            {
                if (!WriteChar('"'))
                    return false;
                if (!WriteChars(key))
                    return false;
                if (!WriteChar('"'))
                    return false;
                if (!WriteChar(':'))
                    return false;
            }
            if (!WriteChar(marker))
                return false;
            _comma = false;
            return true;
        }

        private static bool CloseValue(char marker)
        {
            if (!WriteChar(marker))
                return false;
            _comma = true;
            return true;
        }

        private static bool WriteSeparator() => _comma ? WriteChar(',') : true;

        private static bool WriteChar(char source) => WriteChars(new ReadOnlySpan<char>(source));

        private static bool WriteChars(ReadOnlySpan<char> chars, int max = int.MaxValue)
        {
            int size = Encoding.UTF8.GetByteCount(chars);
            size = Math.Min(size, max);
            Span<byte> destination = AllocBuffer(size);
            if (destination.IsEmpty)
            {
                return false;
            }
            Encoding.UTF8.GetBytes(chars.Slice(0, size), destination);
            return true;
        }

        private static bool WriteSpan(ReadOnlySpan<byte> bytes)
        {
            Span<byte> destination = AllocBuffer(bytes.Length);
            if (destination.IsEmpty)
            {
                return false;
            }
            bytes.CopyTo(destination);
            return true;
        }

        private static unsafe Span<byte> AllocBuffer(int size)
        {
            Debug.Assert(size > 0);

            fixed (byte* ptr = &_triageBuffer.Buffer[_currentBufferIndex])
            {
                // Check if there is any space left in the triage buffer
                if ((_currentBufferIndex + size) >= (TriageBlockBuffer.BufferSize - _reservedBuffer))
                {
                    return default;
                }
                _currentBufferIndex += size;
                return new Span<byte>(ptr, size);
            }
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
