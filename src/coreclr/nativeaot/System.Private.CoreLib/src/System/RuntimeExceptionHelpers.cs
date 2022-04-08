// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;

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

    [ReflectionBlocked]
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
            PN_UnhandledException = 5,                           // ProjectN: "Unhandled exception: a managed exception was not handled before reaching unmanaged code"
            PN_UnhandledExceptionFromPInvoke = 6,                // ProjectN: "Unhandled exception: an unmanaged exception was thrown out of a managed-to-native transition."
            Max
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
                case RhFailFastReason.PN_UnhandledException:
                    return "Unhandled exception: a managed exception was not handled before reaching unmanaged code.";
                case RhFailFastReason.PN_UnhandledExceptionFromPInvoke:
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
#if FEATURE_DUMP_DEBUGGING
            // ReportUnhandledError will also call this in APPX scenarios,
            // but WinRT can failfast before we get another chance
            // (in APPX scenarios, this one will get overwritten by the one with the CCW pointer)
            GenerateExceptionInformationForDump(exception, IntPtr.Zero);
#endif

#if ENABLE_WINRT
            // If possible report the exception to GEH, if not fail fast.
            WinRTInteropCallbacks callbacks = WinRTInterop.UnsafeCallbacks;
            if (callbacks == null || !callbacks.ReportUnhandledError(exception))
                FailFast(GetStringForFailFastReason(RhFailFastReason.PN_UnhandledException), exception);
#else
            FailFast(GetStringForFailFastReason(RhFailFastReason.PN_UnhandledException), exception);
#endif
        }

        // This is the classlib-provided fail-fast function that will be invoked whenever the runtime
        // needs to cause the process to exit. It is the classlib's opprotunity to customize the
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
                    if ((reason == RhFailFastReason.PN_UnhandledException) && (exception != null))
                    {
                        Debug.WriteLine("Unhandled Exception: " + exception.ToString());
                    }

                    failFastMessage = string.Format("Runtime-generated FailFast: ({0}): {1}{2}",
                        reason.ToString(),  // Explicit call to ToString() to avoid MissingMetadataException inside String.Format()
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

#if FEATURE_DUMP_DEBUGGING
                GenerateExceptionInformationForDump(exception, IntPtr.Zero);
#endif
            }

