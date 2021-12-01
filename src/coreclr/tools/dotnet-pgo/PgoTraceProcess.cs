// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    struct LoadedModule
    {
        public LoadedModule(int clrInstanceID, TraceManagedModule managedModule)
        {
            ClrInstanceID = clrInstanceID;
            ManagedModule = managedModule;
        }

        public readonly int ClrInstanceID;
        public readonly TraceManagedModule ManagedModule;
    }

    class PgoTraceProcess
    {
        public PgoTraceProcess(TraceProcess traceProcess)
        {
            TraceProcess = traceProcess;
            foreach (var assemblyLoadTrace in traceProcess.EventsInProcess.ByEventType<AssemblyLoadUnloadTraceData>())
            {
                _assemblyToCLRInstanceIDMap[assemblyLoadTrace.AssemblyID] = assemblyLoadTrace.ClrInstanceID;
            }
        }

        private Dictionary<long, int> _assemblyToCLRInstanceIDMap = new Dictionary<long, int>();

        public readonly TraceProcess TraceProcess;

        public IEnumerable<LoadedModule> EnumerateLoadedManagedModules()
        {
            foreach (var moduleFile in TraceProcess.LoadedModules)
            {
                if (moduleFile is TraceManagedModule)
                {
                    var managedModule = moduleFile as TraceManagedModule;

                    int clrInstanceIDModule;
                    if (!_assemblyToCLRInstanceIDMap.TryGetValue(managedModule.AssemblyID, out clrInstanceIDModule))
                        continue;

                    yield return new LoadedModule(clrInstanceIDModule, managedModule);
                }
            }
        }

        public bool ClrInstanceIsCoreCLRInstance(int clrInstanceId)
        {
            foreach (var module in EnumerateLoadedManagedModules())
            {
                if (module.ClrInstanceID != clrInstanceId)
                    continue;
                if (CompareModuleAgainstSimpleName("System.Private.CoreLib", module.ManagedModule))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CompareModuleAgainstSimpleName(string simpleName, TraceManagedModule managedModule)
        {
            if (managedModule.ModuleFile != null)
            {
                if ((String.Compare(managedModule.ModuleFile.Name, simpleName, StringComparison.OrdinalIgnoreCase) == 0) || (String.Compare(managedModule.ModuleFile.Name, (simpleName + ".il"), StringComparison.OrdinalIgnoreCase) == 0))
                {
                    return true;
                }
            }
            return false;
        }

        public static string ComputeFilePathOnDiskForModule(TraceManagedModule managedModule)
        {
            string filePath = "";
            if (managedModule.ModuleFile != null)
            {
                filePath = managedModule.ModuleFile.FilePath;
                string ildllstr = ".il.dll";
                string ilexestr = ".il.exe";
                if (!File.Exists(filePath) && filePath.EndsWith(ildllstr))
                {
                    filePath = filePath.Substring(0, filePath.Length - ildllstr.Length) + ".dll";
                }
                else if (!File.Exists(filePath) && filePath.EndsWith(ilexestr))
                {
                    filePath = filePath.Substring(0, filePath.Length - ilexestr.Length) + ".exe";
                }
            }

            return filePath;
        }
    }
}
