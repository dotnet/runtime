// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private readonly long[] _data;
        private readonly int _index;

        /// <summary>
        /// CounterData constructor
        /// </summary>
        /// <param name="data"> The pinned array that stores raw counter data </param>
        /// <param name="index"> The index of this counter in <paramref name="data"/> </param>
        internal CounterData(long[] data, int index)
        {
            _data = data;
            _index = index;
        }

        /// <summary>
        /// Value property it used to query/update actual raw counter data.
        /// </summary>
        public long Value
        {
            get => Interlocked.Read(ref _data[_index]);
            set => Interlocked.Exchange(ref _data[_index], value);
        }

        public void Increment() => Interlocked.Increment(ref _data[_index]);

        public void Decrement() => Interlocked.Decrement(ref _data[_index]);

        public void IncrementBy(long value) => Interlocked.Add(ref _data[_index], value);

        /// <summary>
        /// RawValue property it used to query/update actual raw counter data.
        /// This property is not thread-safe and should only be used
        /// for performance-critical single-threaded access.
        /// </summary>
        public long RawValue
        {
            get => _data[_index];
            set => _data[_index] = value;
        }
    }

    /// <summary>
    /// CounterSetInstanceCounterDataSet is part of CounterSetInstance class, and is used to store raw counter data
    /// for all counters added in CounterSet.
    /// </summary>
    public sealed class CounterSetInstanceCounterDataSet : IDisposable
    {
        internal CounterSetInstance _instance;
        private readonly Dictionary<int, CounterData> _counters;
        private int _disposed;

        // Pinned so native perflib can hold a stable pointer to each counter.
        private readonly long[] _data;

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

                _data = GC.AllocateArray<long>(_instance._counterSet._idToCounter.Count, pinned: true);

                int CounterOffset = 0;

                foreach (KeyValuePair<int, CounterType> CounterDef in _instance._counterSet._idToCounter)
                {
                    CounterData thisCounterData = new CounterData(_data, CounterOffset);

                    _counters.Add(CounterDef.Key, thisCounterData);

                    // ArgumentNullException - CounterName is NULL
                    // ArgumentException - CounterName already exists.
                    uint Status = Interop.PerfCounter.PerfSetCounterRefValue(
                                    _instance._counterSet._provider._hProvider,
                                    _instance._nativeInst,
                                    (uint)CounterDef.Key,
                                    (void*)Marshal.UnsafeAddrOfPinnedArrayElement(_data, CounterOffset));
                    if (Status != (uint)Interop.Errors.ERROR_SUCCESS)
                    {
                        // ERROR_INVALID_PARAMETER or ERROR_NOT_FOUND
                        throw Status switch
                        {
                            (uint)Interop.Errors.ERROR_NOT_FOUND => new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_CounterRefValue, _instance._counterSet._counterSet, CounterDef.Key, _instance._instName)),

                            _ => new Win32Exception((int)Status),
                        };
                    }
                    CounterOffset++;
                }
            }
        }

        public void Dispose()
        {
            _disposed = 1;
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
                ArgumentNullException.ThrowIfNull(counterName);

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
