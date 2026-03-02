// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace System.Net.Sockets
{
    internal sealed unsafe partial class SocketAsyncEngine
    {
        /// <summary>Parses an environment variable as a "0"/"1" boolean switch. Returns null if unset or unrecognized.</summary>
        private static bool? TryParseBoolSwitch(string? value)
        {
            if (string.Equals(value, "1", StringComparison.Ordinal)) return true;
            if (string.Equals(value, "0", StringComparison.Ordinal)) return false;
            return null;
        }

        private readonly struct IoUringConfigurationInputs
        {
            internal readonly string? IoUringEnvironmentValue;
            internal readonly bool IoUringFeatureSwitchEnabled;
            internal readonly string? SqPollEnvironmentValue;
            internal readonly bool SqPollFeatureSwitchEnabled;
            internal readonly string? DirectSqeEnvironmentValue;
            internal readonly string? ZeroCopySendEnvironmentValue;

            internal IoUringConfigurationInputs(
                string? ioUringEnvironmentValue,
                bool ioUringFeatureSwitchEnabled,
                string? sqPollEnvironmentValue,
                bool sqPollFeatureSwitchEnabled,
                string? directSqeEnvironmentValue,
                string? zeroCopySendEnvironmentValue)
            {
                IoUringEnvironmentValue = ioUringEnvironmentValue;
                IoUringFeatureSwitchEnabled = ioUringFeatureSwitchEnabled;
                SqPollEnvironmentValue = sqPollEnvironmentValue;
                SqPollFeatureSwitchEnabled = sqPollFeatureSwitchEnabled;
                DirectSqeEnvironmentValue = directSqeEnvironmentValue;
                ZeroCopySendEnvironmentValue = zeroCopySendEnvironmentValue;
            }
        }

        // One-time static lookup per process, following the standard .NET pattern
        // (e.g. GlobalizationMode). Configuration is not expected to change mid-process.
        private static readonly IoUringConfigurationInputs s_cachedConfigInputs = ReadIoUringConfigurationInputs();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IoUringResolvedConfiguration ResolveIoUringResolvedConfiguration()
        {
            IoUringConfigurationInputs inputs = s_cachedConfigInputs;
            return new IoUringResolvedConfiguration(
                ioUringEnabled: ResolveIoUringEnabled(inputs),
                sqPollRequested: ResolveSqPollRequested(inputs),
                directSqeDisabled: ResolveIoUringDirectSqeDisabled(inputs),
                zeroCopySendOptedIn: ResolveZeroCopySendOptedIn(inputs),
                registerBuffersEnabled: s_ioUringRegisterBuffersEnabled,
                adaptiveProvidedBufferSizingEnabled: s_ioUringAdaptiveBufferSizingEnabled,
                providedBufferSize: s_ioUringProvidedBufferSize,
                prepareQueueCapacity: s_ioUringPrepareQueueCapacity,
                cancellationQueueCapacity: s_ioUringCancellationQueueCapacity);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IoUringConfigurationInputs ReadIoUringConfigurationInputs()
        {
#if DEBUG
            string? directSqeValue = Environment.GetEnvironmentVariable(IoUringTestEnvironmentVariables.DirectSqe);
            string? zeroCopySendValue = Environment.GetEnvironmentVariable(IoUringTestEnvironmentVariables.ZeroCopySend);
#else
            string? directSqeValue = null;
            string? zeroCopySendValue = null;
#endif

            return new IoUringConfigurationInputs(
                ioUringEnvironmentValue: Environment.GetEnvironmentVariable(IoUringEnvironmentVariable),
                ioUringFeatureSwitchEnabled: IsIoUringFeatureEnabled,
                sqPollEnvironmentValue: Environment.GetEnvironmentVariable(IoUringSqPollEnvironmentVariable),
                sqPollFeatureSwitchEnabled: IsSqPollFeatureEnabled,
                directSqeEnvironmentValue: directSqeValue,
                zeroCopySendEnvironmentValue: zeroCopySendValue);
        }

        /// <summary>Checks whether io_uring is enabled.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsIoUringEnabled()
        {
            return ResolveIoUringEnabled(s_cachedConfigInputs);
        }

        [FeatureSwitchDefinition(UseIoUringAppContextSwitch)]
        private static bool IsIoUringFeatureEnabled
        {
            get
            {
                if (AppContext.TryGetSwitch(UseIoUringAppContextSwitch, out bool enabled))
                {
                    return enabled;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns whether SEND_ZC should be enabled.
        /// Defaults to enabled; test-only env var can disable for deterministic tests.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsZeroCopySendOptedIn()
        {
            return ResolveZeroCopySendOptedIn(s_cachedConfigInputs);
        }

        private static bool ResolveIoUringDirectSqeDisabled(in IoUringConfigurationInputs inputs)
        {
#if DEBUG
            // Test-only override for deterministic coverage.
            // Inverted: "0" disables direct SQE (returns true), "1" enables (returns false).
            bool? parsed = TryParseBoolSwitch(inputs.DirectSqeEnvironmentValue);
            if (parsed.HasValue) return !parsed.Value;
#endif
            return false;
        }

        private static bool ResolveIoUringEnabled(in IoUringConfigurationInputs inputs) =>
            TryParseBoolSwitch(inputs.IoUringEnvironmentValue) ?? inputs.IoUringFeatureSwitchEnabled;

        private static bool ResolveZeroCopySendOptedIn(in IoUringConfigurationInputs inputs)
        {
#if DEBUG
            bool? parsed = TryParseBoolSwitch(inputs.ZeroCopySendEnvironmentValue);
            if (parsed.HasValue) return parsed.Value;
#endif
            return true;
        }

        [FeatureSwitchDefinition(UseIoUringSqPollAppContextSwitch)]
        private static bool IsSqPollFeatureEnabled
        {
            get
            {
                if (AppContext.TryGetSwitch(UseIoUringSqPollAppContextSwitch, out bool enabled))
                {
                    return enabled;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns whether SQPOLL mode has been explicitly requested.
        /// Follows the standard .NET configuration pattern: environment variable
        /// overrides AppContext switch; either source alone is sufficient.
        /// </summary>
        private static bool IsSqPollRequested()
        {
            return ResolveSqPollRequested(s_cachedConfigInputs);
        }

        private static bool ResolveSqPollRequested(in IoUringConfigurationInputs inputs) =>
            TryParseBoolSwitch(inputs.SqPollEnvironmentValue) ?? inputs.SqPollFeatureSwitchEnabled;

        /// <summary>
        /// Returns whether multishot accept should be force-disabled.
        /// This is an emergency kill-switch to isolate multishot-accept issues
        /// while keeping other io_uring features enabled.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool IsMultishotAcceptDisabled() =>
            string.Equals(
                Environment.GetEnvironmentVariable(IoUringDisableMultishotAcceptEnvironmentVariable),
                "1",
                StringComparison.Ordinal);

        /// <summary>
        /// Returns whether SO_REUSEPORT accept distribution across io_uring engines is disabled.
        /// This is an emergency kill-switch; REUSEPORT accept is on by default when multiple
        /// engines are active. Setting the env var to "1" disables shadow listener creation.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool IsReusePortAcceptDisabled() =>
            string.Equals(
                Environment.GetEnvironmentVariable(IoUringDisableReusePortAcceptEnvironmentVariable),
                "1",
                StringComparison.Ordinal);

        private readonly struct PhysicalCoreGroup
        {
            internal PhysicalCoreGroup(int representativeCpu, int[] logicalCpus)
            {
                RepresentativeCpu = representativeCpu;
                LogicalCpus = logicalCpus;
            }

            internal int RepresentativeCpu { get; }
            internal int[] LogicalCpus { get; }
        }

        static partial void LinuxInitializeEngineAffinityTopology(ref int engineCount, ref int[]? pinnedCpuIndices, ref int[]? cpuToEngineIndex)
        {
            if (!OperatingSystem.IsLinux() || !IsIoUringEnabled())
            {
                return;
            }

            if (!TryDetectPhysicalCoreTopology(out PhysicalCoreGroup[]? groups) || groups is null || groups.Length == 0)
            {
                // Topology unavailable: keep one engine per logical CPU up to a defensive cap.
                int fallbackCount = Math.Max(1, Math.Min(Environment.ProcessorCount, 32));
                engineCount = Math.Min(engineCount, fallbackCount);
                return;
            }

            engineCount = Math.Min(engineCount, groups.Length);
            if (engineCount <= 0)
            {
                engineCount = 1;
            }

            pinnedCpuIndices = new int[engineCount];
            int maxCpuIndex = -1;
            for (int i = 0; i < engineCount; i++)
            {
                int representativeCpu = groups[i].RepresentativeCpu;
                pinnedCpuIndices[i] = representativeCpu;
                if (representativeCpu > maxCpuIndex)
                {
                    maxCpuIndex = representativeCpu;
                }

                int[] logicalCpus = groups[i].LogicalCpus;
                for (int j = 0; j < logicalCpus.Length; j++)
                {
                    if (logicalCpus[j] > maxCpuIndex)
                    {
                        maxCpuIndex = logicalCpus[j];
                    }
                }
            }

            int mapLength = Math.Max(Environment.ProcessorCount, maxCpuIndex + 1);
            cpuToEngineIndex = new int[mapLength];
            Array.Fill(cpuToEngineIndex, -1);

            for (int engineIndex = 0; engineIndex < engineCount; engineIndex++)
            {
                foreach (int cpu in groups[engineIndex].LogicalCpus)
                {
                    if ((uint)cpu < (uint)cpuToEngineIndex.Length)
                    {
                        cpuToEngineIndex[cpu] = engineIndex;
                    }
                }
            }
        }

        partial void LinuxPinEventLoopThreadIfConfigured()
        {
            if (_pinnedCpuIndex < 0 || _pinnedCpuIndex >= IntPtr.Size * 8 || !IsIoUringEnabled())
            {
                return;
            }

            IntPtr mask = (IntPtr)unchecked((nint)(1UL << _pinnedCpuIndex));
            if (Interop.Sys.SchedSetAffinity(0, ref mask) != 0)
            {
                return;
            }

        }

        private static bool TryDetectPhysicalCoreTopology([NotNullWhen(true)] out PhysicalCoreGroup[]? groups)
        {
            groups = null;
            const string cpuRoot = "/sys/devices/system/cpu";
            if (!Directory.Exists(cpuRoot))
            {
                return false;
            }

            IntPtr affinityMask = IntPtr.Zero;
            if (Interop.Sys.SchedGetAffinity(0, out affinityMask) != 0)
            {
                affinityMask = (IntPtr)(-1);
            }

            int affinityBitCount = IntPtr.Size * 8;
            var cpuDirectories = new List<int>();
            foreach (string cpuPath in Directory.EnumerateDirectories(cpuRoot, "cpu*"))
            {
                ReadOnlySpan<char> fileName = Path.GetFileName(cpuPath);
                if (!fileName.StartsWith("cpu", StringComparison.Ordinal))
                {
                    continue;
                }

                if (int.TryParse(fileName.Slice(3), out int cpuIndex) && cpuIndex >= 0)
                {
                    cpuDirectories.Add(cpuIndex);
                }
            }

            if (cpuDirectories.Count == 0)
            {
                return false;
            }

            cpuDirectories.Sort();
            var coreGroups = new Dictionary<(int PackageId, int CoreId), List<int>>();
            foreach (int cpuIndex in cpuDirectories)
            {
                if (cpuIndex >= affinityBitCount)
                {
                    // IntPtr affinity mask cannot represent CPUs above native pointer width.
                    continue;
                }

                nint cpuBit = (nint)(1UL << cpuIndex);
                if ((((nint)affinityMask) & cpuBit) == 0)
                {
                    continue;
                }

                if (!TryReadTopologyId(cpuIndex, "physical_package_id", out int packageId) ||
                    !TryReadTopologyId(cpuIndex, "core_id", out int coreId))
                {
                    continue;
                }

                var key = (packageId, coreId);
                if (!coreGroups.TryGetValue(key, out List<int>? logicalCpus))
                {
                    logicalCpus = new List<int>();
                    coreGroups.Add(key, logicalCpus);
                }

                logicalCpus.Add(cpuIndex);
            }

            if (coreGroups.Count == 0)
            {
                return false;
            }

            var orderedGroups = new List<PhysicalCoreGroup>(coreGroups.Count);
            foreach (KeyValuePair<(int PackageId, int CoreId), List<int>> entry in coreGroups)
            {
                List<int> logicalCpus = entry.Value;
                logicalCpus.Sort();
                orderedGroups.Add(new PhysicalCoreGroup(logicalCpus[0], logicalCpus.ToArray()));
            }

            orderedGroups.Sort(static (a, b) => a.RepresentativeCpu.CompareTo(b.RepresentativeCpu));
            groups = orderedGroups.ToArray();
            return true;
        }

        private static bool TryReadTopologyId(int cpuIndex, string fileName, out int value)
        {
            string path = $"/sys/devices/system/cpu/cpu{cpuIndex}/topology/{fileName}";
            value = 0;
            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                string raw = File.ReadAllText(path).Trim();
                return int.TryParse(raw, out value);
            }
            catch
            {
                return false;
            }
        }
    }
}
