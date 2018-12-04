// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//

using System;
using System.Security;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if FEATURE_COMINTEROP
using System.Runtime.InteropServices.WindowsRuntime;

using WFD = Windows.Foundation.Diagnostics;
#endif


namespace System.Threading.Tasks
{
    internal enum CausalityTraceLevel
    {
#if FEATURE_COMINTEROP
        Required = WFD.CausalityTraceLevel.Required,
        Important = WFD.CausalityTraceLevel.Important,
        Verbose = WFD.CausalityTraceLevel.Verbose
#else
        Required,
        Important,
        Verbose
#endif
    }

    internal enum AsyncCausalityStatus
    {
#if FEATURE_COMINTEROP
        Canceled = WFD.AsyncCausalityStatus.Canceled,
        Completed = WFD.AsyncCausalityStatus.Completed,
        Error = WFD.AsyncCausalityStatus.Error,
        Started = WFD.AsyncCausalityStatus.Started
#else
        Started,
        Completed,
        Canceled,
        Error
#endif
    }

    internal enum CausalityRelation
    {
#if FEATURE_COMINTEROP
        AssignDelegate = WFD.CausalityRelation.AssignDelegate,
        Join = WFD.CausalityRelation.Join,
        Choice = WFD.CausalityRelation.Choice,
        Cancel = WFD.CausalityRelation.Cancel,
        Error = WFD.CausalityRelation.Error
#else
        AssignDelegate,
        Join,
        Choice,
        Cancel,
        Error
#endif
    }

    internal enum CausalitySynchronousWork
    {
#if FEATURE_COMINTEROP
        CompletionNotification = WFD.CausalitySynchronousWork.CompletionNotification,
        ProgressNotification = WFD.CausalitySynchronousWork.ProgressNotification,
        Execution = WFD.CausalitySynchronousWork.Execution
#else
        CompletionNotification,
        ProgressNotification,
        Execution
#endif
    }

    internal static class AsyncCausalityTracer
    {
        internal static void EnableToETW(bool enabled)
        {
#if FEATURE_COMINTEROP
            if (enabled)
                f_LoggingOn |= Loggers.ETW;
            else
                f_LoggingOn &= ~Loggers.ETW;
#endif
        }

        internal static bool LoggingOn
        {
            get
            {
#if FEATURE_COMINTEROP
                return f_LoggingOn != 0;
#else
                return false;
#endif
            }
        }

#if FEATURE_COMINTEROP
        //s_PlatformId = {4B0171A6-F3D0-41A0-9B33-02550652B995}
        private static readonly Guid s_PlatformId = new Guid(0x4B0171A6, 0xF3D0, 0x41A0, 0x9B, 0x33, 0x02, 0x55, 0x06, 0x52, 0xB9, 0x95);

        //Indicates this information comes from the BCL Library
        private const WFD.CausalitySource s_CausalitySource = WFD.CausalitySource.Library;

        //Lazy initialize the actual factory
        private static WFD.IAsyncCausalityTracerStatics s_TracerFactory;

        // The loggers that this Tracer knows about. 
        [Flags]
        private enum Loggers : byte
        {
            CausalityTracer = 1,
            ETW = 2
        }


        //We receive the actual value for these as a callback
        private static Loggers f_LoggingOn; //assumes false by default

        // The precise static constructor will run first time somebody attempts to access this class
        static AsyncCausalityTracer()
        {
            if (!Environment.IsWinRTSupported) return;

            //COM Class Id
            string ClassId = "Windows.Foundation.Diagnostics.AsyncCausalityTracer";

            //COM Interface GUID  {50850B26-267E-451B-A890-AB6A370245EE}
            Guid guid = new Guid(0x50850B26, 0x267E, 0x451B, 0xA8, 0x90, 0XAB, 0x6A, 0x37, 0x02, 0x45, 0xEE);

            object factory = null;

            try
            {
                int hresult = Microsoft.Win32.UnsafeNativeMethods.RoGetActivationFactory(ClassId, ref guid, out factory);

                if (hresult < 0 || factory == null) return; //This prevents having an exception thrown in case IAsyncCausalityTracerStatics isn't registered.

                s_TracerFactory = (WFD.IAsyncCausalityTracerStatics)factory;

                EventRegistrationToken token = s_TracerFactory.add_TracingStatusChanged(new EventHandler<WFD.TracingStatusChangedEventArgs>(TracingStatusChangedHandler));
                Debug.Assert(token != default, "EventRegistrationToken is null");
            }
            catch (Exception ex)
            {
                // Although catching generic Exception is not recommended, this file is one exception
                // since we don't want to propagate any kind of exception to the user since all we are
                // doing here depends on internal state.
                LogAndDisable(ex);
            }
        }

