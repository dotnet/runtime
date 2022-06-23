// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
    public partial class Exception : ISerializable
    {
        partial void RestoreRemoteStackTrace(SerializationInfo info, StreamingContext context)
        {
            // Get the WatsonBuckets that were serialized - this is particularly
            // done to support exceptions going across AD transitions.
            //
            // We use the no throw version since we could be deserializing a pre-V4
            // exception object that may not have this entry. In such a case, we would
            // get null.
            _watsonBuckets = (byte[]?)info.GetValueNoThrow("WatsonBuckets", typeof(byte[])); // Do not rename (binary serialization)

            // If we are constructing a new exception after a cross-appdomain call...
            if (context.State == StreamingContextStates.CrossAppDomain)
            {
                // ...this new exception may get thrown.  It is logically a re-throw, but
                //  physically a brand-new exception.  Since the stack trace is cleared
                //  on a new exception, the "_remoteStackTraceString" is provided to
                //  effectively import a stack trace from a "remote" exception.  So,
                //  move the _stackTraceString into the _remoteStackTraceString.  Note
                //  that if there is an existing _remoteStackTraceString, it will be
                //  preserved at the head of the new string, so everything works as
                //  expected.
                // Even if this exception is NOT thrown, things will still work as expected
                //  because the StackTrace property returns the concatenation of the
                //  _remoteStackTraceString and the _stackTraceString.
                _remoteStackTraceString += _stackTraceString;
                _stackTraceString = null;
            }
        }

        private IDictionary CreateDataContainer()
        {
            if (IsImmutableAgileException(this))
                return new EmptyReadOnlyDictionaryInternal();
            else
                return new ListDictionaryInternal();
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool IsImmutableAgileException(Exception e);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IRuntimeMethodInfo GetMethodFromStackTrace(object stackTrace);

        private MethodBase? GetExceptionMethodFromStackTrace()
        {
            Debug.Assert(_stackTrace != null, "_stackTrace shouldn't be null when this method is called");
            IRuntimeMethodInfo method = GetMethodFromStackTrace(_stackTrace!);

            // Under certain race conditions when exceptions are re-used, this can be null
            if (method == null)
                return null;

            return RuntimeType.GetMethodBase(method);
        }

        public MethodBase? TargetSite
        {
            [RequiresUnreferencedCode("Metadata for the method might be incomplete or removed")]
            get
            {
                if (_exceptionMethod != null)
                {
                    return _exceptionMethod;
                }
                if (_stackTrace == null)
                {
                    return null;
                }

                _exceptionMethod = GetExceptionMethodFromStackTrace();
                return _exceptionMethod;
            }
        }

        // This method will clear the _stackTrace of the exception object upon deserialization
        // to ensure that references from another AD/Process dont get accidentally used.
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            _stackTrace = null;

            // We wont serialize or deserialize the IP for Watson bucketing since
            // we dont know where the deserialized object will be used in.
            // Using it across process or an AppDomain could be invalid and result
            // in AV in the runtime.
            //
            // Hence, we set it to zero when deserialization takes place.
            _ipForWatsonBuckets = UIntPtr.Zero;
        }

        // This is used by the runtime when re-throwing a managed exception.  It will
        //  copy the stack trace to _remoteStackTraceString.
        internal void InternalPreserveStackTrace()
        {
            // Make sure that the _source field is initialized if Source is not overriden.
            // We want it to contain the original faulting point.
            _ = Source;

            string? tmpStackTraceString = StackTrace;

            if (!string.IsNullOrEmpty(tmpStackTraceString))
            {
                _remoteStackTraceString = tmpStackTraceString + Environment.NewLineConst;
            }

            _stackTrace = null;
            _stackTraceString = null;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void PrepareForForeignExceptionRaise();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void GetStackTracesDeepCopy(Exception exception, out byte[]? currentStackTrace, out object[]? dynamicMethodArray);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void SaveStackTracesFromDeepCopy(Exception exception, byte[]? currentStackTrace, object[]? dynamicMethodArray);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern uint GetExceptionCount();

        // This is invoked by ExceptionDispatchInfo.Throw to restore the exception stack trace, corresponding to the original throw of the
        // exception, just before the exception is "rethrown".
        internal void RestoreDispatchState(in DispatchState dispatchState)
        {
            // Restore only for non-preallocated exceptions
            if (!IsImmutableAgileException(this))
            {
                // When restoring back the fields, we again create a copy and set reference to them
                // in the exception object. This will ensure that when this exception is thrown and these
                // fields are modified, then EDI's references remain intact.
                //
                byte[]? stackTraceCopy = (byte[]?)dispatchState.StackTrace?.Clone();
                object[]? dynamicMethodsCopy = (object[]?)dispatchState.DynamicMethods?.Clone();

                // Watson buckets and remoteStackTraceString fields are captured and restored without any locks. It is possible for them to
                // get out of sync without violating overall integrity of the system.
                _watsonBuckets = dispatchState.WatsonBuckets;
                _ipForWatsonBuckets = dispatchState.IpForWatsonBuckets;
                _remoteStackTraceString = dispatchState.RemoteStackTrace;

                // The binary stack trace and references to dynamic methods have to be restored under a lock to guarantee integrity of the system.
                SaveStackTracesFromDeepCopy(this, stackTraceCopy, dynamicMethodsCopy);

                _stackTraceString = null;

                // Marks the TES state to indicate we have restored foreign exception
                // dispatch information.
                PrepareForForeignExceptionRaise();
            }
        }

        private MethodBase? _exceptionMethod;  // Needed for serialization.
        internal string? _message;
        private IDictionary? _data;
        private readonly Exception? _innerException;
        private string? _helpURL;
        private byte[]? _stackTrace;
        private byte[]? _watsonBuckets;
        private string? _stackTraceString; // Needed for serialization.
        private string? _remoteStackTraceString;
#pragma warning disable CA1823, 414  // Fields are not used from managed.
        // _dynamicMethods is an array of System.Resolver objects, used to keep
        // DynamicMethodDescs alive for the lifetime of the exception. We do this because
        // the _stackTrace field holds MethodDescs, and a DynamicMethodDesc can be destroyed
        // unless a System.Resolver object roots it.
        private readonly object[]? _dynamicMethods;
        private string? _source;         // Mainly used by VB.
        private UIntPtr _ipForWatsonBuckets; // Used to persist the IP for Watson Bucketing
        private readonly IntPtr _xptrs;             // Internal EE stuff
        private readonly int _xcode = _COMPlusExceptionCode;             // Internal EE stuff
#pragma warning restore CA1823, 414

        // @MANAGED: HResult is used from within the EE!  Rename with care - check VM directory
        private int _HResult;       // HResult

        // See src\inc\corexcep.h's EXCEPTION_COMPLUS definition:
        private const int _COMPlusExceptionCode = unchecked((int)0xe0434352);   // Win32 exception code for COM+ exceptions

        private bool HasBeenThrown => _stackTrace != null;

        private object? SerializationWatsonBuckets => _watsonBuckets;

        // This piece of infrastructure exists to help avoid deadlocks
        // between parts of CoreLib that might throw an exception while
        // holding a lock that are also used by CoreLib's ResourceManager
        // instance.  As a special case of code that may throw while holding
        // a lock, we also need to fix our asynchronous exceptions to use
        // Win32 resources as well (assuming we ever call a managed
        // constructor on instances of them).  We should grow this set of
        // exception messages as we discover problems, then move the resources
        // involved to native code.
        internal enum ExceptionMessageKind
        {
            ThreadAbort = 1,
            ThreadInterrupted = 2,
            OutOfMemory = 3
        }

        // See comment on ExceptionMessageKind
        internal static string GetMessageFromNativeResources(ExceptionMessageKind kind)
        {
            string? retMesg = null;
            GetMessageFromNativeResources(kind, new StringHandleOnStack(ref retMesg));
            return retMesg!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "ExceptionNative_GetMessageFromNativeResources")]
        private static partial void GetMessageFromNativeResources(ExceptionMessageKind kind, StringHandleOnStack retMesg);

        internal readonly struct DispatchState
        {
            public readonly byte[]? StackTrace;
            public readonly object[]? DynamicMethods;
            public readonly string? RemoteStackTrace;
            public readonly UIntPtr IpForWatsonBuckets;
            public readonly byte[]? WatsonBuckets;

            public DispatchState(
                byte[]? stackTrace,
                object[]? dynamicMethods,
                string? remoteStackTrace,
                UIntPtr ipForWatsonBuckets,
                byte[]? watsonBuckets)
            {
                StackTrace = stackTrace;
                DynamicMethods = dynamicMethods;
                RemoteStackTrace = remoteStackTrace;
                IpForWatsonBuckets = ipForWatsonBuckets;
                WatsonBuckets = watsonBuckets;
            }
        }

        internal DispatchState CaptureDispatchState()
        {
            GetStackTracesDeepCopy(this, out byte[]? stackTrace, out object[]? dynamicMethods);

            return new DispatchState(stackTrace, dynamicMethods,
                _remoteStackTraceString, _ipForWatsonBuckets, _watsonBuckets);
        }

        // Returns true if setting the _remoteStackTraceString field is legal, false if not (immutable exception).
        // A false return value means the caller should early-exit the operation.
        // Can also throw InvalidOperationException if a stack trace is already set or if object has been thrown.
        private bool CanSetRemoteStackTrace()
        {
            // If this is a preallocated singleton exception, silently skip the operation,
            // regardless of the value of throwIfHasExistingStack.
            if (IsImmutableAgileException(this))
            {
                return false;
            }

            // Check to see if the exception already has a stack set in it.
            if (_stackTrace != null || _stackTraceString != null || _remoteStackTraceString != null)
            {
                ThrowHelper.ThrowInvalidOperationException();
            }

            return true;
        }
    }
}
