// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.Tracing.Etlx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection;
using System.IO;
using System.Text;
using System.Net;

namespace Microsoft.Diagnostics.Tools.Pgo
{
    class TraceRuntimeDescToTypeSystemDesc
    {
        TraceProcess _traceProcess;
        TypeSystemContext _context;
        int _clrInstanceID;

        struct TraceMethodData
        {
            public TraceMethodData(long loaderModuleID, long typeID, int methodToken, long[] typeParameters)
            {
                LoaderModuleID = loaderModuleID;
                MethodToken = methodToken;
                TypeParameters = typeParameters;
                TypeID = typeID;
            }


            public readonly long LoaderModuleID;
            public readonly int MethodToken;
            public readonly long[] TypeParameters;
            public readonly long TypeID;
        }

        class MethodDescInfo
        {
            public MethodDescInfo(long id, TraceMethodData methodIDDetailsTraceData)
            {
                ID = id;
                MethodDetailsTraceData = methodIDDetailsTraceData;
            }

            public readonly long ID;
            public MethodDesc Method;
            public readonly TraceMethodData MethodDetailsTraceData;
        }

        struct TraceTypeData
        {
            public TraceTypeData(long moduleID, int typeNameID, Microsoft.Diagnostics.Tracing.Parsers.Clr.TypeFlags flags, byte corElementType, long[] typeParameters, string name)
            {
                ModuleID = moduleID;
                TypeNameID = typeNameID;
                Flags = flags;
                CorElementType = corElementType;
                TypeParameters = typeParameters;
                Name = name;
            }

            public readonly long ModuleID;
            public readonly int TypeNameID;
            public readonly Microsoft.Diagnostics.Tracing.Parsers.Clr.TypeFlags Flags;
            public readonly byte CorElementType;
            public readonly long[] TypeParameters;
            public readonly string Name;
        }

        class TypeHandleInfo
        {
            public TypeHandleInfo(long id, TraceTypeData traceData)
            {
                ID = id;
                TypeValue = traceData;
            }

            public readonly long ID;
            public TypeDesc Type;
            public readonly TraceTypeData TypeValue;

            public override string ToString()
            {
                return Type != null ? Type.ToString() : "NULL";
            }
        }

        class ModuleDescInfo
        {
            public ModuleDescInfo(long id, string assemblyName)
            {
                ID = id;
                AssemblyName = assemblyName;
            }

            public readonly long ID;
            public ModuleDesc Module;
            public readonly string AssemblyName;
        }

        private readonly Dictionary<long, MethodDescInfo> _methods = new Dictionary<long, MethodDescInfo>();
        private readonly Dictionary<long, TypeHandleInfo> _types = new Dictionary<long, TypeHandleInfo>();
        private readonly Dictionary<long, ModuleDescInfo> _modules = new Dictionary<long, ModuleDescInfo>();
        private readonly object _lock = new object();
        private readonly int s_bulkTypeEvents = 0;
        private readonly int s_bulkTypeTypes = 0;

