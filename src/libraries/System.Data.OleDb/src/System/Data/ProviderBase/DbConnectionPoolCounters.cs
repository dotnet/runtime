// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Principal;

namespace System.Data.ProviderBase
{
    internal abstract class DbConnectionPoolCounters
    {
        private static class CreationData
        {
            internal static readonly CounterCreationData HardConnectsPerSecond = new CounterCreationData(
                                                                        "HardConnectsPerSecond",
                                                                        "The number of actual connections per second that are being made to servers",
                                                                        PerformanceCounterType.RateOfCountsPerSecond32);

            internal static readonly CounterCreationData HardDisconnectsPerSecond = new CounterCreationData(
                                                                        "HardDisconnectsPerSecond",
                                                                        "The number of actual disconnects per second that are being made to servers",
                                                                        PerformanceCounterType.RateOfCountsPerSecond32);

            internal static readonly CounterCreationData SoftConnectsPerSecond = new CounterCreationData(
                                                                        "SoftConnectsPerSecond",
                                                                        "The number of connections we get from the pool per second",
                                                                        PerformanceCounterType.RateOfCountsPerSecond32);

            internal static readonly CounterCreationData SoftDisconnectsPerSecond = new CounterCreationData(
                                                                        "SoftDisconnectsPerSecond",
                                                                        "The number of connections we return to the pool per second",
                                                                        PerformanceCounterType.RateOfCountsPerSecond32);

            internal static readonly CounterCreationData NumberOfNonPooledConnections = new CounterCreationData(
                                                                        "NumberOfNonPooledConnections",
                                                                        "The number of connections that are not using connection pooling",
                                                                        PerformanceCounterType.NumberOfItems32);

            internal static readonly CounterCreationData NumberOfPooledConnections = new CounterCreationData(
                                                                        "NumberOfPooledConnections",
                                                                        "The number of connections that are managed by the connection pooler",
                                                                        PerformanceCounterType.NumberOfItems32);

            internal static readonly CounterCreationData NumberOfActiveConnectionPoolGroups = new CounterCreationData(
                                                                        "NumberOfActiveConnectionPoolGroups",
                                                                        "The number of unique connection strings",
                                                                        PerformanceCounterType.NumberOfItems32);

            internal static readonly CounterCreationData NumberOfInactiveConnectionPoolGroups = new CounterCreationData(
                                                                        "NumberOfInactiveConnectionPoolGroups",
                                                                        "The number of unique connection strings waiting for pruning",
                                                                        PerformanceCounterType.NumberOfItems32);

            internal static readonly CounterCreationData NumberOfActiveConnectionPools = new CounterCreationData(
                                                                        "NumberOfActiveConnectionPools",
                                                                        "The number of connection pools",
                                                                        PerformanceCounterType.NumberOfItems32);

            internal static readonly CounterCreationData NumberOfInactiveConnectionPools = new CounterCreationData(
                                                                        "NumberOfInactiveConnectionPools",
                                                                        "The number of connection pools",
                                                                        PerformanceCounterType.NumberOfItems32);

            internal static readonly CounterCreationData NumberOfActiveConnections = new CounterCreationData(
                                                                        "NumberOfActiveConnections",
                                                                        "The number of connections currently in-use",
                                                                        PerformanceCounterType.NumberOfItems32);

            internal static readonly CounterCreationData NumberOfFreeConnections = new CounterCreationData(
                                                                        "NumberOfFreeConnections",
                                                                        "The number of connections currently available for use",
                                                                        PerformanceCounterType.NumberOfItems32);

            internal static readonly CounterCreationData NumberOfStasisConnections = new CounterCreationData(
                                                                        "NumberOfStasisConnections",
                                                                        "The number of connections currently waiting to be made ready for use",
                                                                        PerformanceCounterType.NumberOfItems32);

            internal static readonly CounterCreationData NumberOfReclaimedConnections = new CounterCreationData(
                                                                        "NumberOfReclaimedConnections",
                                                                        "The number of connections we reclaim from GC'd external connections",
                                                                        PerformanceCounterType.NumberOfItems32);
        };

        internal sealed class Counter
        {
            private PerformanceCounter? _instance;

            internal Counter(string? categoryName, string? instanceName, string counterName, PerformanceCounterType counterType)
            {
                if (ADP.IsPlatformNT5)
                {
                    try
                    {
                        if (!ADP.IsEmpty(categoryName) && !ADP.IsEmpty(instanceName))
                        {
                            PerformanceCounter instance = new PerformanceCounter();
                            instance.CategoryName = categoryName;
                            instance.CounterName = counterName;
                            instance.InstanceName = instanceName;
                            instance.InstanceLifetime = PerformanceCounterInstanceLifetime.Process;
                            instance.ReadOnly = false;
                            instance.RawValue = 0;  // make sure we start out at zero
                            _instance = instance;
                        }
                    }
                    catch (InvalidOperationException e)
                    {
                        ADP.TraceExceptionWithoutRethrow(e);
                        // TODO: generate Application EventLog entry about inability to find perf counter
                    }
                }
            }

