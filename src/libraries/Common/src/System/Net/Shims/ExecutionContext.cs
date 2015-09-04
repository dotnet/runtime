// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// IMPORTANT: This is temporary code.

namespace System.Threading
{
    internal sealed partial class ExecutionContext : System.IDisposable
    {
        private static ThreadLocal<ExecutionContext> s_threadEc = new ThreadLocal<ExecutionContext>(() => new ExecutionContext());

        internal ExecutionContext() { }

        [System.Security.SecuritySafeCriticalAttribute]
        public static System.Threading.ExecutionContext Capture()
        {
            return new ExecutionContext();
        }

        [System.Security.SecuritySafeCriticalAttribute]
        public System.Threading.ExecutionContext CreateCopy()
        {
            return new ExecutionContext();
        }

        public void Dispose() { }

        private bool _isFlowSuppressed = false;
        public static bool IsFlowSuppressed()
        {
            return s_threadEc.Value._isFlowSuppressed;
        }

        [System.Security.SecuritySafeCriticalAttribute]
        public static void RestoreFlow()
        {
            if (!IsFlowSuppressed())
            {
                throw new InvalidOperationException();
            }

            s_threadEc.Value._isFlowSuppressed = false;
        }

        [System.Security.SecurityCriticalAttribute]
        public static void Run(System.Threading.ExecutionContext executionContext, System.Threading.ContextCallback callback, object state)
        {
            // Minimal implementation of ExecutionContext flow to enable APM.
            callback(state);
        }

        [System.Security.SecurityCriticalAttribute]
        public static System.Threading.AsyncFlowControl SuppressFlow()
        {
            if (IsFlowSuppressed())
            {
                throw new InvalidOperationException();
            }

            s_threadEc.Value._isFlowSuppressed = true;
            return new AsyncFlowControl(s_threadEc.Value);
        }
    }

    internal struct AsyncFlowControl : IDisposable
    {
        private ExecutionContext _ec;
        public AsyncFlowControl(ExecutionContext ec)
        {
            _ec = ec;
        }

        public void Dispose()
        {
            if (_ec != null)
            {
                _ec.Dispose();
                _ec = null;
            }
        }
    }

    internal delegate void ContextCallback(object state);
}