#if TARGET_WINDOWS
            uint errorCode = 0x80004005; // E_FAIL
            // To help enable testing to bucket the failures we choose one of the following as errorCode:
            // * hashcode of EETypePtr if it is an unhandled managed exception
            // * HRESULT, if available
            // * RhFailFastReason, if it is one of the known reasons
            if (exception != null)
            {
                if (reason == RhFailFastReason.PN_UnhandledException)
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

#if FEATURE_DUMP_DEBUGGING

#pragma warning disable 414 // field is assigned, but never used -- This is because C# doesn't realize that we
        //                                      copy the field into a buffer.
        /// <summary>
        /// This is the header that describes our 'error report' buffer to the minidump auxiliary provider.
        /// Its format is know to that system-wide DLL, so do not change it.  The remainder of the buffer is
        /// opaque to the minidump auxiliary provider, so it'll have its own format that is more easily
        /// changed.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct ERROR_REPORT_BUFFER_HEADER
        {
            private int _headerSignature;
            private int _bufferByteCount;

            public void WriteHeader(int cbBuffer)
            {
                _headerSignature = 0x31304244;   // 'DB01'
                _bufferByteCount = cbBuffer;
            }
        }

        /// <summary>
        /// This header describes the contents of the serialized error report to DAC, which can deserialize it
        /// from a dump file or live debugging session.  This format is easier to change than the
        /// ERROR_REPORT_BUFFER_HEADER, but it is still well-known to DAC, so any changes must update the
        /// version number and also have corresponding changes made to DAC.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SERIALIZED_ERROR_REPORT_HEADER
        {
            private int _errorReportSignature;           // This is the version of the 'container format'.
            private int _exceptionSerializationVersion;  // This is the version of the Exception format.  It is
                                                         // separate from the 'container format' version since the
                                                         // implementation of the Exception serialization is owned by
                                                         // the Exception class.
            private int _exceptionCount;                 // We just contain a logical array of exceptions.
            private int _loadedModuleCount;              // Number of loaded modules. present when signature >= ER02.
            // {ExceptionCount} serialized Exceptions follow.
            // {LoadedModuleCount} module handles follow. present when signature >= ER02.

            public void WriteHeader(int nExceptions, int nLoadedModules)
            {
                _errorReportSignature = 0x32305245;  // 'ER02'
                _exceptionSerializationVersion = Exception.CurrentSerializationSignature;
                _exceptionCount = nExceptions;
                _loadedModuleCount = nLoadedModules;
            }
        }

        /// <summary>
        /// Holds metadata about an exception in flight. Class because ConditionalWeakTable only accepts reference types
        /// </summary>
        private class ExceptionData
        {
            public ExceptionData()
            {
                // Set this to a non-zero value so that logic mapping entries to threads
                // doesn't think an uninitialized ExceptionData is on thread 0
                ExceptionMetadata.ThreadId = 0xFFFFFFFF;
            }

            public struct ExceptionMetadataStruct
            {
                public uint ExceptionId { get; set; } // Id assigned to the exception. May not be contiguous or start at 0.
                public uint InnerExceptionId { get; set; } // ID of the inner exception or 0xFFFFFFFF for 'no inner exception'
                public uint ThreadId { get; set; } // Managed thread ID the eception was thrown on
                public int NestingLevel { get; set; } // If multiple exceptions are currently active on a thread, this gives the ordering for them.
                                                        // The highest number is the most recent exception. -1 means the exception is not currently in flight
                                                        // (but it may still be an InnerException).
                public IntPtr ExceptionCCWPtr { get; set; } // If the exception was thrown in an interop scenario, this contains the CCW pointer, otherwise, IntPtr.Zero
            }

            public ExceptionMetadataStruct ExceptionMetadata;

            /// <summary>
            /// Data created by Exception.SerializeForDump()
            /// </summary>
            public byte[] SerializedExceptionData { get; set; }

            /// <summary>
            /// Serializes the exception metadata and SerializedExceptionData
            /// </summary>
            public unsafe byte[] Serialize()
            {
                checked
                {
                    byte[] serializedData = new byte[sizeof(ExceptionMetadataStruct) + SerializedExceptionData.Length];
                    fixed (byte* pSerializedData = &serializedData[0])
                    {
                        ExceptionMetadataStruct* pMetadata = (ExceptionMetadataStruct*)pSerializedData;
                        pMetadata->ExceptionId = ExceptionMetadata.ExceptionId;
                        pMetadata->InnerExceptionId = ExceptionMetadata.InnerExceptionId;
                        pMetadata->ThreadId = ExceptionMetadata.ThreadId;
                        pMetadata->NestingLevel = ExceptionMetadata.NestingLevel;
                        pMetadata->ExceptionCCWPtr = ExceptionMetadata.ExceptionCCWPtr;

                        SerializedExceptionData.AsSpan().CopyTo(new Span<byte>(pSerializedData + sizeof(ExceptionMetadataStruct), SerializedExceptionData.Length));
                    }
                    return serializedData;
                }
            }
        }

        /// <summary>
        /// Table of exceptions that were on stacks triggering GenerateExceptionInformationForDump
        /// </summary>
        private static readonly ConditionalWeakTable<Exception, ExceptionData> s_exceptionDataTable = new ConditionalWeakTable<Exception, ExceptionData>();

        /// <summary>
        /// Counter for exception ID assignment
        /// </summary>
        private static int s_currentExceptionId;

        /// <summary>
        /// This method will call the runtime to gather the Exception objects from every exception dispatch in
        /// progress on the current thread.  It will then serialize them into a new buffer and pass that
        /// buffer back to the runtime, which will publish it to a place where a global "minidump auxiliary
        /// provider" will be able to save the buffer's contents into triage dumps.
        ///
        /// Thread safety information: The guarantee of this method is that the buffer it produces will have
        /// complete and correct information for all live exceptions on the current thread (as long as the same exception object
        /// is not thrown simultaneously on multiple threads). It will do a best-effort attempt to serialize information about exceptions
        /// already recorded on other threads, but that data can be lost or corrupted. The restrictions are:
        /// 1. Only exceptions active or recorded on the current thread have their table data modified.
        /// 2. After updating data in the table, we serialize a snapshot of the table (provided by ConditionalWeakTable.Values),
        ///    regardless of what other threads might do to the table before or after. However, because of #1, this thread's
        ///    exception data should stay stable
        /// 3. There is a dependency on the fact that ConditionalWeakTable's members are all threadsafe and that .Values returns a snapshot
        /// </summary>
        public static void GenerateExceptionInformationForDump(Exception currentException, IntPtr exceptionCCWPtr)
        {
            LowLevelList<byte[]> serializedExceptions = new LowLevelList<byte[]>();

            // If currentException is null, there's a state corrupting exception in flight and we can't serialize it
            if (currentException != null)
            {
                SerializeExceptionsForDump(currentException, exceptionCCWPtr, serializedExceptions);
            }

            GenerateErrorReportForDump(serializedExceptions);
        }

        private static void SerializeExceptionsForDump(Exception currentException, IntPtr exceptionCCWPtr, LowLevelList<byte[]> serializedExceptions)
        {
            const uint NoInnerExceptionValue = 0xFFFFFFFF;

            // Approximate upper size limit for the serialized exceptions (but we'll always serialize currentException)
            // If we hit the limit, because we serialize in arbitrary order, there may be missing InnerExceptions or nested exceptions.
            const int MaxBufferSize = 20000;

            int nExceptions;
            RuntimeImports.RhGetExceptionsForCurrentThread(null, out nExceptions);
            Exception[] curThreadExceptions = new Exception[nExceptions];
            RuntimeImports.RhGetExceptionsForCurrentThread(curThreadExceptions, out nExceptions);
            LowLevelList<Exception> exceptions = new LowLevelList<Exception>(curThreadExceptions);
            LowLevelList<Exception> nonThrownInnerExceptions = new LowLevelList<Exception>();

            uint currentThreadId = (uint)Environment.CurrentNativeThreadId;

            // Reset nesting levels for exceptions on this thread that might not be currently in flight
            foreach (KeyValuePair<Exception, ExceptionData> item in s_exceptionDataTable)
            {
                ExceptionData exceptionData = item.Value;
                if (exceptionData.ExceptionMetadata.ThreadId == currentThreadId)
                {
                    exceptionData.ExceptionMetadata.NestingLevel = -1;
                }
            }

            // Find all inner exceptions, even if they're not currently being handled
            for (int i = 0; i < exceptions.Count; i++)
            {
                if (exceptions[i].InnerException != null && !exceptions.Contains(exceptions[i].InnerException))
                {
                    exceptions.Add(exceptions[i].InnerException);
                    nonThrownInnerExceptions.Add(exceptions[i].InnerException);
                }
            }

            int currentNestingLevel = curThreadExceptions.Length - 1;

            // Make sure we serialize currentException
            if (!exceptions.Contains(currentException))
            {
                // When this happens, currentException is probably passed to this function through System.Environment.FailFast(), we
                // would want to treat as if this exception is last thrown in the current thread.
                exceptions.Insert(0, currentException);
                currentNestingLevel++;
            }

            // Populate exception data for all exceptions interesting to this thread.
            // Whether or not there was previously data for that object, it might have changed.
            for (int i = 0; i < exceptions.Count; i++)
            {
                ExceptionData exceptionData = s_exceptionDataTable.GetOrCreateValue(exceptions[i]);

                exceptionData.ExceptionMetadata.ExceptionId = (uint)System.Threading.Interlocked.Increment(ref s_currentExceptionId);
                if (exceptionData.ExceptionMetadata.ExceptionId == NoInnerExceptionValue)
                {
                    exceptionData.ExceptionMetadata.ExceptionId = (uint)System.Threading.Interlocked.Increment(ref s_currentExceptionId);
                }

                exceptionData.ExceptionMetadata.ThreadId = currentThreadId;

                // Only include nesting information for exceptions that were thrown on this thread
                if (!nonThrownInnerExceptions.Contains(exceptions[i]))
                {
                    exceptionData.ExceptionMetadata.NestingLevel = currentNestingLevel;
                    currentNestingLevel--;
                }
                else
                {
                    exceptionData.ExceptionMetadata.NestingLevel = -1;
                }

                // Only match the CCW pointer up to the current exception
                if (object.ReferenceEquals(exceptions[i], currentException))
                {
                    exceptionData.ExceptionMetadata.ExceptionCCWPtr = exceptionCCWPtr;
                }

                byte[] serializedEx = exceptions[i].SerializeForDump();
                exceptionData.SerializedExceptionData = serializedEx;
            }

            // Populate inner exception ids now that we have all of them in the table
            for (int i = 0; i < exceptions.Count; i++)
            {
                ExceptionData exceptionData;
                if (!s_exceptionDataTable.TryGetValue(exceptions[i], out exceptionData))
                {
                    // This shouldn't happen, but we can't meaningfully throw here
                    continue;
                }

                if (exceptions[i].InnerException != null)
                {
                    ExceptionData innerExceptionData;
                    if (s_exceptionDataTable.TryGetValue(exceptions[i].InnerException, out innerExceptionData))
                    {
                        exceptionData.ExceptionMetadata.InnerExceptionId = innerExceptionData.ExceptionMetadata.ExceptionId;
                    }
                }
                else
                {
                    exceptionData.ExceptionMetadata.InnerExceptionId = NoInnerExceptionValue;
                }
            }

            int totalSerializedExceptionSize = 0;
            // Make sure we include the current exception, regardless of buffer size
            ExceptionData currentExceptionData = null;
            if (s_exceptionDataTable.TryGetValue(currentException, out currentExceptionData))
            {
                byte[] serializedExceptionData = currentExceptionData.Serialize();
                serializedExceptions.Add(serializedExceptionData);
                totalSerializedExceptionSize = serializedExceptionData.Length;
            }

            checked
            {
                foreach (KeyValuePair<Exception, ExceptionData> item in s_exceptionDataTable)
                {
                    ExceptionData exceptionData = item.Value;

                    // Already serialized currentException
                    if (currentExceptionData != null && exceptionData.ExceptionMetadata.ExceptionId == currentExceptionData.ExceptionMetadata.ExceptionId)
                    {
                        continue;
                    }

                    byte[] serializedExceptionData = exceptionData.Serialize();
                    if (totalSerializedExceptionSize + serializedExceptionData.Length >= MaxBufferSize)
                    {
                        break;
                    }

                    serializedExceptions.Add(serializedExceptionData);
                    totalSerializedExceptionSize += serializedExceptionData.Length;
                }
            }
        }

        private static unsafe void GenerateErrorReportForDump(LowLevelList<byte[]> serializedExceptions)
        {
            checked
            {
                int loadedModuleCount = (int)RuntimeImports.RhGetLoadedOSModules(null);
                int cbModuleHandles = sizeof(System.IntPtr) * loadedModuleCount;
                int cbFinalBuffer = sizeof(ERROR_REPORT_BUFFER_HEADER) + sizeof(SERIALIZED_ERROR_REPORT_HEADER) + cbModuleHandles;
                for (int i = 0; i < serializedExceptions.Count; i++)
                {
                    cbFinalBuffer += serializedExceptions[i].Length;
                }

                byte[] finalBuffer = new byte[cbFinalBuffer];
                fixed (byte* pBuffer = &finalBuffer[0])
                {
                    byte* pCursor = pBuffer;
                    int cbRemaining = cbFinalBuffer;

                    ERROR_REPORT_BUFFER_HEADER* pDacHeader = (ERROR_REPORT_BUFFER_HEADER*)pCursor;
                    pDacHeader->WriteHeader(cbFinalBuffer);
                    pCursor += sizeof(ERROR_REPORT_BUFFER_HEADER);
                    cbRemaining -= sizeof(ERROR_REPORT_BUFFER_HEADER);

                    SERIALIZED_ERROR_REPORT_HEADER* pPayloadHeader = (SERIALIZED_ERROR_REPORT_HEADER*)pCursor;
                    pPayloadHeader->WriteHeader(serializedExceptions.Count, loadedModuleCount);
                    pCursor += sizeof(SERIALIZED_ERROR_REPORT_HEADER);
                    cbRemaining -= sizeof(SERIALIZED_ERROR_REPORT_HEADER);

                    // copy the serialized exceptions to report buffer
                    for (int i = 0; i < serializedExceptions.Count; i++)
                    {
                        int cbChunk = serializedExceptions[i].Length;
                        serializedExceptions[i].AsSpan().CopyTo(new Span<byte>(pCursor, cbChunk));
                        cbRemaining -= cbChunk;
                        pCursor += cbChunk;
                    }

                    // copy the module-handle array to report buffer
                    IntPtr[] loadedModuleHandles = new IntPtr[loadedModuleCount];
                    RuntimeImports.RhGetLoadedOSModules(loadedModuleHandles);
                    loadedModuleHandles.AsSpan().CopyTo(new Span<IntPtr>(pCursor, loadedModuleHandles.Length));
                    cbRemaining -= cbModuleHandles;
                    pCursor += cbModuleHandles;

                    Debug.Assert(cbRemaining == 0);
                }
                UpdateErrorReportBuffer(finalBuffer);
            }
        }

        private static GCHandle s_ExceptionInfoBufferPinningHandle;
        private static Lock s_ExceptionInfoBufferLock = new Lock();

        private static unsafe void UpdateErrorReportBuffer(byte[] finalBuffer)
        {
            Debug.Assert(finalBuffer?.Length > 0);

            using (LockHolder.Hold(s_ExceptionInfoBufferLock))
            {
                fixed (byte* pBuffer = &finalBuffer[0])
                {
                    byte* pPrevBuffer = (byte*)RuntimeImports.RhSetErrorInfoBuffer(pBuffer);
                    Debug.Assert(s_ExceptionInfoBufferPinningHandle.IsAllocated == (pPrevBuffer != null));
                    if (pPrevBuffer != null)
                    {
                        byte[] currentExceptionInfoBuffer = (byte[])s_ExceptionInfoBufferPinningHandle.Target;
                        Debug.Assert(currentExceptionInfoBuffer?.Length > 0);
                        fixed (byte* pPrev = &currentExceptionInfoBuffer[0])
                            Debug.Assert(pPrev == pPrevBuffer);
                    }
                    if (!s_ExceptionInfoBufferPinningHandle.IsAllocated)
                    {
                        // We allocate a pinning GC handle because we are logically giving the runtime 'unmanaged memory'.
                        s_ExceptionInfoBufferPinningHandle = GCHandle.Alloc(finalBuffer, GCHandleType.Pinned);
                    }
                    else
                    {
                        s_ExceptionInfoBufferPinningHandle.Target = finalBuffer;
                    }
                }
            }
        }
#endif // FEATURE_DUMP_DEBUGGING
    }
}
