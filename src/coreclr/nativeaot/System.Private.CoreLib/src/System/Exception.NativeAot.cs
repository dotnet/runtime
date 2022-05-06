// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using MethodBase = System.Reflection.MethodBase;

namespace System
{
    public partial class Exception
    {
        public MethodBase? TargetSite
        {
            [RequiresUnreferencedCode("Metadata for the method might be incomplete or removed")]
            get
            {
                if (!HasBeenThrown)
                    return null;

                return new StackFrame(_corDbgStackTrace[0], needFileInfo: false).GetMethod();
            }
        }

        private static IDictionary CreateDataContainer() => new ListDictionaryInternal();

        private static string? SerializationWatsonBuckets => null;

        // WARNING: We allow diagnostic tools to directly inspect these three members (_message, _innerException and _HResult)
        // See https://github.com/dotnet/corert/blob/master/Documentation/design-docs/diagnostics/diagnostics-tools-contract.md for more details.
        // Please do not change the type, the name, or the semantic usage of this member without understanding the implication for tools.
        // Get in touch with the diagnostics team if you have questions.
        internal string? _message;
        private IDictionary? _data;
        private Exception? _innerException;
        private string? _helpURL;
        private string? _source;         // Mainly used by VB.
        private int _HResult;     // HResult

        // To maintain compatibility across runtimes, if this object was deserialized, it will store its stack trace as a string
        private string? _stackTraceString;
        private string? _remoteStackTraceString;

        internal IntPtr[] GetStackIPs()
        {
            IntPtr[] ips = new IntPtr[_idxFirstFreeStackTraceEntry];
            if (_corDbgStackTrace != null)
            {
                Array.Copy(_corDbgStackTrace, ips, ips.Length);
            }
            return ips;
        }

        // WARNING: We allow diagnostic tools to directly inspect these two members (_corDbgStackTrace and _idxFirstFreeStackTraceEntry)
        // See https://github.com/dotnet/corert/blob/master/Documentation/design-docs/diagnostics/diagnostics-tools-contract.md for more details.
        // Please do not change the type, the name, or the semantic usage of this member without understanding the implication for tools.
        // Get in touch with the diagnostics team if you have questions.

        // _corDbgStackTrace: Do not rename: This is for the use of the CorDbg interface. Contains the stack trace as an array of EIP's (ordered from
        // most nested call to least.) May also include a few "special" IP's from the SpecialIP class:
        private IntPtr[]? _corDbgStackTrace;
        private int _idxFirstFreeStackTraceEntry;

        internal static IntPtr EdiSeparator => (IntPtr)1;  // Marks a boundary where an ExceptionDispatchInfo rethrew an exception.

        private void AppendStackIP(IntPtr IP, bool isFirstRethrowFrame)
        {
            if (_idxFirstFreeStackTraceEntry == 0)
            {
                _corDbgStackTrace = new IntPtr[16];
            }
            else if (isFirstRethrowFrame)
            {
                // For the first frame after rethrow, we replace the last entry in the stack trace with the IP
                // of the rethrow.  This is overwriting the IP of where control left the corresponding try
                // region for the catch that is rethrowing.
                _corDbgStackTrace[_idxFirstFreeStackTraceEntry - 1] = IP;
                return;
            }

            if (_idxFirstFreeStackTraceEntry >= _corDbgStackTrace.Length)
                GrowStackTrace();

            _corDbgStackTrace[_idxFirstFreeStackTraceEntry++] = IP;
        }

        private void GrowStackTrace()
        {
            IntPtr[] newArray = new IntPtr[_corDbgStackTrace.Length * 2];
            for (int i = 0; i < _corDbgStackTrace.Length; i++)
            {
                newArray[i] = _corDbgStackTrace[i];
            }
            _corDbgStackTrace = newArray;
        }

        private bool HasBeenThrown => _idxFirstFreeStackTraceEntry != 0;

        private enum RhEHFrameType
        {
            RH_EH_FIRST_FRAME = 1,
            RH_EH_FIRST_RETHROW_FRAME = 2,
        }

        [RuntimeExport("AppendExceptionStackFrame")]
        private static void AppendExceptionStackFrame(object exceptionObj, IntPtr IP, int flags)
        {
            // This method is called by the runtime's EH dispatch code and is not allowed to leak exceptions
            // back into the dispatcher.
            try
            {
                Exception? ex = exceptionObj as Exception;
                if (ex == null)
                    Environment.FailFast("Exceptions must derive from the System.Exception class");

                if (!RuntimeExceptionHelpers.SafeToPerformRichExceptionSupport)
                    return;

                bool isFirstFrame = (flags & (int)RhEHFrameType.RH_EH_FIRST_FRAME) != 0;
                bool isFirstRethrowFrame = (flags & (int)RhEHFrameType.RH_EH_FIRST_RETHROW_FRAME) != 0;

                // When we're throwing an exception object, we first need to clear its stacktrace with two exceptions:
                // 1. Don't clear if we're rethrowing with `throw;`.
                // 2. Don't clear if we're throwing through ExceptionDispatchInfo.
                //    This is done through invoking RestoreDispatchState which sets the last frame to EdiSeparator followed by throwing normally using `throw ex;`.
                if (!isFirstRethrowFrame && isFirstFrame && ex._idxFirstFreeStackTraceEntry > 0 && ex._corDbgStackTrace[ex._idxFirstFreeStackTraceEntry - 1] != EdiSeparator)
                    ex._idxFirstFreeStackTraceEntry = 0;

                // If out of memory, avoid any calls that may allocate.  Otherwise, they may fail
                // with another OutOfMemoryException, which may lead to infinite recursion.
                bool fatalOutOfMemory = ex == PreallocatedOutOfMemoryException.Instance;

                if (!fatalOutOfMemory)
                    ex.AppendStackIP(IP, isFirstRethrowFrame);

                // UNIX-TODO: RhpEtwExceptionThrown
#if TARGET_WINDOWS
                if (isFirstFrame)
                {
                    string typeName = !fatalOutOfMemory  ? ex.GetType().ToString() : "System.OutOfMemoryException";
                    string message = !fatalOutOfMemory  ? ex.Message :
                        "Insufficient memory to continue the execution of the program.";

                    unsafe
                    {
                        fixed (char* exceptionTypeName = typeName, exceptionMessage = message)
                            RuntimeImports.RhpEtwExceptionThrown(exceptionTypeName, exceptionMessage, IP, ex.HResult);
                    }
                }
#endif
            }
            catch
            {
                // We may end up with a confusing stack trace or a confusing ETW trace log, but at least we
                // can continue to dispatch this exception.
            }
        }

