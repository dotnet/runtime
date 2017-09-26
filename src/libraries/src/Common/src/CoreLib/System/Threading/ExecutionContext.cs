// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Capture execution  context for a thread
**
**
===========================================================*/

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

using Thread = Internal.Runtime.Augments.RuntimeThread;

namespace System.Threading
{
    public delegate void ContextCallback(Object state);

    internal struct ExecutionContextSwitcher
    {
        internal ExecutionContext m_ec;
        internal SynchronizationContext m_sc;

        internal void Undo(Thread currentThread)
        {
            Debug.Assert(currentThread == Thread.CurrentThread);

            // The common case is that these have not changed, so avoid the cost of a write if not needed.
            if (currentThread.SynchronizationContext != m_sc)
            {
                currentThread.SynchronizationContext = m_sc;
            }

            if (currentThread.ExecutionContext != m_ec)
            {
                ExecutionContext.Restore(currentThread, m_ec);
            }
        }
    }

    public sealed class ExecutionContext : IDisposable, ISerializable
    {
        internal static readonly ExecutionContext Default = new ExecutionContext();

        private readonly IAsyncLocalValueMap m_localValues;
        private readonly IAsyncLocal[] m_localChangeNotifications;
        private readonly bool m_isFlowSuppressed;

        private ExecutionContext()
        {
            m_localValues = AsyncLocalValueMap.Empty;
            m_localChangeNotifications = Array.Empty<IAsyncLocal>();
        }

        private ExecutionContext(
            IAsyncLocalValueMap localValues,
            IAsyncLocal[] localChangeNotifications,
            bool isFlowSuppressed)
        {
            m_localValues = localValues;
            m_localChangeNotifications = localChangeNotifications;
            m_isFlowSuppressed = isFlowSuppressed;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public static ExecutionContext Capture()
        {
            ExecutionContext executionContext = Thread.CurrentThread.ExecutionContext;
            return
                executionContext == null ? Default :
                executionContext.m_isFlowSuppressed ? null :
                executionContext;
        }

        private ExecutionContext ShallowClone(bool isFlowSuppressed)
        {
            Debug.Assert(isFlowSuppressed != m_isFlowSuppressed);

            if (!isFlowSuppressed &&
                m_localValues == Default.m_localValues &&
                m_localChangeNotifications == Default.m_localChangeNotifications)
            {
                return null; // implies the default context
            }
            return new ExecutionContext(m_localValues, m_localChangeNotifications, isFlowSuppressed);
        }

        public static AsyncFlowControl SuppressFlow()
        {
            Thread currentThread = Thread.CurrentThread;
            ExecutionContext executionContext = currentThread.ExecutionContext ?? Default;
            if (executionContext.m_isFlowSuppressed)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CannotSupressFlowMultipleTimes);
            }

            executionContext = executionContext.ShallowClone(isFlowSuppressed: true);
            var asyncFlowControl = new AsyncFlowControl();
            currentThread.ExecutionContext = executionContext;
            asyncFlowControl.Initialize(currentThread);
            return asyncFlowControl;
        }

