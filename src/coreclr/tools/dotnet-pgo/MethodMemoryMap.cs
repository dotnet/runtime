// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ILCompiler.Reflection.ReadyToRun;
using Internal.TypeSystem;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    // A map that can be used to resolve memory addresses back to the MethodDesc.
    internal class MethodMemoryMap
    {
        private readonly ulong[] _infoKeys;
        public readonly MemoryRegionInfo[] _infos;

        struct JittedID : IEquatable<JittedID>
        {
            public JittedID(long methodID, long reJITID)
            {
                MethodID = methodID;
                ReJITID = reJITID;
            }

            public readonly long MethodID;
            public readonly long ReJITID;

            public override int GetHashCode() => HashCode.Combine(MethodID, ReJITID);
            public override bool Equals([NotNullWhen(true)] object obj) => obj is JittedID id ? Equals(id) : false;

            public bool Equals(JittedID other)
            {
                if (other.MethodID != MethodID)
                    return false;
                return other.ReJITID == ReJITID;
            }
        }

        public MethodMemoryMap(
            TraceProcess p,
            TraceTypeSystemContext tsc,
            TraceRuntimeDescToTypeSystemDesc idParser,
            int clrInstanceID,
            FileInfo preciseDebugInfoFile,
            Logger logger)
        {
            // Capture the addresses of jitted code
            List<MemoryRegionInfo> infos = new List<MemoryRegionInfo>();
            Dictionary<JittedID, MemoryRegionInfo> info = new Dictionary<JittedID, MemoryRegionInfo>();
            foreach (var e in p.EventsInProcess.ByEventType<MethodLoadUnloadTraceData>())
            {
                if (e.ClrInstanceID != clrInstanceID)
                {
                    continue;
                }

                MethodDesc method = null;
                try
                {
                    method = idParser.ResolveMethodID(e.MethodID);
                }
                catch
                {
                }

                if (method != null)
                {
                    JittedID jittedID = new JittedID(e.MethodID, 0);
                    if (!info.ContainsKey(jittedID))
                    {
                        info.Add(jittedID, new MemoryRegionInfo
                        {
                            StartAddress = e.MethodStartAddress,
                            EndAddress = e.MethodStartAddress + checked((uint)e.MethodSize),
                            Method = method,
                        });
                    }
                }
            }

            foreach (var e in p.EventsInProcess.ByEventType<MethodLoadUnloadVerboseTraceData>())
            {
                if (e.ClrInstanceID != clrInstanceID)
                {
                    continue;
                }

                MethodDesc method = null;
                try
                {
                    method = idParser.ResolveMethodID(e.MethodID, throwIfNotFound: false);
                }
                catch
                {
                }

                if (method != null)
                {
                    JittedID jittedID = new JittedID(e.MethodID, e.ReJITID);
                    if (!info.ContainsKey(jittedID))
                    {
                        info.Add(jittedID, new MemoryRegionInfo
                        {
                            StartAddress = e.MethodStartAddress,
                            EndAddress = e.MethodStartAddress + checked((uint)e.MethodSize),
                            Method = method,
                        });
                    }
                }
            }

            var sigProvider = new R2RSignatureTypeProviderForGlobalTables(tsc);
            foreach (var module in p.LoadedModules)
            {
                if (module.FilePath == "")
                    continue;

                if (!File.Exists(module.FilePath))
                    continue;

                try
                {
                    byte[] image = File.ReadAllBytes(module.FilePath);
                    using (FileStream fstream = new FileStream(module.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        var r2rCheckPEReader = new System.Reflection.PortableExecutable.PEReader(fstream, System.Reflection.PortableExecutable.PEStreamOptions.LeaveOpen);

                        if (!ILCompiler.Reflection.ReadyToRun.ReadyToRunReader.IsReadyToRunImage(r2rCheckPEReader))
                            continue;
                    }

                    var reader = new ILCompiler.Reflection.ReadyToRun.ReadyToRunReader(tsc, module.FilePath);
                    foreach (var methodEntry in reader.GetCustomMethodToRuntimeFunctionMapping<TypeDesc, MethodDesc, R2RSigProviderContext>(sigProvider))
                    {
                        foreach (var runtimeFunction in methodEntry.Value.RuntimeFunctions)
                        {
                            infos.Add(new MemoryRegionInfo
                            {
                                StartAddress = module.ImageBase + (ulong)runtimeFunction.StartAddress,
                                EndAddress = module.ImageBase + (ulong)runtimeFunction.StartAddress + (uint)runtimeFunction.Size,
                                Method = methodEntry.Key,
                                NativeToILMap = runtimeFunction.DebugInfo != null ? CreateNativeToILMap(methodEntry.Key, runtimeFunction.DebugInfo.BoundsList) : null,
                            });
                        }
                    }
                }
                catch
                {
                    logger.PrintWarning($"Failed to load method entry points from R2R module {module.FilePath}");
                }
            }

            List<PreciseDebugInfo> preciseInfos = null;
            if (preciseDebugInfoFile != null)
            {
                preciseInfos =
                    File.ReadAllLines(preciseDebugInfoFile.FullName)
                    .Select(l => JsonSerializer.Deserialize<PreciseDebugInfo>(l))
                    .ToList();
            }

            if (preciseInfos != null && preciseInfos.Count > 0)
            {
                foreach (PreciseDebugInfo preciseDebugInf in preciseInfos)
                {
                    if (info.TryGetValue(new JittedID((long)preciseDebugInf.MethodID, 0), out MemoryRegionInfo inf))
                        inf.NativeToILMap = CreateNativeToILMap(idParser, preciseDebugInf);
                }
            }
            else
            {
                // Associate NativeToILMap with MethodLoad event found Memory Regions
                foreach (MethodILToNativeMapTraceData e in p.EventsInProcess.ByEventType<MethodILToNativeMapTraceData>())
                {
                    if (info.TryGetValue(new JittedID(e.MethodID, e.ReJITID), out MemoryRegionInfo inf))
                        inf.NativeToILMap = CreateNativeToILMap(inf.Method, e);
                }
            }

            // Sort the R2R data by StartAddress
            MemoryRegionInfoStartAddressComparer startAddressComparer = new MemoryRegionInfoStartAddressComparer();
            infos.Sort(startAddressComparer);

            // For each method found via MethodLoad events, check to see if it exists in the infos array, and if it does not, build a list to add
            List<MemoryRegionInfo> memoryRegionsToAdd = new List<MemoryRegionInfo>();
            foreach (var methodLoadInfo in info.Values)
            {
                int searchResult = infos.BinarySearch(methodLoadInfo, startAddressComparer);
                if (searchResult < 0)
                {
                    memoryRegionsToAdd.Add(methodLoadInfo);
                }
            }

            // Add the regions from the MethodLoad events, and keep the overall array sorted
            infos.AddRange(memoryRegionsToAdd);
            infos.Sort(startAddressComparer);

            _infos = infos.ToArray();

            _infoKeys = _infos.Select(i => i.StartAddress).ToArray();

#if DEBUG
            for (int i = 0; i < _infos.Length - 1; i++)
            {
                var cur = _infos[i];
                var next = _infos[i + 1];
                if (cur.EndAddress <= next.StartAddress)
                    continue;

                logger.PrintWarning($"Overlap in memory ranges {cur.Method} overlaps with {next.Method}");
            }
#endif
        }

        public MemoryRegionInfo GetInfo(ulong ip)
        {
            int index = Array.BinarySearch(_infoKeys, ip);
            if (index < 0)
                index = ~index - 1;

            if (index < 0)
                return null; // Before first

            var info = _infos[index];
            if (ip < info.StartAddress || ip >= info.EndAddress)
                return null;

            return info;
        }

        public MethodDesc GetMethod(ulong ip) => GetInfo(ip)?.Method;

        private class MemoryRegionInfoStartAddressComparer : IComparer<MemoryRegionInfo>
        {
            int IComparer<MemoryRegionInfo>.Compare(MemoryRegionInfo x, MemoryRegionInfo y) => x.StartAddress.CompareTo(y.StartAddress);
        }

        private static KeyValueMap<uint, IPMapping> CreateNativeToILMap(MethodDesc method, List<DebugInfoBoundsEntry> boundsList)
        {
            List<DebugInfoBoundsEntry> sorted = boundsList.OrderBy(e => e.NativeOffset).ToList();

            return new(sorted.Select(e => e.NativeOffset).ToArray(), sorted.Select(e => new IPMapping((int)e.ILOffset, null, method)).ToArray());
        }

        private static KeyValueMap<uint, IPMapping> CreateNativeToILMap(MethodDesc method, MethodILToNativeMapTraceData ev)
        {
            List<(uint rva, int ilOffset)> pairs = new List<(uint rva, int ilOffset)>(ev.CountOfMapEntries);
            for (int i = 0; i < ev.CountOfMapEntries; i++)
                pairs.Add(((uint)ev.NativeOffset(i), ev.ILOffset(i)));

            pairs.RemoveAll(p => p.ilOffset < 0);
            pairs.Sort((p1, p2) => p1.rva.CompareTo(p2.rva));
            return new(pairs.Select(p => p.rva).ToArray(), pairs.Select(p => new IPMapping(p.ilOffset, null, method)).ToArray());
        }

        private static KeyValueMap<uint, IPMapping> CreateNativeToILMap(TraceRuntimeDescToTypeSystemDesc idParser, PreciseDebugInfo inf)
        {
            Dictionary<uint, (InlineContext ctx, MethodDesc md)> byOrdinal = new();
            AddSubTree(inf.InlineTree);

            void AddSubTree(InlineContext ctx)
            {
                MethodDesc md = idParser.ResolveMethodID((long)ctx.MethodID, false);
                byOrdinal.Add(ctx.Ordinal, (ctx, md));

                foreach (var child in ctx.Inlinees)
                    AddSubTree(child);
            }

            var ordered = inf.Mappings.OrderBy(m => m.NativeOffset).ToList();
            IPMapping CreateMapping(PreciseIPMapping preciseMapping)
            {
                (InlineContext ctx, MethodDesc md) = byOrdinal[preciseMapping.InlineContext];
                return new IPMapping(checked((int)preciseMapping.ILOffset), ctx, md);
            }

            return new(ordered.Select(p => p.NativeOffset).ToArray(), ordered.Select(CreateMapping).ToArray());
        }
    }

    internal class MemoryRegionInfo
    {
        public ulong StartAddress { get; set; }
        public ulong EndAddress { get; set; }
        public MethodDesc Method { get; set; }
        public KeyValueMap<uint, IPMapping> NativeToILMap { get; set; }
    }

    internal record struct IPMapping(
        int ILOffset,
        InlineContext InlineContext,
        MethodDesc InlineeMethod);
}