        //==================================================================================================================
        // Support for ExceptionDispatchInfo class - imports and exports the stack trace.
        //==================================================================================================================

        internal DispatchState CaptureDispatchState()
        {
            IntPtr[]? stackTrace = _corDbgStackTrace;
            if (stackTrace != null)
            {
                IntPtr[] newStackTrace = new IntPtr[stackTrace.Length];
                Array.Copy(stackTrace, 0, newStackTrace, 0, stackTrace.Length);
                stackTrace = newStackTrace;
            }
            return new DispatchState(stackTrace);
        }

        internal void RestoreDispatchState(DispatchState DispatchState)
        {
            IntPtr[]? stackTrace = DispatchState.StackTrace;
            int idxFirstFreeStackTraceEntry = 0;
            if (stackTrace != null)
            {
                IntPtr[] newStackTrace = new IntPtr[stackTrace.Length + 1];
                Array.Copy(stackTrace, 0, newStackTrace, 0, stackTrace.Length);
                stackTrace = newStackTrace;
                while (stackTrace[idxFirstFreeStackTraceEntry] != (IntPtr)0)
                    idxFirstFreeStackTraceEntry++;
                stackTrace[idxFirstFreeStackTraceEntry++] = EdiSeparator;
            }

            // Since EDI can be created at various points during exception dispatch (e.g. at various frames on the stack) for the same exception instance,
            // they can have different data to be restored. Thus, to ensure atomicity of restoration from each EDI, perform the restore under a lock.
            lock (s_DispatchStateLock)
            {
                _corDbgStackTrace = stackTrace;
                _idxFirstFreeStackTraceEntry = idxFirstFreeStackTraceEntry;
            }
        }

        internal readonly struct DispatchState
        {
            public readonly IntPtr[]? StackTrace;

            public DispatchState(IntPtr[]? stackTrace)
            {
                StackTrace = stackTrace;
            }
        }

        // This is the object against which a lock will be taken
        // when attempt to restore the EDI. Since its static, its possible
        // that unrelated exception object restorations could get blocked
        // for a small duration but that sounds reasonable considering
        // such scenarios are going to be extremely rare, where timing
        // matches precisely.
        private static readonly object s_DispatchStateLock = new object();

        /// <summary>
        /// This is the binary format for serialized exceptions that get saved into a special buffer that is
        /// known to WER (by way of a runtime API) and will be saved into triage dumps.  This format is known
        /// to SOS, so any changes must update CurrentSerializationVersion and have corresponding updates to
        /// SOS.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SERIALIZED_EXCEPTION_HEADER
        {
            internal IntPtr ExceptionEEType;
            internal int HResult;
            internal int StackTraceElementCount;
            // IntPtr * N  : StackTrace elements
        }
        internal const int CurrentSerializationSignature = 0x31305845;  // 'EX01'

        /// <summary>
        /// This method performs the serialization of one Exception object into the returned byte[].
        /// </summary>
        internal unsafe byte[] SerializeForDump()
        {
            checked
            {
                int nStackTraceElements = _idxFirstFreeStackTraceEntry;
                int cbBuffer = sizeof(SERIALIZED_EXCEPTION_HEADER) + (nStackTraceElements * IntPtr.Size);

                byte[] buffer = new byte[cbBuffer];
                fixed (byte* pBuffer = &buffer[0])
                {
                    SERIALIZED_EXCEPTION_HEADER* pHeader = (SERIALIZED_EXCEPTION_HEADER*)pBuffer;
                    pHeader->HResult = _HResult;
                    pHeader->ExceptionEEType = (IntPtr)this.GetMethodTable();
                    pHeader->StackTraceElementCount = nStackTraceElements;
                    IntPtr* pStackTraceElements = (IntPtr*)(pBuffer + sizeof(SERIALIZED_EXCEPTION_HEADER));
                    for (int i = 0; i < nStackTraceElements; i++)
                    {
                        pStackTraceElements[i] = _corDbgStackTrace[i];
                    }
                }

                return buffer;
            }
        }

        // Returns true if setting the _remoteStackTraceString field is legal, false if not (immutable exception).
        // A false return value means the caller should early-exit the operation.
        // Can also throw InvalidOperationException if a stack trace is already set or if object has been thrown.
        private bool CanSetRemoteStackTrace()
        {
            // Check to see if the exception already has a stack set in it.
            if (HasBeenThrown || _stackTraceString != null || _remoteStackTraceString != null)
            {
                ThrowHelper.ThrowInvalidOperationException();
            }

            return true; // CoreRT runtime doesn't have immutable agile exceptions, always return true
        }
    }
}
