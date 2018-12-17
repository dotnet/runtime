// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics.PerformanceData
{
    /// <summary>
    /// CounterData class is used to store actual raw counter data. It is the value element within
    /// CounterSetInstanceCounterDataSet, which is part of CounterSetInstance.
    /// </summary>    
    public sealed class CounterData
    {
        unsafe private long* _offset;

        /// <summary>
        /// CounterData constructor
        /// </summary>
        /// <param name="pCounterData"> The memory location to store raw counter data </param>        
        unsafe internal CounterData(long* pCounterData)
        {
            _offset = pCounterData;
            *_offset = 0;
        }

        /// <summary>
        /// Value property it used to query/update actual raw counter data.
        /// </summary>
        public long Value
        {
            get
            {
                unsafe
                {
                    return Interlocked.Read(ref (*_offset));
                }
            }
            set
            {
                unsafe
                {
                    Interlocked.Exchange(ref (*_offset), value);
                }
            }
        }

        public void Increment()
        {
            unsafe
            {
                Interlocked.Increment(ref (*_offset));
            }
        }

        public void Decrement()
        {
            unsafe
            {
                Interlocked.Decrement(ref (*_offset));
            }
        }

        public void IncrementBy(long value)
        {
            unsafe
            {
                Interlocked.Add(ref (*_offset), value);
            }
        }

        /// <summary>
        /// RawValue property it used to query/update actual raw counter data.
        /// This property is not thread-safe and should only be used 
        /// for performance-critical single-threaded access.
        /// </summary>
        public long RawValue
        {
            get
            {
                unsafe
                {
                    return (*_offset);
                }
            }
            set
            {
                unsafe
                {
                    *_offset = value;
                }
            }
        }
    }

    /// <summary>
    /// CounterSetInstanceCounterDataSet is part of CounterSetInstance class, and is used to store raw counter data
    /// for all counters added in CounterSet.
    /// </summary>    
    public sealed class CounterSetInstanceCounterDataSet : IDisposable
    {
        internal CounterSetInstance _instance;
        private Dictionary<int, CounterData> _counters;
        private int _disposed;
        unsafe internal byte* _dataBlock;
        
        internal CounterSetInstanceCounterDataSet(CounterSetInstance thisInst)
        {
            _instance = thisInst;
            _counters = new Dictionary<int, CounterData>();

            unsafe
            {
                if (_instance._counterSet._provider == null)
                {
                    throw new ArgumentException(SR.Format(SR.Perflib_Argument_ProviderNotFound, _instance._counterSet._providerGuid), "ProviderGuid");
                }
                if (_instance._counterSet._provider._hProvider.IsInvalid)
                {
                    throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_NoActiveProvider, _instance._counterSet._providerGuid));
                }

                _dataBlock = (byte*)Marshal.AllocHGlobal(_instance._counterSet._idToCounter.Count * sizeof(long));
                if (_dataBlock == null)
                {
                    throw new InsufficientMemoryException(SR.Format(SR.Perflib_InsufficientMemory_InstanceCounterBlock, _instance._counterSet._counterSet, _instance._instName));
                }

                int CounterOffset = 0;

                foreach (KeyValuePair<int, CounterType> CounterDef in _instance._counterSet._idToCounter)
                {
                    CounterData thisCounterData = new CounterData((long*)(_dataBlock + CounterOffset * sizeof(long)));

                    _counters.Add(CounterDef.Key, thisCounterData);

                    // ArgumentNullException - CounterName is NULL
                    // ArgumentException - CounterName already exists.
                    uint Status = Interop.PerfCounter.PerfSetCounterRefValue(
                                    _instance._counterSet._provider._hProvider,
                                    _instance._nativeInst,
                                    (uint)CounterDef.Key,
                                    (void*)(_dataBlock + CounterOffset * sizeof(long)));
                    if (Status != (uint)Interop.Errors.ERROR_SUCCESS)
                    {
                        Dispose(true);

                        // ERROR_INVALID_PARAMETER or ERROR_NOT_FOUND
                        switch (Status)
                        {
                            case (uint)Interop.Errors.ERROR_NOT_FOUND:
                                throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_CounterRefValue, _instance._counterSet._counterSet, CounterDef.Key, _instance._instName));

                            default:
                                throw new Win32Exception((int)Status);
                        }
                    }
                    CounterOffset++;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CounterSetInstanceCounterDataSet()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                unsafe
                {
                    if (_dataBlock != null)
                    {
                        // Need to free allocated heap memory that is used to store all raw counter data.
                        Marshal.FreeHGlobal((IntPtr)_dataBlock);
                        _dataBlock = null;
                    }
                }
            }
        }

        /// <summary>
        /// CounterId indexer to access specific CounterData object.
        /// </summary>
        /// <param name="counterId">CounterId that matches one CounterSet::AddCounter()call</param>
        /// <returns>CounterData object with matched counterId</returns>
        public CounterData this[int counterId]
        {
            get
            {
                if (_disposed != 0)
                {
                    return null;
                }

                try
                {
                    return _counters[counterId];
                }
                catch (KeyNotFoundException)
                {
                    return null;
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// CounterName indexer to access specific CounterData object.
        /// </summary>
        /// <param name="counterName">CounterName that matches one CounterSet::AddCounter() call</param>
        /// <returns>CounterData object with matched counterName</returns>        
        public CounterData this[string counterName]
        {
            get
            {
                if (counterName == null)
                {
                    throw new ArgumentNullException(nameof(counterName));
                }
                if (counterName.Length == 0)
                {
                    throw new ArgumentNullException(nameof(counterName));
                }
                if (_disposed != 0)
                {
                    return null;
                }

                try
                {
                    int CounterId = _instance._counterSet._stringToId[counterName];
                    try
                    {
                        return _counters[CounterId];
                    }
                    catch (KeyNotFoundException)
                    {
                        return null;
                    }
                    catch
                    {
                        throw;
                    }
                }
                catch (KeyNotFoundException)
                {
                    return null;
                }
                catch
                {
                    throw;
                }
            }
        }
    }
}
