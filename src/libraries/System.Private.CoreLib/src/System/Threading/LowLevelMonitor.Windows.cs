// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading
{
    internal unsafe partial struct LowLevelMonitor
    {
        internal struct Monitor
        {
            // We cannot allocate CRITICAL_SECTION on GC heap. The CRITICAL_SECTION documentation
            // explicitly says that critical section object cannot be moved or copied. The debug
            // info attached to critical section has back pointer to the owning CRITICAL_SECTION,
            // and moving CRITICAL_SECTION around makes this back pointer orphaned.
            public Interop.Kernel32.CRITICAL_SECTION _criticalSection;
            public Interop.Kernel32.CONDITION_VARIABLE _conditionVariable;
        }

        private Monitor* _pMonitor;

        public void Initialize()
        {
            _pMonitor = (Monitor*)Marshal.AllocHGlobal(sizeof(Monitor));

            Interop.Kernel32.InitializeCriticalSection(&_pMonitor->_criticalSection);
            Interop.Kernel32.InitializeConditionVariable(&_pMonitor->_conditionVariable);
        }

        private void DisposeCore()
        {
            if (_pMonitor == null)
            {
                return;
            }

            Interop.Kernel32.DeleteCriticalSection(&_pMonitor->_criticalSection);
            Marshal.FreeHGlobal((IntPtr)_pMonitor);
            _pMonitor = null;
        }

        private void AcquireCore()
        {
            Interop.Kernel32.EnterCriticalSection(&_pMonitor->_criticalSection);
        }

        private void ReleaseCore()
        {
            Interop.Kernel32.LeaveCriticalSection(&_pMonitor->_criticalSection);
        }

        private void WaitCore()
        {
            WaitCore(-1);
        }

        private bool WaitCore(int timeoutMilliseconds)
        {
            Debug.Assert(timeoutMilliseconds >= -1);
            return Interop.Kernel32.SleepConditionVariableCS(&_pMonitor->_conditionVariable, &_pMonitor->_criticalSection, timeoutMilliseconds);
        }

        private void Signal_ReleaseCore()
        {
            Interop.Kernel32.WakeConditionVariable(&_pMonitor->_conditionVariable);
            Interop.Kernel32.LeaveCriticalSection(&_pMonitor->_criticalSection);
        }
    }
}