        private static void TracingStatusChangedHandler(object sender, WFD.TracingStatusChangedEventArgs args)
        {
            if (args.Enabled)
                f_LoggingOn |= Loggers.CausalityTracer;
            else
                f_LoggingOn &= ~Loggers.CausalityTracer;
        }
#endif

        //
        // The TraceXXX methods should be called only if LoggingOn property returned true
        //
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Tracking is slow path. Disable inlining for it.
        internal static void TraceOperationCreation(CausalityTraceLevel traceLevel, int taskId, string operationName, ulong relatedContext)
        {
#if FEATURE_COMINTEROP
            try
            {
                if ((f_LoggingOn & Loggers.ETW) != 0)
                    TplEtwProvider.Log.TraceOperationBegin(taskId, operationName, (long)relatedContext);
                if ((f_LoggingOn & Loggers.CausalityTracer) != 0)
                    s_TracerFactory.TraceOperationCreation((WFD.CausalityTraceLevel)traceLevel, s_CausalitySource, s_PlatformId, GetOperationId((uint)taskId), operationName, relatedContext);
            }
            catch (Exception ex)
            {
                //view function comment
                LogAndDisable(ex);
            }
#endif
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        internal static void TraceOperationCompletion(CausalityTraceLevel traceLevel, int taskId, AsyncCausalityStatus status)
        {
#if FEATURE_COMINTEROP
            try
            {
                if ((f_LoggingOn & Loggers.ETW) != 0)
                    TplEtwProvider.Log.TraceOperationEnd(taskId, status);
                if ((f_LoggingOn & Loggers.CausalityTracer) != 0)
                    s_TracerFactory.TraceOperationCompletion((WFD.CausalityTraceLevel)traceLevel, s_CausalitySource, s_PlatformId, GetOperationId((uint)taskId), (WFD.AsyncCausalityStatus)status);
            }
            catch (Exception ex)
            {
                //view function comment
                LogAndDisable(ex);
            }
#endif
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        internal static void TraceOperationRelation(CausalityTraceLevel traceLevel, int taskId, CausalityRelation relation)
        {
#if FEATURE_COMINTEROP
            try
            {
                if ((f_LoggingOn & Loggers.ETW) != 0)
                    TplEtwProvider.Log.TraceOperationRelation(taskId, relation);
                if ((f_LoggingOn & Loggers.CausalityTracer) != 0)
                    s_TracerFactory.TraceOperationRelation((WFD.CausalityTraceLevel)traceLevel, s_CausalitySource, s_PlatformId, GetOperationId((uint)taskId), (WFD.CausalityRelation)relation);
            }
            catch (Exception ex)
            {
                //view function comment
                LogAndDisable(ex);
            }
#endif
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        internal static void TraceSynchronousWorkStart(CausalityTraceLevel traceLevel, int taskId, CausalitySynchronousWork work)
        {
#if FEATURE_COMINTEROP
            try
            {
                if ((f_LoggingOn & Loggers.ETW) != 0)
                    TplEtwProvider.Log.TraceSynchronousWorkBegin(taskId, work);
                if ((f_LoggingOn & Loggers.CausalityTracer) != 0)
                    s_TracerFactory.TraceSynchronousWorkStart((WFD.CausalityTraceLevel)traceLevel, s_CausalitySource, s_PlatformId, GetOperationId((uint)taskId), (WFD.CausalitySynchronousWork)work);
            }
            catch (Exception ex)
            {
                //view function comment
                LogAndDisable(ex);
            }
#endif
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        internal static void TraceSynchronousWorkCompletion(CausalityTraceLevel traceLevel, CausalitySynchronousWork work)
        {
#if FEATURE_COMINTEROP
            try
            {
                if ((f_LoggingOn & Loggers.ETW) != 0)
                    TplEtwProvider.Log.TraceSynchronousWorkEnd(work);
                if ((f_LoggingOn & Loggers.CausalityTracer) != 0)
                    s_TracerFactory.TraceSynchronousWorkCompletion((WFD.CausalityTraceLevel)traceLevel, s_CausalitySource, (WFD.CausalitySynchronousWork)work);
            }
            catch (Exception ex)
            {
                //view function comment
                LogAndDisable(ex);
            }
#endif
        }

#if FEATURE_COMINTEROP
        //fix for 796185: leaking internal exceptions to customers,
        //we should catch and log exceptions but never propagate them.
        private static void LogAndDisable(Exception ex)
        {
            f_LoggingOn = 0;
            Debugger.Log(0, "AsyncCausalityTracer", ex.ToString());
        }
#endif

        private static ulong GetOperationId(uint taskId)
        {
            return (((ulong)Thread.GetDomainID()) << 32) + taskId;
        }
    }
}
