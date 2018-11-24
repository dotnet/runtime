// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*============================================================
**
**
**
** Purpose: Capture synchronization semantics for asynchronous callbacks
**
** 
===========================================================*/

namespace System.Threading
{
    using Microsoft.Win32.SafeHandles;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ExceptionServices;
    using System.Runtime;
    using System.Runtime.Versioning;
    using System.Runtime.ConstrainedExecution;
    using System.Reflection;
    using System.Security;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;


    [Flags]
    internal enum SynchronizationContextProperties
    {
        None = 0,
        RequireWaitNotification = 0x1
    };

    public class SynchronizationContext
    {
        private SynchronizationContextProperties _props = SynchronizationContextProperties.None;

        public SynchronizationContext()
        {
        }


        // helper delegate to statically bind to Wait method
        private delegate int WaitDelegate(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout);

        private static Type s_cachedPreparedType1;
        private static Type s_cachedPreparedType2;
        private static Type s_cachedPreparedType3;
        private static Type s_cachedPreparedType4;
        private static Type s_cachedPreparedType5;

        // protected so that only the derived sync context class can enable these flags
        [SuppressMessage("Microsoft.Concurrency", "CA8001", Justification = "We never dereference s_cachedPreparedType*, so ordering is unimportant")]
        protected void SetWaitNotificationRequired()
        {
            //
            // Prepare the method so that it can be called in a reliable fashion when a wait is needed.
            // This will obviously only make the Wait reliable if the Wait method is itself reliable. The only thing
            // preparing the method here does is to ensure there is no failure point before the method execution begins.
            //
            // Preparing the method in this way is quite expensive, but only needs to be done once per type, per AppDomain.
            // So we keep track of a few types we've already prepared in this AD.  It is uncommon to have more than
            // a few SynchronizationContext implementations, so we only cache the first five we encounter; this lets
            // our cache be much faster than a more general cache might be.  This is important, because this
            // is a *very* hot code path for many WPF and WinForms apps.
            //
            Type type = this.GetType();
            if (s_cachedPreparedType1 != type &&
                s_cachedPreparedType2 != type &&
                s_cachedPreparedType3 != type &&
                s_cachedPreparedType4 != type &&
                s_cachedPreparedType5 != type)
            {
                RuntimeHelpers.PrepareDelegate(new WaitDelegate(this.Wait));

                if (s_cachedPreparedType1 == null) s_cachedPreparedType1 = type;
                else if (s_cachedPreparedType2 == null) s_cachedPreparedType2 = type;
                else if (s_cachedPreparedType3 == null) s_cachedPreparedType3 = type;
                else if (s_cachedPreparedType4 == null) s_cachedPreparedType4 = type;
                else if (s_cachedPreparedType5 == null) s_cachedPreparedType5 = type;
            }

            _props |= SynchronizationContextProperties.RequireWaitNotification;
        }

        public bool IsWaitNotificationRequired()
        {
            return ((_props & SynchronizationContextProperties.RequireWaitNotification) != 0);
        }


        public virtual void Send(SendOrPostCallback d, object state)
        {
            d(state);
        }

        public virtual void Post(SendOrPostCallback d, object state)
        {
            ThreadPool.QueueUserWorkItem(s => s.d(s.state), (d, state), preferLocal: false);
        }


        /// <summary>
        ///     Optional override for subclasses, for responding to notification that operation is starting.
        /// </summary>
        public virtual void OperationStarted()
        {
        }

        /// <summary>
        ///     Optional override for subclasses, for responding to notification that operation has completed.
        /// </summary>
        public virtual void OperationCompleted()
        {
        }

        // Method called when the CLR does a wait operation
        [CLSCompliant(false)]
        public virtual int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            return WaitHelper(waitHandles, waitAll, millisecondsTimeout);
        }

        // Method that can be called by Wait overrides
        [CLSCompliant(false)]
        protected static int WaitHelper(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            if (waitHandles == null)
            {
                throw new ArgumentNullException(nameof(waitHandles));
            }

            return WaitHelperNative(waitHandles, waitAll, millisecondsTimeout);
        }

        // Static helper to which the above method can delegate to in order to get the default
        // COM behavior.
        [CLSCompliant(false)]
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int WaitHelperNative(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout);

        public static void SetSynchronizationContext(SynchronizationContext syncContext)
        {
            Thread.CurrentThread.SynchronizationContext = syncContext;
        }

        public static SynchronizationContext Current
        {
            get
            {
                SynchronizationContext context = Thread.CurrentThread.SynchronizationContext;

#if FEATURE_APPX
                if (context == null && ApplicationModel.IsUap)
                    context = GetWinRTContext();
#endif

                return context;
            }
        }

#if FEATURE_APPX
        private static SynchronizationContext GetWinRTContext()
        {
            Debug.Assert(Environment.IsWinRTSupported);
            Debug.Assert(ApplicationModel.IsUap);

            //
            // We call into the VM to get the dispatcher.  This is because:
            //
            //  a) We cannot call the WinRT APIs directly from mscorlib, because we don't have the fancy projections here.
            //  b) We cannot call into System.Runtime.WindowsRuntime here, because we don't want to load that assembly
            //     into processes that don't need it (for performance reasons).
            //
            // So, we check the VM to see if the current thread has a dispatcher; if it does, we pass that along to
            // System.Runtime.WindowsRuntime to get a corresponding SynchronizationContext.
            //
            object dispatcher = GetWinRTDispatcherForCurrentThread();
            if (dispatcher != null)
                return GetWinRTSynchronizationContext(dispatcher);

            return null;
        }

        private static Func<object, SynchronizationContext> s_createSynchronizationContextDelegate = null;

        private static SynchronizationContext GetWinRTSynchronizationContext(object dispatcher)
        {
            //
            // Since we can't directly reference System.Runtime.WindowsRuntime from mscorlib, we have to get the factory via reflection.
            // It would be better if we could just implement WinRTSynchronizationContextFactory in mscorlib, but we can't, because
            // we can do very little with WinRT stuff in mscorlib.
            //
            Func<object, SynchronizationContext> createSynchronizationContextDelegate = s_createSynchronizationContextDelegate;
            if (createSynchronizationContextDelegate == null)
            {
                Type factoryType = Type.GetType("System.Threading.WinRTSynchronizationContextFactory, System.Runtime.WindowsRuntime", throwOnError: true);
                
                // Create an instance delegate for the Create static method
                MethodInfo createMethodInfo = factoryType.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                createSynchronizationContextDelegate = (Func<object, SynchronizationContext>)Delegate.CreateDelegate(typeof(Func<object, SynchronizationContext>), createMethodInfo, /* throwOnBindFailure */ true);

                s_createSynchronizationContextDelegate = createSynchronizationContextDelegate;
            }

            return s_createSynchronizationContextDelegate(dispatcher);
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Interface)]
        private static extern object GetWinRTDispatcherForCurrentThread();
#endif //FEATURE_APPX


        // helper to Clone this SynchronizationContext, 
        public virtual SynchronizationContext CreateCopy()
        {
            // the CLR dummy has an empty clone function - no member data
            return new SynchronizationContext();
        }

        private static int InvokeWaitMethodHelper(SynchronizationContext syncContext, IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            return syncContext.Wait(waitHandles, waitAll, millisecondsTimeout);
        }
    }
}