        public TraceRuntimeDescToTypeSystemDesc(TraceProcess traceProcess, TypeSystemContext context, int clrInstanceID)
        {
            _traceProcess = traceProcess;
            _context = context;
            _clrInstanceID = clrInstanceID;

            foreach (var methodIDDetailsData in traceProcess.EventsInProcess.ByEventType<MethodDetailsTraceData>())
            {

                MethodDescInfo currentInfo;
                if (_methods.TryGetValue(methodIDDetailsData.MethodID, out currentInfo))
                {
                    if (currentInfo.MethodDetailsTraceData.LoaderModuleID != methodIDDetailsData.LoaderModuleID)
                        throw new Exception("Re-use of MethodID with different data. Unload scenario?)");
                    if (currentInfo.MethodDetailsTraceData.MethodToken != methodIDDetailsData.MethodToken)
                        throw new Exception("Re-use of MethodID with different data. Unload scenario?)");
                    if (currentInfo.MethodDetailsTraceData.TypeID != methodIDDetailsData.TypeID)
                        throw new Exception("Re-use of MethodID with different data. Unload scenario?)");
                    if (currentInfo.MethodDetailsTraceData.TypeParameters.Length != methodIDDetailsData.TypeParameterCount)
                        throw new Exception("Re-use of MethodID with different data. Unload scenario?)");
                    for (int ix = 0; ix < methodIDDetailsData.TypeParameterCount; ix++)
                    {
                        if (currentInfo.MethodDetailsTraceData.TypeParameters[ix] != (long)methodIDDetailsData.TypeParameters(ix))
                            throw new Exception("Re-use of MethodID with different data. Unload scenario?)");
                    }
                    continue;
                }

                long[] typeParameters = Array.Empty<long>();
                if (methodIDDetailsData.TypeParameterCount != 0)
                {
                    typeParameters = new long[methodIDDetailsData.TypeParameterCount];
                    for (int ix = 0; ix < typeParameters.Length; ix++)
                    {
                        typeParameters[ix] = (long)methodIDDetailsData.TypeParameters(ix);
                    }
                }
                else
                {
                    typeParameters = Array.Empty<long>();
                }

                TraceMethodData traceMethodData = new TraceMethodData(typeID: (long)methodIDDetailsData.TypeID,
                    loaderModuleID: methodIDDetailsData.LoaderModuleID,
                    methodToken: methodIDDetailsData.MethodToken,
                    typeParameters: typeParameters);

                currentInfo = new MethodDescInfo(methodIDDetailsData.MethodID, traceMethodData);
                _methods.Add(methodIDDetailsData.MethodID, currentInfo);
            }

            foreach (var bulkTypeTrace in traceProcess.EventsInProcess.ByEventType<GCBulkTypeTraceData>())
            {
                s_bulkTypeEvents++;

                if (bulkTypeTrace.ClrInstanceID != _clrInstanceID)
                    continue;

                for (int i = 0; i < bulkTypeTrace.Count; i++)
                {
                    TypeHandleInfo currentInfo;
                    var typeTrace = bulkTypeTrace.Values(i);
                    s_bulkTypeTypes++;

                    if (_types.TryGetValue((long)typeTrace.TypeID, out currentInfo))
                    {
                        if (currentInfo.TypeValue.ModuleID != (long)typeTrace.ModuleID)
                            throw new Exception("Re-use of TypeID with different data. Unload scenario?)");
                        if (currentInfo.TypeValue.TypeNameID != typeTrace.TypeNameID)
                            throw new Exception("Re-use of TypeID with different data. Unload scenario?)");
                        if (currentInfo.TypeValue.Flags != typeTrace.Flags)
                            throw new Exception("Re-use of TypeID with different data. Unload scenario?)");
                        if (currentInfo.TypeValue.CorElementType != typeTrace.CorElementType)
                            throw new Exception("Re-use of TypeID with different data. Unload scenario?)");
                        if (currentInfo.TypeValue.TypeParameters.Length != typeTrace.TypeParameterCount)
                            throw new Exception("Re-use of TypeID with different data. Unload scenario?)");

                        for (int ix = 0; ix < typeTrace.TypeParameterCount; ix++)
                        {
                            if (currentInfo.TypeValue.TypeParameters[ix] != (long)typeTrace.TypeParameterID(ix))
                                throw new Exception("Re-use of TypeID with different data. Unload scenario?)");
                        }
                        continue;
                    }

                    long[] typeParameters = Array.Empty<long>();
                    if (typeTrace.TypeParameterCount != 0)
                    {
                        typeParameters = new long[typeTrace.TypeParameterCount];
                        for (int ix = 0; ix < typeParameters.Length; ix++)
                        {
                            typeParameters[ix] = (long)typeTrace.TypeParameterID(ix);
                        }
                    }
                    else
                    {
                        typeParameters = Array.Empty<long>();
                    }
                    TraceTypeData traceTypeData = new TraceTypeData(moduleID: (long)typeTrace.ModuleID,
                        typeNameID: typeTrace.TypeNameID,
                        flags: typeTrace.Flags,
                        corElementType: typeTrace.CorElementType,
                        typeParameters: typeParameters,
                        name: typeTrace.TypeName);

                    currentInfo = new TypeHandleInfo((long)typeTrace.TypeID, traceTypeData);
                    _types.Add((long)typeTrace.TypeID, currentInfo);
                }
            }

            Dictionary<long, int> assemblyToCLRInstanceIDMap = new Dictionary<long, int>();
            Dictionary<long, string> assemblyToFullyQualifiedAssemblyName = new Dictionary<long, string>();
            foreach (var assemblyLoadTrace in _traceProcess.EventsInProcess.ByEventType<AssemblyLoadUnloadTraceData>())
            {
                assemblyToCLRInstanceIDMap[assemblyLoadTrace.AssemblyID] = assemblyLoadTrace.ClrInstanceID;
                assemblyToFullyQualifiedAssemblyName[assemblyLoadTrace.AssemblyID] = assemblyLoadTrace.FullyQualifiedAssemblyName;
            }

            foreach (var moduleFile in _traceProcess.LoadedModules)
            {
                if (moduleFile is TraceManagedModule)
                {
                    var managedModule = moduleFile as TraceManagedModule;

                    int clrInstanceIDModule;
                    if (!assemblyToCLRInstanceIDMap.TryGetValue(managedModule.AssemblyID, out clrInstanceIDModule))
                        continue;

                    if (clrInstanceIDModule != _clrInstanceID)
                        continue;

                    var currentInfo = new ModuleDescInfo(managedModule.ModuleID, assemblyToFullyQualifiedAssemblyName[managedModule.AssemblyID]);
                    if (!_modules.ContainsKey(managedModule.ModuleID))
                        _modules.Add(managedModule.ModuleID, currentInfo);
                }
            }
        }

