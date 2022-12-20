// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics.Tracing;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    public partial class Exception
    {
        internal static uint GetExceptionCount()
        {
            return (uint)EventPipeInternal.GetRuntimeCounterValue(EventPipeInternal.RuntimeCounters.EXCEPTION_COUNT);
        }

        internal readonly struct DispatchState
        {
            public readonly MonoStackFrame[]? StackFrames;

            public DispatchState(MonoStackFrame[]? stackFrames)
            {
                StackFrames = stackFrames;
            }
        }

        #region Keep in sync with MonoException in object-internals.h
        private string? _unused1;
        internal string? _message;
        private IDictionary? _data;
        private Exception? _innerException;
        private string? _helpURL;
        private object? _traceIPs;
        private string? _stackTraceString;
        private string? _remoteStackTraceString;
        private int _unused4;
        private object[]? _dynamicMethods; // Dynamic methods referenced by the stack trace
        private int _HResult;
        private string? _source;
        private object? _unused6;
        internal MonoStackFrame[]? foreignExceptionsFrames;
        private IntPtr[]? native_trace_ips;
        private int caught_in_unmanaged;
        #endregion

        private bool HasBeenThrown => _traceIPs != null;

        public MethodBase? TargetSite
        {
            [RequiresUnreferencedCode("Metadata for the method might be incomplete or removed")]
            get
            {
                StackTrace st = new StackTrace(this, true);
                if (st.FrameCount > 0)
                    return st.GetFrame(0)?.GetMethod();

                return null;
            }
        }

        internal DispatchState CaptureDispatchState()
        {
            MonoStackFrame[]? stackFrames;

            if (_traceIPs != null)
            {
                stackFrames = Diagnostics.StackTrace.get_trace(this, 0, true);
                if (stackFrames.Length > 0)
                    stackFrames[stackFrames.Length - 1].isLastFrameFromForeignException = true;

                // Make sure foreignExceptionsFrames does not change at this point.
                // Otherwise, the Array.Copy into combinedStackFrames can fail due to size differences
                //
                // See https://github.com/dotnet/runtime/issues/70081
                MonoStackFrame[]? feFrames = foreignExceptionsFrames;

                if (feFrames != null)
                {
                    var combinedStackFrames = new MonoStackFrame[stackFrames.Length + feFrames.Length];
                    Array.Copy(feFrames, 0, combinedStackFrames, 0, feFrames.Length);
                    Array.Copy(stackFrames, 0, combinedStackFrames, feFrames.Length, stackFrames.Length);

                    stackFrames = combinedStackFrames;
                }
            }
            else
            {
                stackFrames = foreignExceptionsFrames;
            }

            return new DispatchState(stackFrames);
        }

        internal void RestoreDispatchState(in DispatchState state)
        {
            foreignExceptionsFrames = state.StackFrames;
            _stackTraceString = null;
        }

        // Returns true if setting the _remoteStackTraceString field is legal, false if not (immutable exception).
        // A false return value means the caller should early-exit the operation.
        // Can also throw InvalidOperationException if a stack trace is already set or if object has been thrown.
        private bool CanSetRemoteStackTrace()
        {
            if (_traceIPs != null || _stackTraceString != null || _remoteStackTraceString != null)
            {
                ThrowHelper.ThrowInvalidOperationException();
            }

            return true; // mono runtime doesn't have immutable agile exceptions, always return true
        }

        private static IDictionary CreateDataContainer() => new ListDictionaryInternal();

        private static string? SerializationWatsonBuckets => null;
    }
}
