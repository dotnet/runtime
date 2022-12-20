// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics.PerformanceData
{
    internal sealed class PerfProvider
    {
        internal Guid _providerGuid;
        internal int _counterSet;
        internal SafePerfProviderHandle _hProvider;

        internal PerfProvider(Guid providerGuid)
        {
            _providerGuid = providerGuid;
            uint Status = Interop.PerfCounter.PerfStartProvider(ref _providerGuid, null, out _hProvider);
            // ERROR_INVALID_PARAMETER, ERROR_OUTOFMEMORY
            if (Status != (uint)Interop.Errors.ERROR_SUCCESS)
            {
                throw new Win32Exception((int)Status);
            }
        }
    }

    internal static class PerfProviderCollection
    {
        // Internal global PERFLIB V2 provider collection that contains a collection of PerfProvider objects.
        // Use mutex to serialize collection initialization/update.
        private static object s_hiddenInternalSyncObject;
        private static readonly List<PerfProvider> s_providerList = new List<PerfProvider>();
        private static readonly Dictionary<object, int> s_counterSetList = new Dictionary<object, int>();
        private static readonly CounterType[] s_counterTypes = Enum.GetValues<CounterType>();
        private static readonly CounterSetInstanceType[] s_counterSetInstanceTypes = Enum.GetValues<CounterSetInstanceType>();

        private static object s_lockObject
        {
            get
            {
                if (s_hiddenInternalSyncObject == null)
                {
                    object o = new object();
                    Interlocked.CompareExchange(ref s_hiddenInternalSyncObject, o, null);
                }
                return s_hiddenInternalSyncObject;
            }
        }

        internal static PerfProvider QueryProvider(Guid providerGuid)
        {
            // Most of the cases should be that the application contains 1 provider that supports several CounterSets;
            // that is, ContainsKey should succeed except for the first time.
            lock (s_lockObject)
            {
                foreach (PerfProvider ProviderEntry in s_providerList)
                {
                    if (ProviderEntry._providerGuid == providerGuid)
                    {
                        return ProviderEntry;
                    }
                }

                PerfProvider NewProvider = new PerfProvider(providerGuid);
                s_providerList.Add(NewProvider);
                return NewProvider;
            }
        }

        internal static void RemoveProvider(Guid providerGuid)
        {
            lock (s_lockObject)
            {
                PerfProvider MatchedProvider = null;

                foreach (PerfProvider ProviderEntry in s_providerList)
                {
                    if (ProviderEntry._providerGuid == providerGuid)
                    {
                        MatchedProvider = ProviderEntry;
                    }
                }
                if (MatchedProvider != null)
                {
                    MatchedProvider._hProvider.Dispose();
                    s_providerList.Remove(MatchedProvider);
                }
            }
        }

        internal static void RegisterCounterSet(Guid counterSetGuid)
        {
            // Input counterSetGuid should not be registered yet. That is, ContainsKey() should fail most of times.
            lock (s_lockObject)
            {
                if (s_counterSetList.ContainsKey(counterSetGuid))
                {
                    throw new ArgumentException(SR.Format(SR.Perflib_Argument_CounterSetAlreadyRegister, counterSetGuid), nameof(counterSetGuid));
                }
                s_counterSetList.Add(counterSetGuid, 0);
            }
        }

        internal static void UnregisterCounterSet(Guid counterSetGuid)
        {
            lock (s_lockObject)
            {
                s_counterSetList.Remove(counterSetGuid);
            }
        }

        internal static bool ValidateCounterType(CounterType inCounterType)
        {
            foreach (CounterType DefinedCounterType in s_counterTypes)
            {
                if (DefinedCounterType == inCounterType)
                {
                    return true;
                }
            }
            return false;
        }

        internal static bool ValidateCounterSetInstanceType(CounterSetInstanceType inCounterSetInstanceType)
        {
            foreach (CounterSetInstanceType DefinedCounterSetInstanceType in s_counterSetInstanceTypes)
            {
                if (DefinedCounterSetInstanceType == inCounterSetInstanceType)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