        // Call before any api other than ResolveModuleID will work
        public void Init()
        {
            // Fill in all the types
            foreach (var entry in _types)
            {
                ResolveTypeHandle(entry.Key, false);
            }
        }

        public void RemoveModuleIDFromLoader(long handle)
        {
            lock (_lock)
            {
                var moduleDesc = _modules[handle];
                _modules.Remove(handle);
            }
        }

        public ModuleDesc ResolveModuleID(long handle, bool throwIfNotFound = true)
        {
            lock (_lock)
            {
                ModuleDescInfo minfo;
                if (_modules.TryGetValue(handle, out minfo))
                {
                    if (minfo.Module != null)
                        return minfo.Module;

                    minfo.Module = _context.ResolveAssembly(new AssemblyName(minfo.AssemblyName), throwIfNotFound);
                    return minfo.Module;
                }
                else
                {
                    if (throwIfNotFound)
                        throw new Exception("Unknown ModuleID value");
                    return null;
                }
            }
        }

        public TypeDesc ResolveTypeHandle(long handle, bool throwIfNotFound = true)
        {
            lock(_lock)
            {
                TypeHandleInfo tinfo;
                if (_types.TryGetValue(handle, out tinfo))
                {
                    if (tinfo.Type != null)
                        return tinfo.Type;

                    if ((tinfo.TypeValue.Flags & Microsoft.Diagnostics.Tracing.Parsers.Clr.TypeFlags.Array) != 0)
                    {
                        if (tinfo.TypeValue.TypeParameters.Length != 1)
                        {
                            throw new Exception("Bad format for BulkType");
                        }

                        TypeDesc elementType = ResolveTypeHandle((long)tinfo.TypeValue.TypeParameters[0], throwIfNotFound);
                        if (elementType == null)
                            return null;

                        if (tinfo.TypeValue.CorElementType == (byte)SignatureTypeCode.SZArray)
                        {
                            tinfo.Type = elementType.MakeArrayType();
                        }
                        else
                        {
                            int rank = tinfo.TypeValue.Flags.GetArrayRank();
                            tinfo.Type = elementType.MakeArrayType(rank);
                        }
                    }
                    else if (tinfo.TypeValue.CorElementType == (byte)SignatureTypeCode.ByReference)
                    {
                        if (tinfo.TypeValue.TypeParameters.Length != 1)
                        {
                            throw new Exception("Bad format for BulkType");
                        }

                        TypeDesc elementType = ResolveTypeHandle((long)tinfo.TypeValue.TypeParameters[0], throwIfNotFound);
                        if (elementType == null)
                            return null;

                        tinfo.Type = elementType.MakeByRefType();
                    }
                    else if (tinfo.TypeValue.CorElementType == (byte)SignatureTypeCode.Pointer)
                    {
                        if (tinfo.TypeValue.TypeParameters.Length != 1)
                        {
                            throw new Exception("Bad format for BulkType");
                        }

                        TypeDesc elementType = ResolveTypeHandle((long)tinfo.TypeValue.TypeParameters[0], throwIfNotFound);
                        if (elementType == null)
                            return null;

                        tinfo.Type = elementType.MakePointerType();
                    }
                    else if (tinfo.TypeValue.CorElementType == (byte)SignatureTypeCode.FunctionPointer)
                    {
                        tinfo.Type = null;
                    }
                    else
                    {
                        // Must be class type or instantiated type.
                        ModuleDesc module = ResolveModuleID((long)tinfo.TypeValue.ModuleID, throwIfNotFound);
                        if (module == null)
                            return null;

                        EcmaModule ecmaModule = module as EcmaModule;
                        if (ecmaModule == null)
                        {
                            if (throwIfNotFound)
                                throw new Exception($"Unable to resolve module for {handle:8x}");
                            return null;
                        }

                        if ((tinfo.TypeValue.TypeNameID & 0xFF000000) != 0x02000000)
                        {
                            throw new Exception($"Invalid typedef {tinfo.TypeValue.TypeNameID:4x}");
                        }

                        TypeDefinitionHandle typedef = MetadataTokens.TypeDefinitionHandle(tinfo.TypeValue.TypeNameID & 0xFFFFFF);
                        MetadataType uninstantiatedType = (MetadataType)ecmaModule.GetType(typedef);
                        // Instantiate the type if requested
                        if ((tinfo.TypeValue.TypeParameters.Length != 0) && uninstantiatedType != null)
                        {
                            if (uninstantiatedType.Instantiation.Length != tinfo.TypeValue.TypeParameters.Length)
                            {
                                throw new Exception($"Invalid TypeParameterCount {tinfo.TypeValue.TypeParameters.Length} expected {uninstantiatedType.Instantiation.Length} as needed by '{uninstantiatedType}'");
                            }

                            TypeDesc[] instantiation = new TypeDesc[tinfo.TypeValue.TypeParameters.Length];
                            for (int i = 0; i < instantiation.Length; i++)
                            {
                                instantiation[i] = ResolveTypeHandle((long)tinfo.TypeValue.TypeParameters[i], throwIfNotFound);
                                if (instantiation[i] == null)
                                    return null;
                            }
                            tinfo.Type = uninstantiatedType.Context.GetInstantiatedType(uninstantiatedType, new Instantiation(instantiation));
                        }
                        else
                        {
                            if ((uninstantiatedType.Name == "__Canon") && uninstantiatedType.Namespace == "System" && (uninstantiatedType.Module == uninstantiatedType.Context.SystemModule))
                            {
                                tinfo.Type = uninstantiatedType.Context.CanonType;
                            }
                            else
                            {
                                tinfo.Type = uninstantiatedType;
                            }
                        }
                    }
                    if (tinfo.Type == null)
                    {
                        if (throwIfNotFound)
                            throw new Exception("Unknown typeHandle value");
                        return null;
                    }
                    return tinfo.Type;
                }
                else
                {
                    if (throwIfNotFound)
                        throw new Exception("Unknown typeHandle value");
                    return null;
                }
            }
        }