            internal void Decrement()
            {
                PerformanceCounter? instance = _instance;
                instance?.Decrement();
            }

            internal void Dispose()
            { // TODO: race condition, Dispose at the same time as Increment/Decrement
                PerformanceCounter? instance = _instance;
                _instance = null;
                instance?.RemoveInstance();
            }

            internal void Increment()
            {
                _instance?.Increment();
            }
        };

        private const int CounterInstanceNameMaxLength = 127;

        internal readonly Counter HardConnectsPerSecond;
        internal readonly Counter HardDisconnectsPerSecond;
        internal readonly Counter SoftConnectsPerSecond;
        internal readonly Counter SoftDisconnectsPerSecond;
        internal readonly Counter NumberOfNonPooledConnections;
        internal readonly Counter NumberOfPooledConnections;
        internal readonly Counter NumberOfActiveConnectionPoolGroups;
        internal readonly Counter NumberOfInactiveConnectionPoolGroups;
        internal readonly Counter NumberOfActiveConnectionPools;
        internal readonly Counter NumberOfInactiveConnectionPools;
        internal readonly Counter NumberOfActiveConnections;
        internal readonly Counter NumberOfFreeConnections;
        internal readonly Counter NumberOfStasisConnections;
        internal readonly Counter NumberOfReclaimedConnections;

        protected DbConnectionPoolCounters() : this(null, null)
        {
        }

