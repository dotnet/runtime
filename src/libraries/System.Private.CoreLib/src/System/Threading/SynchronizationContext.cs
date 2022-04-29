// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public partial class SynchronizationContext
    {
        private bool _requireWaitNotification;

        public SynchronizationContext()
        {
        }

        public static SynchronizationContext? Current => Thread.CurrentThread._synchronizationContext;

        protected void SetWaitNotificationRequired() => _requireWaitNotification = true;

        public bool IsWaitNotificationRequired() => _requireWaitNotification;

        public virtual void Send(SendOrPostCallback d, object? state) => d(state);

        public virtual void Post(SendOrPostCallback d, object? state) => ThreadPool.QueueUserWorkItem(static s => s.d(s.state), (d, state), preferLocal: false);

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

        [CLSCompliant(false)]
        public virtual int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            return WaitHelper(waitHandles, waitAll, millisecondsTimeout);
        }

        [CLSCompliant(false)]
        protected static int WaitHelper(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            ArgumentNullException.ThrowIfNull(waitHandles);

            return WaitHandle.WaitMultipleIgnoringSyncContext(waitHandles, waitAll, millisecondsTimeout);
        }

        public static void SetSynchronizationContext(SynchronizationContext? syncContext) => Thread.CurrentThread._synchronizationContext = syncContext;

        public virtual SynchronizationContext CreateCopy() => new SynchronizationContext();
    }
}