        public MethodDesc ResolveMethodID(long handle, bool throwIfNotFound = true)
        {
            lock (_lock)
            {
                MethodDescInfo minfo;
                if (_methods.TryGetValue(handle, out minfo))
                {
                    if (minfo.Method != null)
                        return minfo.Method;

                    TypeDesc owningType = ResolveTypeHandle(minfo.MethodDetailsTraceData.TypeID, throwIfNotFound);
                    if (owningType == null)
                        return null;

                    MetadataType owningMDType = owningType as MetadataType;
                    if (owningMDType == null)
                        throw new Exception("Method not parented by MetadataType");

                    if ((minfo.MethodDetailsTraceData.MethodToken & 0xFF000000) != 0x06000000)
                    {
                        throw new Exception($"Invalid methoddef {minfo.MethodDetailsTraceData.MethodToken:4x}");
                    }

                    MethodDefinitionHandle methoddef = MetadataTokens.MethodDefinitionHandle(minfo.MethodDetailsTraceData.MethodToken & 0xFFFFFF);

                    MethodDesc uninstantiatedMethod = null;
                    foreach (MethodDesc m in owningMDType.GetMethods())
                    {
                        EcmaMethod ecmaMeth = m.GetTypicalMethodDefinition() as EcmaMethod;
                        if (ecmaMeth == null)
                        {
                            continue;
                        }

                        if (ecmaMeth.Handle == methoddef)
                        {
                            uninstantiatedMethod = m;
                            break;
                        }
                    }

                    if (uninstantiatedMethod == null)
                    {
                        if (throwIfNotFound)
                        {
                            EcmaType ecmaType = owningMDType.GetTypeDefinition() as EcmaType;
                            
                            throw new Exception($"Unknown MethodID value finding MethodDef {minfo.MethodDetailsTraceData.MethodToken:x} on type {owningMDType} from module {ecmaType.Module.Assembly.GetName().Name}");
                        }
                        return null;
                    }

                    // Instantiate the type if requested
                    if (minfo.MethodDetailsTraceData.TypeParameters.Length != 0)
                    {
                        if (uninstantiatedMethod.Instantiation.Length != minfo.MethodDetailsTraceData.TypeParameters.Length)
                        {
                            throw new Exception($"Invalid TypeParameterCount {minfo.MethodDetailsTraceData.TypeParameters.Length} expected {uninstantiatedMethod.Instantiation.Length} as needed by '{uninstantiatedMethod}'");
                        }

                        TypeDesc[] instantiation = new TypeDesc[minfo.MethodDetailsTraceData.TypeParameters.Length];
                        for (int i = 0; i < instantiation.Length; i++)
                        {
                            instantiation[i] = ResolveTypeHandle((long)minfo.MethodDetailsTraceData.TypeParameters[i], throwIfNotFound);
                            if (instantiation[i] == null)
                                return null;
                        }

                        minfo.Method = _context.GetInstantiatedMethod(uninstantiatedMethod, new Instantiation(instantiation));

                        if (minfo.Method == null)
                        {
                            if (throwIfNotFound)
                            {
                                StringBuilder s = new StringBuilder();
                                foreach (TypeDesc type in instantiation)
                                {
                                    if (s.Length != 0)
                                        s.Append(',');
                                    s.Append(type);
                                }
                                throw new Exception("Unable to instantiate {uninstantiatedMethod} over <{s}>");
                            }
                            return null;
                        }
                    }
                    else
                    {
                        minfo.Method = uninstantiatedMethod;
                    }

                    if (minfo.Method == null)
                    {
                        if (throwIfNotFound)
                            throw new Exception("Unknown MethodID value");
                        return null;
                    }

                    return minfo.Method;
                }
                else
                {
                    if (throwIfNotFound)
                        throw new Exception("Unknown MethodID value");
                    return null;
                }
            }
        }
    }
}