        protected DbConnectionPoolCounters(string? categoryName, string? categoryHelp)
        {
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(this.UnloadEventHandler);
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(this.ExitEventHandler);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(this.ExceptionEventHandler);

            string? instanceName = null;

            if (!ADP.IsEmpty(categoryName))
            {
                if (ADP.IsPlatformNT5)
                {
                    instanceName = GetInstanceName();
                }
            }

            // level 0-3: hard connects/disconnects, plus basic pool/pool entry statistics
            string? basicCategoryName = categoryName;
            HardConnectsPerSecond = new Counter(basicCategoryName, instanceName, CreationData.HardConnectsPerSecond.CounterName, CreationData.HardConnectsPerSecond.CounterType);
            HardDisconnectsPerSecond = new Counter(basicCategoryName, instanceName, CreationData.HardDisconnectsPerSecond.CounterName, CreationData.HardDisconnectsPerSecond.CounterType);
            NumberOfNonPooledConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfNonPooledConnections.CounterName, CreationData.NumberOfNonPooledConnections.CounterType);
            NumberOfPooledConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfPooledConnections.CounterName, CreationData.NumberOfPooledConnections.CounterType);
            NumberOfActiveConnectionPoolGroups = new Counter(basicCategoryName, instanceName, CreationData.NumberOfActiveConnectionPoolGroups.CounterName, CreationData.NumberOfActiveConnectionPoolGroups.CounterType);
            NumberOfInactiveConnectionPoolGroups = new Counter(basicCategoryName, instanceName, CreationData.NumberOfInactiveConnectionPoolGroups.CounterName, CreationData.NumberOfInactiveConnectionPoolGroups.CounterType);
            NumberOfActiveConnectionPools = new Counter(basicCategoryName, instanceName, CreationData.NumberOfActiveConnectionPools.CounterName, CreationData.NumberOfActiveConnectionPools.CounterType);
            NumberOfInactiveConnectionPools = new Counter(basicCategoryName, instanceName, CreationData.NumberOfInactiveConnectionPools.CounterName, CreationData.NumberOfInactiveConnectionPools.CounterType);
            NumberOfStasisConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfStasisConnections.CounterName, CreationData.NumberOfStasisConnections.CounterType);
            NumberOfReclaimedConnections = new Counter(basicCategoryName, instanceName, CreationData.NumberOfReclaimedConnections.CounterName, CreationData.NumberOfReclaimedConnections.CounterType);

            // level 4: expensive stuff
            string? verboseCategoryName = null;
            if (!ADP.IsEmpty(categoryName))
            {
                // don't load TraceSwitch if no categoryName so that Odbc/OleDb have a chance of not loading TraceSwitch
                // which are also used by System.Diagnostics.PerformanceCounter.ctor & System.Transactions.get_Current
                TraceSwitch perfCtrSwitch = new TraceSwitch("ConnectionPoolPerformanceCounterDetail", "level of detail to track with connection pool performance counters");
                if (TraceLevel.Verbose == perfCtrSwitch.Level)
                {
                    verboseCategoryName = categoryName;
                }
            }
            SoftConnectsPerSecond = new Counter(verboseCategoryName, instanceName, CreationData.SoftConnectsPerSecond.CounterName, CreationData.SoftConnectsPerSecond.CounterType);
            SoftDisconnectsPerSecond = new Counter(verboseCategoryName, instanceName, CreationData.SoftDisconnectsPerSecond.CounterName, CreationData.SoftDisconnectsPerSecond.CounterType);
            NumberOfActiveConnections = new Counter(verboseCategoryName, instanceName, CreationData.NumberOfActiveConnections.CounterName, CreationData.NumberOfActiveConnections.CounterType);
            NumberOfFreeConnections = new Counter(verboseCategoryName, instanceName, CreationData.NumberOfFreeConnections.CounterName, CreationData.NumberOfFreeConnections.CounterType);
        }
        private static string? GetAssemblyName()
        {
            string? result = null;

            // First try GetEntryAssembly name, then AppDomain.FriendlyName.
            Assembly? assembly = Assembly.GetEntryAssembly();

            if (null != assembly)
            {
                AssemblyName name = assembly.GetName();
                if (name != null)
                {
                    result = name.Name;
                }
            }
            return result;
        }

        // SxS: this method uses GetCurrentProcessId to construct the instance name.
        private static string GetInstanceName()
        {
            string? instanceName = GetAssemblyName(); // instance perfcounter name

            if (ADP.IsEmpty(instanceName))
            {
                AppDomain appDomain = AppDomain.CurrentDomain;
                if (null != appDomain)
                {
                    instanceName = appDomain.FriendlyName;
                }
            }

            uint pid = Interop.Kernel32.GetCurrentProcessId();

            // there are several characters which have special meaning
            // to PERFMON.  They recommend that we translate them as shown below, to
            // prevent problems.

            string result = $"{instanceName}[{pid}]";
            result = result.Replace('(', '[').Replace(')', ']').Replace('#', '_').Replace('/', '_').Replace('\\', '_');

            // counter instance name cannot be greater than 127
            if (result.Length > CounterInstanceNameMaxLength)
            {
                // Replacing the middle part with "[...]"
                // For example: if path is c:\long_path\very_(Ax200)_long__path\perftest.exe and process ID is 1234 than the resulted instance name will be:
                // c:\long_path\very_(AxM)[...](AxN)_long__path\perftest.exe[1234]
                // while M and N are adjusted to make each part before and after the [...] = 61 (making the total = 61 + 5 + 61 = 127)
                const string insertString = "[...]";
                int firstPartLength = (CounterInstanceNameMaxLength - insertString.Length) / 2;
                int lastPartLength = CounterInstanceNameMaxLength - firstPartLength - insertString.Length;
                result = $"{result.Substring(0, firstPartLength)}{insertString}{result.Substring(result.Length - lastPartLength, lastPartLength)}";

                Debug.Assert(result.Length == CounterInstanceNameMaxLength,
                    $"wrong calculation of the instance name: expected {CounterInstanceNameMaxLength}, actual: {result.Length}");
            }

            return result;
        }

        public void Dispose()
        {
            // ExceptionEventHandler with IsTerminiating may be called before
            // the Connection Close is called or the variables are initialized
            SafeDispose(HardConnectsPerSecond);
            SafeDispose(HardDisconnectsPerSecond);
            SafeDispose(SoftConnectsPerSecond);
            SafeDispose(SoftDisconnectsPerSecond);
            SafeDispose(NumberOfNonPooledConnections);
            SafeDispose(NumberOfPooledConnections);
            SafeDispose(NumberOfActiveConnectionPoolGroups);
            SafeDispose(NumberOfInactiveConnectionPoolGroups);
            SafeDispose(NumberOfActiveConnectionPools);
            SafeDispose(NumberOfActiveConnections);
            SafeDispose(NumberOfFreeConnections);
            SafeDispose(NumberOfStasisConnections);
            SafeDispose(NumberOfReclaimedConnections);
        }

        private static void SafeDispose(Counter counter)
        {
            counter?.Dispose();
        }

        private void ExceptionEventHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if ((null != e) && e.IsTerminating)
            {
                Dispose();
            }
        }

        private void ExitEventHandler(object? sender, EventArgs e)
        {
            Dispose();
        }

        private void UnloadEventHandler(object? sender, EventArgs e)
        {
            Dispose();
        }
    }

    internal sealed class DbConnectionPoolCountersNoCounters : DbConnectionPoolCounters
    {
        public static readonly DbConnectionPoolCountersNoCounters SingletonInstance = new DbConnectionPoolCountersNoCounters();

        private DbConnectionPoolCountersNoCounters() : base()
        {
        }
    }
}
