// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public MethodMemoryMap(
            TraceProcess p,
            TraceTypeSystemContext tsc,
            TraceRuntimeDescToTypeSystemDesc idParser,
            int clrInstanceID)
        {
            // Capture the addresses of jitted code
            List<MemoryRegionInfo> infos = new List<MemoryRegionInfo>();
            Dictionary<long, MemoryRegionInfo> info = new Dictionary<long, MemoryRegionInfo>();
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
                    infos.Add(new MemoryRegionInfo
                    {
                        StartAddress = e.MethodStartAddress,
                        EndAddress = e.MethodStartAddress + checked((uint)e.MethodSize),
                        MethodID = e.MethodID,
                        Method = method,
                    });
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
                    method = idParser.ResolveMethodID(e.MethodID);
                }
                catch
                {
                }

                if (method != null)
                {
                    infos.Add(new MemoryRegionInfo
                    {
                        StartAddress = e.MethodStartAddress,
                        EndAddress = e.MethodStartAddress + checked((uint)e.MethodSize),
                        MethodID = e.MethodID,
                        Method = method,
                    });
                }
            }

            var sigProvider = new R2RSignatureTypeProvider(tsc);
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
                                NativeToILMap = runtimeFunction.DebugInfo != null ? NativeToILMap.FromR2RBounds(runtimeFunction.DebugInfo.BoundsList) : null,
                            });
                        }
                    }
                }
                catch { }
            }

            // Can have duplicate events, so pick first for each
            var byMethodID = infos.GroupBy(i => i.MethodID).ToDictionary(g => g.Key, g => g.First());
            foreach (MethodILToNativeMapTraceData e in p.EventsInProcess.ByEventType<MethodILToNativeMapTraceData>())
            {
                if (byMethodID.TryGetValue(e.MethodID, out MemoryRegionInfo inf))
                    inf.NativeToILMap = NativeToILMap.FromEvent(e);
            }

            _infos = byMethodID.Values.OrderBy(i => i.StartAddress).ToArray();
            _infoKeys = _infos.Select(i => i.StartAddress).ToArray();

#if DEBUG
            for (int i = 0; i < _infos.Length - 1; i++)
            {
                var cur = _infos[i];
                var next = _infos[i + 1];
                if (cur.EndAddress <= next.StartAddress)
                    continue;

                Debug.Fail("Overlap in memory ranges");
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
    }

    public class MemoryRegionInfo
    {
        public ulong StartAddress { get; set; }
        public ulong EndAddress { get; set; }
        public long MethodID { get; set; }
        public MethodDesc Method { get; set; }
        public NativeToILMap NativeToILMap { get; set; }
    }
}
