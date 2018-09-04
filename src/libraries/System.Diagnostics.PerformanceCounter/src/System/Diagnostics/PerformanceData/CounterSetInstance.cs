// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics.PerformanceData
{
    /// <summary>
    /// CounterSetInstance class maps to "Instance" in native performance counter implementation.
    /// </summary>
    public sealed class CounterSetInstance : IDisposable
    {
        internal CounterSet _counterSet;
        internal string _instName;
        private int _active;
        unsafe internal Interop.PerfCounter.PerfCounterSetInstanceStruct* _nativeInst;
        
        internal unsafe CounterSetInstance(CounterSet counterSetDefined, string instanceName)
        {
            if (counterSetDefined == null)
            {
                throw new ArgumentNullException(nameof(counterSetDefined));
            }
            if (instanceName == null)
            {
                throw new ArgumentNullException(nameof(instanceName));
            }
            if (instanceName.Length == 0)
            {
                throw new ArgumentException(SR.Perflib_Argument_EmptyInstanceName, nameof(instanceName));
            }

            _counterSet = counterSetDefined;
            _instName = instanceName;

            Debug.Assert(sizeof(Interop.PerfCounter.PerfCounterSetInstanceStruct) == 32);

            _nativeInst = Interop.PerfCounter.PerfCreateInstance(
                    _counterSet._provider._hProvider, ref _counterSet._counterSet, _instName, 0);
            int Status = (int)((_nativeInst != null) ? Interop.Errors.ERROR_SUCCESS : Marshal.GetLastWin32Error());
            if (_nativeInst != null)
            {
                Counters = new CounterSetInstanceCounterDataSet(this);
            }
            else
            {
                // ERROR_INVALID_PARAMETER,
                // ERROR_NOT_FOUND (cannot find installed CounterSet),
                // ERROR_ALREADY_EXISTS,
                // ERROR_NOT_ENOUGH_MEMORY

                switch (Status)
                {
                    case (int)Interop.Errors.ERROR_ALREADY_EXISTS:
                        throw new ArgumentException(SR.Format(SR.Perflib_Argument_InstanceAlreadyExists, _instName, _counterSet._counterSet), nameof(instanceName));

                    case (int)Interop.Errors.ERROR_NOT_FOUND:
                        throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_CounterSetNotInstalled, _counterSet._counterSet));

                    case (int)Interop.Errors.ERROR_INVALID_PARAMETER:
                        if (_counterSet._instType == CounterSetInstanceType.Single)
                        {
                            throw new ArgumentException(SR.Format(SR.Perflib_Argument_InvalidInstance, _counterSet._counterSet), nameof(instanceName));
                        }
                        else
                        {
                            throw new Win32Exception(Status);
                        }

                    default:
                        throw new Win32Exception(Status);
                }
            }

            _active = 1;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CounterSetInstance()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Counters != null)
                {
                    Counters.Dispose();
                    Counters = null;
                }
            }
            unsafe
            {
                if (_nativeInst != null && Interlocked.Exchange(ref _active, 0) != 0)
                {
                    lock (_counterSet)
                    {
                        if (_counterSet._provider != null)
                        {
                            uint Status = Interop.PerfCounter.PerfDeleteInstance(_counterSet._provider._hProvider, _nativeInst);
                        }
                        _nativeInst = null;
                    }
                }
            }
        }

        /// <summary>
        /// Access CounterSetInstanceCounterDataSet property. Developers can then use defined indexer to access
        /// specific CounterData object to query/update raw counter data.
        /// </summary>
        public CounterSetInstanceCounterDataSet Counters { get; private set; }
    }
}
