// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;

// Windows.Foundation.Diagnostics cannot be referenced from managed code because
// they're hidden by the metadata adapter. We redeclare the interfaces manually
// to be able to talk to native WinRT objects.

namespace Windows.Foundation.Diagnostics
{
    [ComImport]
    [Guid("50850B26-267E-451B-A890-AB6A370245EE")]
    [WindowsRuntimeImport]
    internal interface IAsyncCausalityTracerStatics
    {
        void TraceOperationCreation(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, string operationName, ulong relatedContext);
        void TraceOperationCompletion(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, AsyncCausalityStatus status);
        void TraceOperationRelation(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, CausalityRelation relation);
        void TraceSynchronousWorkStart(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, CausalitySynchronousWork work);
        void TraceSynchronousWorkCompletion(CausalityTraceLevel traceLevel, CausalitySource source, CausalitySynchronousWork work);
        //These next 2 functions could've been represented as an event except that the EventRegistrationToken wasn't being propagated to WinRT
        EventRegistrationToken add_TracingStatusChanged(System.EventHandler<TracingStatusChangedEventArgs> eventHandler);
        void remove_TracingStatusChanged(EventRegistrationToken token);
    }

    [ComImport]
    [Guid("410B7711-FF3B-477F-9C9A-D2EFDA302DC3")]
    [WindowsRuntimeImport]
    internal interface ITracingStatusChangedEventArgs
    {
        bool Enabled { get; }
        CausalityTraceLevel TraceLevel { get; }
    }

    // We need this dummy class to satisfy a QI when the TracingStatusChangedHandler
    // after being stored in a GIT cookie and then called by the WinRT API. This usually
    // happens when calling a Managed WinMD which access this feature.
    [ComImport]
    [Guid("410B7711-FF3B-477F-9C9A-D2EFDA302DC3")]
    [WindowsRuntimeImport]
    internal sealed class TracingStatusChangedEventArgs : ITracingStatusChangedEventArgs
    {
        public extern bool Enabled
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }

        public extern CausalityTraceLevel TraceLevel
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            get;
        }
    }

    internal enum CausalityRelation
    {
        AssignDelegate,
        Join,
        Choice,
        Cancel,
        Error
    }

    internal enum CausalitySource
    {
        Application,
        Library,
        System
    }

    internal enum CausalitySynchronousWork
    {
        CompletionNotification,
        ProgressNotification,
        Execution
    }

    internal enum CausalityTraceLevel
    {
        Required,
        Important,
        Verbose
    }

    internal enum AsyncCausalityStatus
    {
        Canceled = 2,
        Completed = 1,
        Error = 3,
        Started = 0
    }
}