        public static void RestoreFlow()
        {
            Thread currentThread = Thread.CurrentThread;
            ExecutionContext executionContext = currentThread.ExecutionContext;
            if (executionContext == null || !executionContext.m_isFlowSuppressed)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CannotRestoreUnsupressedFlow);
            }

            currentThread.ExecutionContext = executionContext.ShallowClone(isFlowSuppressed: false);
        }

        public static bool IsFlowSuppressed()
        {
            ExecutionContext executionContext = Thread.CurrentThread.ExecutionContext;
            return executionContext != null && executionContext.m_isFlowSuppressed;
        }

        public static void Run(ExecutionContext executionContext, ContextCallback callback, Object state)
        {
            if (executionContext == null)
                throw new InvalidOperationException(SR.InvalidOperation_NullContext);

            Thread currentThread = Thread.CurrentThread;
            ExecutionContextSwitcher ecsw = default(ExecutionContextSwitcher);
            try
            {
                EstablishCopyOnWriteScope(currentThread, ref ecsw);
                ExecutionContext.Restore(currentThread, executionContext);
                callback(state);
            }
            catch
            {
                // Note: we have a "catch" rather than a "finally" because we want
                // to stop the first pass of EH here.  That way we can restore the previous
                // context before any of our callers' EH filters run.  That means we need to
                // end the scope separately in the non-exceptional case below.
                ecsw.Undo(currentThread);
                throw;
            }
            ecsw.Undo(currentThread);
        }

        internal static void Restore(Thread currentThread, ExecutionContext executionContext)
        {
            Debug.Assert(currentThread == Thread.CurrentThread);

            ExecutionContext previous = currentThread.ExecutionContext ?? Default;
            currentThread.ExecutionContext = executionContext;

            // New EC could be null if that's what ECS.Undo saved off.
            // For the purposes of dealing with context change, treat this as the default EC
            executionContext = executionContext ?? Default;

            if (previous != executionContext)
            {
                OnContextChanged(previous, executionContext);
            }
        }

        internal static void EstablishCopyOnWriteScope(Thread currentThread, ref ExecutionContextSwitcher ecsw)
        {
            Debug.Assert(currentThread == Thread.CurrentThread);

            ecsw.m_ec = currentThread.ExecutionContext;
            ecsw.m_sc = currentThread.SynchronizationContext;
        }

        private static void OnContextChanged(ExecutionContext previous, ExecutionContext current)
        {
            Debug.Assert(previous != null);
            Debug.Assert(current != null);
            Debug.Assert(previous != current);

            foreach (IAsyncLocal local in previous.m_localChangeNotifications)
            {
                object previousValue;
                object currentValue;
                previous.m_localValues.TryGetValue(local, out previousValue);
                current.m_localValues.TryGetValue(local, out currentValue);

                if (previousValue != currentValue)
                    local.OnValueChanged(previousValue, currentValue, true);
            }

            if (current.m_localChangeNotifications != previous.m_localChangeNotifications)
            {
                try
                {
                    foreach (IAsyncLocal local in current.m_localChangeNotifications)
                    {
                        // If the local has a value in the previous context, we already fired the event for that local
                        // in the code above.
                        object previousValue;
                        if (!previous.m_localValues.TryGetValue(local, out previousValue))
                        {
                            object currentValue;
                            current.m_localValues.TryGetValue(local, out currentValue);

                            if (previousValue != currentValue)
                                local.OnValueChanged(previousValue, currentValue, true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Environment.FailFast(
                        SR.ExecutionContext_ExceptionInAsyncLocalNotification,
                        ex);
                }
            }
        }

        internal static object GetLocalValue(IAsyncLocal local)
        {
            ExecutionContext current = Thread.CurrentThread.ExecutionContext;
            if (current == null)
                return null;

            object value;
            current.m_localValues.TryGetValue(local, out value);
            return value;
        }

        internal static void SetLocalValue(IAsyncLocal local, object newValue, bool needChangeNotifications)
        {
            ExecutionContext current = Thread.CurrentThread.ExecutionContext ?? ExecutionContext.Default;

            object previousValue;
            bool hadPreviousValue = current.m_localValues.TryGetValue(local, out previousValue);

            if (previousValue == newValue)
                return;

            IAsyncLocalValueMap newValues = current.m_localValues.Set(local, newValue);

            //
            // Either copy the change notification array, or create a new one, depending on whether we need to add a new item.
            //
            IAsyncLocal[] newChangeNotifications = current.m_localChangeNotifications;
            if (needChangeNotifications)
            {
                if (hadPreviousValue)
                {
                    Debug.Assert(Array.IndexOf(newChangeNotifications, local) >= 0);
                }
                else
                {
                    int newNotificationIndex = newChangeNotifications.Length;
                    Array.Resize(ref newChangeNotifications, newNotificationIndex + 1);
                    newChangeNotifications[newNotificationIndex] = local;
                }
            }

            Thread.CurrentThread.ExecutionContext =
                new ExecutionContext(newValues, newChangeNotifications, current.m_isFlowSuppressed);

            if (needChangeNotifications)
            {
                local.OnValueChanged(previousValue, newValue, false);
            }
        }

        public ExecutionContext CreateCopy()
        {
            return this; // since CoreCLR's ExecutionContext is immutable, we don't need to create copies.
        }

        public void Dispose()
        {
            // For CLR compat only
        }
    }

    public struct AsyncFlowControl : IDisposable
    {
        private Thread _thread;

        internal void Initialize(Thread currentThread)
        {
            Debug.Assert(currentThread == Thread.CurrentThread);
            _thread = currentThread;
        }

        public void Undo()
        {
            if (_thread == null)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CannotUseAFCMultiple);
            }
            if (Thread.CurrentThread != _thread)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CannotUseAFCOtherThread);
            }

            // An async flow control cannot be undone when a different execution context is applied. The desktop framework
            // mutates the execution context when its state changes, and only changes the instance when an execution context
            // is applied (for instance, through ExecutionContext.Run). The framework prevents a suppressed-flow execution
            // context from being applied by returning null from ExecutionContext.Capture, so the only type of execution
            // context that can be applied is one whose flow is not suppressed. After suppressing flow and changing an async
            // local's value, the desktop framework verifies that a different execution context has not been applied by
            // checking the execution context instance against the one saved from when flow was suppressed. In .NET Core,
            // since the execution context instance will change after changing the async local's value, it verifies that a
            // different execution context has not been applied, by instead ensuring that the current execution context's
            // flow is suppressed.
            if (!ExecutionContext.IsFlowSuppressed())
            {
                throw new InvalidOperationException(SR.InvalidOperation_AsyncFlowCtrlCtxMismatch);
            }

            _thread = null;
            ExecutionContext.RestoreFlow();
        }

        public void Dispose()
        {
            Undo();
        }

        public override bool Equals(object obj)
        {
            return obj is AsyncFlowControl && Equals((AsyncFlowControl)obj);
        }

        public bool Equals(AsyncFlowControl obj)
        {
            return _thread == obj._thread;
        }

        public override int GetHashCode()
        {
            return _thread?.GetHashCode() ?? 0;
        }

        public static bool operator ==(AsyncFlowControl a, AsyncFlowControl b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(AsyncFlowControl a, AsyncFlowControl b)
        {
            return !(a == b);
        }
    }
}
