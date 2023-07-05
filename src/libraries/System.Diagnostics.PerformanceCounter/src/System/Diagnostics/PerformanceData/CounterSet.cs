// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace System.Diagnostics.PerformanceData
{
    /// <summary>
    /// CounterSet is equivalent to "Counter Object" in native performance counter terminology,
    /// or "Counter Category" in previous framework releases. It defines a abstract grouping of
    /// counters, where each counter defines measurable matrix. In the new performance counter
    /// infrastructure, CounterSet is defined by GUID called CounterSetGuid, and is hosted inside
    /// provider application, which is also defined by another GUID called ProviderGuid.
    /// </summary>
    public class CounterSet : IDisposable
    {
        internal PerfProvider _provider;
        internal Guid _providerGuid;
        internal Guid _counterSet;
        internal CounterSetInstanceType _instType;
        private readonly object _lockObject;
        private bool _instanceCreated;
        internal Dictionary<string, int> _stringToId;
        internal Dictionary<int, CounterType> _idToCounter;

        /// <summary>
        /// CounterSet constructor.
        /// </summary>
        /// <param name="providerGuid">ProviderGuid identifies the provider application. A provider identified by ProviderGuid could publish several CounterSets defined by different CounterSetGuids</param>
        /// <param name="counterSetGuid">CounterSetGuid identifies the specific CounterSet. CounterSetGuid should be unique.</param>
        /// <param name="instanceType">One of defined CounterSetInstanceType values</param>
        public CounterSet(Guid providerGuid, Guid counterSetGuid, CounterSetInstanceType instanceType)
        {
            if (!PerfProviderCollection.ValidateCounterSetInstanceType(instanceType))
            {
                throw new ArgumentException(SR.Format(SR.Perflib_Argument_InvalidCounterSetInstanceType, instanceType), nameof(instanceType));
            }

            _providerGuid = providerGuid;
            _counterSet = counterSetGuid;
            _instType = instanceType;
            PerfProviderCollection.RegisterCounterSet(_counterSet);
            _provider = PerfProviderCollection.QueryProvider(_providerGuid);
            _lockObject = new object();
            _stringToId = new Dictionary<string, int>();
            _idToCounter = new Dictionary<int, CounterType>();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CounterSet()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            lock (this)
            {
                PerfProviderCollection.UnregisterCounterSet(_counterSet);

                if (_instanceCreated && _provider != null)
                {
                    lock (_lockObject)
                    {
                        if (_provider != null)
                        {
                            Interlocked.Decrement(ref _provider._counterSet);
                            if (_provider._counterSet <= 0)
                            {
                                PerfProviderCollection.RemoveProvider(_providerGuid);
                            }
                            _provider = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add non-displayable new counter to CounterSet; that is, perfmon would not display the counter.
        /// </summary>
        /// <param name="counterId">CounterId uniquely identifies the counter within CounterSet</param>
        /// <param name="counterType">One of defined CounterType values</param>
        public void AddCounter(int counterId, CounterType counterType)
        {
            if (_provider == null)
            {
                throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_NoActiveProvider, _providerGuid));
            }
            if (!PerfProviderCollection.ValidateCounterType(counterType))
            {
                throw new ArgumentException(SR.Format(SR.Perflib_Argument_InvalidCounterType, counterType), nameof(counterType));
            }
            if (_instanceCreated)
            {
                throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_AddCounterAfterInstance, _counterSet));
            }

            lock (_lockObject)
            {
                if (_instanceCreated)
                {
                    throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_AddCounterAfterInstance, _counterSet));
                }
                if (_idToCounter.ContainsKey(counterId))
                {
                    throw new ArgumentException(SR.Format(SR.Perflib_Argument_CounterAlreadyExists, counterId, _counterSet), nameof(counterId));
                }

                _idToCounter.Add(counterId, counterType);
            }
        }

        /// <summary>
        /// Add named new counter to CounterSet.
        /// </summary>
        /// <param name="counterId">CounterId uniquely identifies the counter within CounterSet</param>
        /// <param name="counterType">One of defined CounterType values</param>
        /// <param name="counterName">This is friendly name to help provider developers as indexer. and it might not match what is displayed in counter consumption applications lie perfmon.</param>
        public void AddCounter(int counterId, CounterType counterType, string counterName)
        {
            ArgumentNullException.ThrowIfNull(counterName);

            if (counterName.Length == 0)
            {
                throw new ArgumentException(SR.Perflib_Argument_EmptyCounterName, nameof(counterName));
            }
            if (!PerfProviderCollection.ValidateCounterType(counterType))
            {
                throw new ArgumentException(SR.Format(SR.Perflib_Argument_InvalidCounterType, counterType), nameof(counterType));
            }
            if (_provider == null)
            {
                throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_NoActiveProvider, _providerGuid));
            }
            if (_instanceCreated)
            {
                throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_AddCounterAfterInstance, _counterSet));
            }

            lock (_lockObject)
            {
                if (_instanceCreated)
                {
                    throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_AddCounterAfterInstance, _counterSet));
                }
                if (_stringToId.ContainsKey(counterName))
                {
                    throw new ArgumentException(SR.Format(SR.Perflib_Argument_CounterNameAlreadyExists, counterName, _counterSet), nameof(counterName));
                }
                if (_idToCounter.ContainsKey(counterId))
                {
                    throw new ArgumentException(SR.Format(SR.Perflib_Argument_CounterAlreadyExists, counterId, _counterSet), nameof(counterId));
                }

                _stringToId.Add(counterName, counterId);
                _idToCounter.Add(counterId, counterType);
            }
        }

        /// <summary>
        /// Create instances of the CounterSet. Created CounterSetInstance identifies active identity and tracks raw counter data for that identity.
        /// </summary>
        /// <param name="instanceName">Friendly name identifies the instance. InstanceName would be shown in counter consumption applications like perfmon.</param>
        /// <returns>CounterSetInstance object</returns>
        public CounterSetInstance CreateCounterSetInstance(string instanceName)
        {
            ArgumentNullException.ThrowIfNull(instanceName);

            if (instanceName.Length == 0)
            {
                throw new ArgumentException(SR.Perflib_Argument_EmptyInstanceName, nameof(instanceName));
            }
            if (_provider == null)
            {
                throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_NoActiveProvider, _providerGuid));
            }
            if (!_instanceCreated)
            {
                lock (_lockObject)
                {
                    if (!_instanceCreated)
                    {
                        if (_provider == null)
                        {
                            throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_NoActiveProvider, _providerGuid));
                        }
                        if (_provider._hProvider.IsInvalid)
                        {
                            throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_NoActiveProvider, _providerGuid));
                        }
                        if (_idToCounter.Count == 0)
                        {
                            throw new InvalidOperationException(SR.Format(SR.Perflib_InvalidOperation_CounterSetContainsNoCounter, _counterSet));
                        }

                        uint Status = (uint)Interop.Errors.ERROR_SUCCESS;

                        unsafe
                        {
                            uint CounterSetInfoSize = (uint)sizeof(Interop.PerfCounter.PerfCounterSetInfoStruct)
                                            + (uint)_idToCounter.Count * (uint)sizeof(Interop.PerfCounter.PerfCounterInfoStruct);
                            uint CounterSetInfoUsed = 0;
                            byte* CounterSetBuffer = stackalloc byte[(int)CounterSetInfoSize];

                            Debug.Assert(sizeof(Interop.PerfCounter.PerfCounterSetInfoStruct) == 40);
                            Debug.Assert(sizeof(Interop.PerfCounter.PerfCounterInfoStruct) == 32);

                            Interop.PerfCounter.PerfCounterSetInfoStruct* CounterSetInfo;
                            Interop.PerfCounter.PerfCounterInfoStruct* CounterInfo;

                            uint CurrentCounter = 0;
                            uint CurrentOffset = 0;

                            CounterSetInfo = (Interop.PerfCounter.PerfCounterSetInfoStruct*)CounterSetBuffer;
                            CounterSetInfo->CounterSetGuid = _counterSet;
                            CounterSetInfo->ProviderGuid = _providerGuid;
                            CounterSetInfo->NumCounters = (uint)_idToCounter.Count;
                            CounterSetInfo->InstanceType = (uint)_instType;

                            foreach (KeyValuePair<int, CounterType> CounterDef in _idToCounter)
                            {
                                CounterSetInfoUsed = (uint)sizeof(Interop.PerfCounter.PerfCounterSetInfoStruct)
                                                + (uint)CurrentCounter * (uint)sizeof(Interop.PerfCounter.PerfCounterInfoStruct);
                                if (CounterSetInfoUsed < CounterSetInfoSize)
                                {
                                    CounterInfo = (Interop.PerfCounter.PerfCounterInfoStruct*)(CounterSetBuffer + CounterSetInfoUsed);
                                    CounterInfo->CounterId = (uint)CounterDef.Key;
                                    CounterInfo->CounterType = (uint)CounterDef.Value;
                                    CounterInfo->Attrib = 0x0000000000000001;   // PERF_ATTRIB_BY_REFERENCE
                                    CounterInfo->Size = (uint)sizeof(void*); // always use pointer size
                                    CounterInfo->DetailLevel = 100;                  // PERF_DETAIL_NOVICE
                                    CounterInfo->Scale = 0;                    // Default scale
                                    CounterInfo->Offset = CurrentOffset;

                                    CurrentOffset += CounterInfo->Size;
                                }
                                CurrentCounter++;
                            }
                            Status = Interop.PerfCounter.PerfSetCounterSetInfo(_provider._hProvider, CounterSetInfo, CounterSetInfoSize);

                            // ERROR_INVALID_PARAMETER, ERROR_ALREADY_EXISTS, ERROR_NOT_ENOUGH_MEMORY, ERROR_OUTOFMEMORY
                            if (Status != (uint)Interop.Errors.ERROR_SUCCESS)
                            {
                                throw Status switch
                                {
                                    (uint)Interop.Errors.ERROR_ALREADY_EXISTS => new InvalidOperationException(SR.Format(SR.Perflib_Argument_CounterSetAlreadyRegister, _counterSet)),

                                    _ => new Win32Exception((int)Status),
                                };
                            }

                            Interlocked.Increment(ref _provider._counterSet);
                        }

                        _instanceCreated = true;
                    }
                }
            }

            CounterSetInstance thisInst = new CounterSetInstance(this, instanceName);
            return thisInst;
        }
    }
}
