// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;
using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Implementation of ISOSDacInterface* interfaces intended to be passed out to consumers
/// interacting with the DAC via those COM interfaces.
/// </summary>
/// <remarks>
/// Functions on <see cref="ISOSDacInterface"/> are defined with PreserveSig. Target and Contracts
/// throw on errors. Implementations in this class should wrap logic in a try-catch and return the
/// corresponding error code.
/// </remarks>
[GeneratedComClass]
internal sealed partial class SOSDacImpl : ISOSDacInterface, ISOSDacInterface2, ISOSDacInterface9
{
    private readonly Target _target;
    private readonly TargetPointer _stringMethodTable;
    private readonly TargetPointer _objectMethodTable;

    public SOSDacImpl(Target target)
    {
        _target = target;
        _stringMethodTable = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.StringMethodTable));
        _objectMethodTable = _target.ReadPointer(_target.ReadGlobalPointer(Constants.Globals.ObjectMethodTable));
    }

    public unsafe int GetAppDomainConfigFile(ulong appDomain, int count, char* configFile, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAppDomainData(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetAppDomainList(uint count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] values, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAppDomainName(ulong addr, uint count, char* name, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAppDomainStoreData(void* data) => HResults.E_NOTIMPL;
    public unsafe int GetApplicationBase(ulong appDomain, int count, char* appBase, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAssemblyData(ulong baseDomainPtr, ulong assembly, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetAssemblyList(ulong appDomain, int count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] values, int* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAssemblyLocation(ulong assembly, int count, char* location, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAssemblyModuleList(ulong assembly, uint count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] modules, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetAssemblyName(ulong assembly, uint count, char* name, uint* pNeeded) => HResults.E_NOTIMPL;

    public int GetBreakingChangeVersion()
    {
        return _target.ReadGlobal<byte>(Constants.Globals.SOSBreakingChangeVersion);
    }

    public unsafe int GetCCWData(ulong ccw, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetCCWInterfaces(ulong ccw, uint count, void* interfaces, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetClrWatsonBuckets(ulong thread, void* pGenericModeBlock) => HResults.E_NOTIMPL;
    public unsafe int GetCodeHeaderData(ulong ip, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetCodeHeapList(ulong jitManager, uint count, void* codeHeaps, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetDacModuleHandle(void* phModule) => HResults.E_NOTIMPL;
    public unsafe int GetDomainFromContext(ulong context, ulong* domain) => HResults.E_NOTIMPL;
    public unsafe int GetDomainLocalModuleData(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetDomainLocalModuleDataFromAppDomain(ulong appDomainAddr, int moduleID, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetDomainLocalModuleDataFromModule(ulong moduleAddr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetFailedAssemblyData(ulong assembly, uint* pContext, int* pResult) => HResults.E_NOTIMPL;
    public unsafe int GetFailedAssemblyDisplayName(ulong assembly, uint count, char* name, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetFailedAssemblyList(ulong appDomain, int count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] values, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetFailedAssemblyLocation(ulong assesmbly, uint count, char* location, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetFieldDescData(ulong fieldDesc, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetFrameName(ulong vtable, uint count, char* frameName, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetGCHeapData(void* data) => HResults.E_NOTIMPL;
    public unsafe int GetGCHeapDetails(ulong heap, void* details) => HResults.E_NOTIMPL;
    public unsafe int GetGCHeapList(uint count, [In, MarshalUsing(CountElementName = "count"), Out] ulong[] heaps, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetGCHeapStaticData(void* data) => HResults.E_NOTIMPL;
    public unsafe int GetHandleEnum(void** ppHandleEnum) => HResults.E_NOTIMPL;
    public unsafe int GetHandleEnumForGC(uint gen, void** ppHandleEnum) => HResults.E_NOTIMPL;
    public unsafe int GetHandleEnumForTypes([In, MarshalUsing(CountElementName = "count")] uint[] types, uint count, void** ppHandleEnum) => HResults.E_NOTIMPL;
    public unsafe int GetHeapAllocData(uint count, void* data, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetHeapAnalyzeData(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetHeapAnalyzeStaticData(void* data) => HResults.E_NOTIMPL;
    public unsafe int GetHeapSegmentData(ulong seg, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetHillClimbingLogEntry(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetILForModule(ulong moduleAddr, int rva, ulong* il) => HResults.E_NOTIMPL;
    public unsafe int GetJitHelperFunctionName(ulong ip, uint count, byte* name, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetJitManagerList(uint count, void* managers, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetJumpThunkTarget(void* ctx, ulong* targetIP, ulong* targetMD) => HResults.E_NOTIMPL;
    public unsafe int GetMethodDescData(ulong methodDesc, ulong ip, DacpMethodDescData* data, uint cRevertedRejitVersions, DacpReJitData* rgRevertedRejitData, uint* pcNeededRevertedRejitData)
    {
        if (methodDesc == 0)
        {
            return HResults.E_INVALIDARG;
        }
        if (cRevertedRejitVersions != 0 && rgRevertedRejitData == null)
        {
            return HResults.E_INVALIDARG;
        }
        if (rgRevertedRejitData != null && pcNeededRevertedRejitData == null)
        {
            // If you're asking for reverted rejit data, you'd better ask for the number of
            // elements we return
            return HResults.E_INVALIDARG;
        }
        try
        {
            Contracts.IRuntimeTypeSystem rtsContract = _target.Contracts.RuntimeTypeSystem;
            Contracts.MethodDescHandle methodDescHandle = rtsContract.GetMethodDescHandle(methodDesc);

            data->MethodTablePtr = rtsContract.GetMethodTable(methodDescHandle);

            return HResults.E_NOTIMPL;
        }
        catch (global::System.Exception ex)
        {
            return ex.HResult;
        }
    }

    public unsafe int GetMethodDescFromToken(ulong moduleAddr, uint token, ulong* methodDesc) => HResults.E_NOTIMPL;
    public unsafe int GetMethodDescName(ulong methodDesc, uint count, char* name, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetMethodDescPtrFromFrame(ulong frameAddr, ulong* ppMD) => HResults.E_NOTIMPL;
    public unsafe int GetMethodDescPtrFromIP(ulong ip, ulong* ppMD) => HResults.E_NOTIMPL;
    public unsafe int GetMethodDescTransparencyData(ulong methodDesc, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetMethodTableData(ulong mt, DacpMethodTableData* data)
    {
        if (mt == 0 || data == null)
            return HResults.E_INVALIDARG;

        try
        {
            Contracts.IRuntimeTypeSystem contract = _target.Contracts.RuntimeTypeSystem;
            Contracts.TypeHandle methodTable = contract.GetTypeHandle(mt);

            DacpMethodTableData result = default;
            result.baseSize = contract.GetBaseSize(methodTable);
            // [compat] SOS DAC APIs added this base size adjustment for strings
            // due to: "2008/09/25 Title: New implementation of StringBuilder and improvements in String class"
            // which changed StringBuilder not to use a String as an internal buffer and in the process
            // changed the String internals so that StringObject::GetBaseSize() now includes the nul terminator character,
            // which is apparently not expected by SOS.
            if (contract.IsString(methodTable))
                result.baseSize -= sizeof(char);

            result.componentSize = contract.GetComponentSize(methodTable);
            bool isFreeObjectMT = contract.IsFreeObjectMethodTable(methodTable);
            result.bIsFree = isFreeObjectMT ? 1 : 0;
            if (!isFreeObjectMT)
            {
                result.module = contract.GetModule(methodTable);
                // Note: really the canonical method table, not the EEClass, which we don't expose
                result.klass = contract.GetCanonicalMethodTable(methodTable);
                result.parentMethodTable = contract.GetParentMethodTable(methodTable);
                result.wNumInterfaces = contract.GetNumInterfaces(methodTable);
                result.wNumMethods = contract.GetNumMethods(methodTable);
                result.wNumVtableSlots = 0; // always return 0 since .NET 9
                result.wNumVirtuals = 0; // always return 0 since .NET 9
                result.cl = contract.GetTypeDefToken(methodTable);
                result.dwAttrClass = contract.GetTypeDefTypeAttributes(methodTable);
                result.bContainsGCPointers = contract.ContainsGCPointers(methodTable) ? 1 : 0;
                result.bIsShared = 0;
                result.bIsDynamic = contract.IsDynamicStatics(methodTable) ? 1 : 0;
            }
            *data = result;
            return HResults.S_OK;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }
    }
    public unsafe int GetMethodTableFieldData(ulong mt, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetMethodTableForEEClass(ulong eeClassReallyCanonMT, ulong* value)
    {
        if (eeClassReallyCanonMT == 0 || value == null)
            return HResults.E_INVALIDARG;

        try
        {
            Contracts.IRuntimeTypeSystem contract = _target.Contracts.RuntimeTypeSystem;
            Contracts.TypeHandle methodTableHandle = contract.GetTypeHandle(eeClassReallyCanonMT);
            *value = methodTableHandle.Address;
            return HResults.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return ex.HResult;
        }
    }

    private unsafe void CopyStringToTargetBuffer(char* stringBuf, uint bufferSize, uint* neededBufferSize, string str)
    {
        ReadOnlySpan<char> strSpan = str.AsSpan();
        if (neededBufferSize != null)
            *neededBufferSize = checked((uint)(strSpan.Length + 1));

        if (stringBuf != null && bufferSize > 0)
        {
            Span<char> target = new Span<char>(stringBuf, checked((int)bufferSize));
            int nullTerminatorLocation = strSpan.Length > bufferSize - 1 ? checked((int)(bufferSize - 1)) : strSpan.Length;
            strSpan = strSpan.Slice(0, nullTerminatorLocation);
            strSpan.CopyTo(target);
            target[nullTerminatorLocation] = '\0';
        }
    }

    public unsafe int GetMethodTableName(ulong mt, uint count, char* mtName, uint* pNeeded)
    {
        if (mt == 0)
            return HResults.E_INVALIDARG;

        try
        {
            Contracts.IRuntimeTypeSystem typeSystemContract = _target.Contracts.RuntimeTypeSystem;
            Contracts.TypeHandle methodTableHandle = typeSystemContract.GetTypeHandle(mt);
            if (typeSystemContract.IsFreeObjectMethodTable(methodTableHandle))
            {
                CopyStringToTargetBuffer(mtName, count, pNeeded, "Free");
                return HResults.S_OK;
            }

            // TODO(cdac) - The original code handles the case of the module being in the process of being unloaded. This is not yet handled

            System.Text.StringBuilder methodTableName = new();
            try
            {
                TargetPointer modulePointer = typeSystemContract.GetModule(methodTableHandle);
                TypeNameBuilder.AppendType(_target, methodTableName, methodTableHandle, TypeNameFormat.FormatNamespace | TypeNameFormat.FormatFullInst);
            }
            catch
            {
                try
                {
                    string? fallbackName = _target.Contracts.DacStreams.StringFromEEAddress(mt);
                    if (fallbackName != null)
                    {
                        methodTableName.Clear();
                        methodTableName.Append(fallbackName);
                    }
                }
                catch
                { }
            }
            CopyStringToTargetBuffer(mtName, count, pNeeded, methodTableName.ToString());
            return HResults.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return ex.HResult;
        }
    }

    public unsafe int GetMethodTableSlot(ulong mt, uint slot, ulong* value) => HResults.E_NOTIMPL;
    public unsafe int GetMethodTableTransparencyData(ulong mt, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetModule(ulong addr, void** mod) => HResults.E_NOTIMPL;

    public unsafe int GetModuleData(ulong moduleAddr, DacpModuleData* data)
    {
        if (moduleAddr == 0 || data == null)
            return HResults.E_INVALIDARG;

        try
        {
            Contracts.ILoader contract = _target.Contracts.Loader;
            Contracts.ModuleHandle handle = contract.GetModuleHandle(moduleAddr);

            data->Address = moduleAddr;
            data->PEAssembly = moduleAddr; // Module address in .NET 9+ - correspondingly, SOS-DAC APIs for PE assemblies expect a module address
            data->Assembly = contract.GetAssembly(handle);

            Contracts.ModuleFlags flags = contract.GetFlags(handle);
            bool isReflectionEmit = flags.HasFlag(Contracts.ModuleFlags.ReflectionEmit);
            data->isReflection = (uint)(isReflectionEmit ? 1 : 0);
            data->isPEFile = (uint)(isReflectionEmit ? 0 : 1);      // ReflectionEmit module means it is not a PE file
            data->dwTransientFlags = (uint)flags;

            data->ilBase = contract.GetILBase(handle);
            data->metadataStart = contract.GetMetadataAddress(handle, out ulong metadataSize);
            data->metadataSize = metadataSize;

            data->LoaderAllocator = contract.GetLoaderAllocator(handle);
            data->ThunkHeap = contract.GetThunkHeap(handle);

            Contracts.ModuleLookupTables tables = contract.GetLookupTables(handle);
            data->FieldDefToDescMap = tables.FieldDefToDesc;
            data->ManifestModuleReferencesMap = tables.ManifestModuleReferences;
            data->MemberRefToDescMap = tables.MemberRefToDesc;
            data->MethodDefToDescMap = tables.MethodDefToDesc;
            data->TypeDefToMethodTableMap = tables.TypeDefToMethodTable;
            data->TypeRefToMethodTableMap = tables.TypeRefToMethodTable;

            // Always 0 - .NET no longer has these concepts
            data->dwModuleID = 0;
            data->dwBaseClassIndex = 0;
            data->dwModuleIndex = 0;
        }
        catch (global::System.Exception e)
        {
            return e.HResult;
        }

        return HResults.S_OK;
    }

    public unsafe int GetNestedExceptionData(ulong exception, ulong* exceptionObject, ulong* nextNestedException)
    {
        try
        {
            Contracts.IException contract = _target.Contracts.Exception;
            TargetPointer exceptionObjectLocal = contract.GetNestedExceptionInfo(exception, out TargetPointer nextNestedExceptionLocal);
            *exceptionObject = exceptionObjectLocal;
            *nextNestedException = nextNestedExceptionLocal;
        }
        catch (global::System.Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }

    public unsafe int GetObjectClassName(ulong obj, uint count, char* className, uint* pNeeded) => HResults.E_NOTIMPL;

    public unsafe int GetObjectData(ulong objAddr, DacpObjectData* data)
    {
        try
        {
            Contracts.IObject objectContract = _target.Contracts.Object;
            Contracts.IRuntimeTypeSystem runtimeTypeSystemContract = _target.Contracts.RuntimeTypeSystem;

            TargetPointer mt = objectContract.GetMethodTableAddress(objAddr);
            TypeHandle handle = runtimeTypeSystemContract.GetTypeHandle(mt);

            data->MethodTable = mt;
            data->Size = runtimeTypeSystemContract.GetBaseSize(handle);
            data->dwComponentSize = runtimeTypeSystemContract.GetComponentSize(handle); ;

            if (runtimeTypeSystemContract.IsFreeObjectMethodTable(handle))
            {
                data->ObjectType = DacpObjectType.OBJ_FREE;
            }
            else if (mt == _stringMethodTable)
            {
                data->ObjectType = DacpObjectType.OBJ_STRING;

                // Update the size to include the string character components
                data->Size += (uint)objectContract.GetStringValue(objAddr).Length * data->dwComponentSize;
            }
            else if (mt == _objectMethodTable)
            {
                data->ObjectType = DacpObjectType.OBJ_OBJECT;
            }
            else if (runtimeTypeSystemContract.IsArray(handle, out uint rank))
            {
                data->ObjectType = DacpObjectType.OBJ_ARRAY;
                data->dwRank = rank;

                TargetPointer arrayData = objectContract.GetArrayData(objAddr, out uint numComponents, out TargetPointer boundsStart, out TargetPointer lowerBounds);
                data->ArrayDataPtr = arrayData;
                data->dwNumComponents = numComponents;
                data->ArrayBoundsPtr = boundsStart;
                data->ArrayLowerBoundsPtr = lowerBounds;

                // Update the size to include the array components
                data->Size += numComponents * data->dwComponentSize;

                // Get the type of the array elements
                TypeHandle element = runtimeTypeSystemContract.GetTypeParam(handle);
                data->ElementTypeHandle = element.Address;
                data->ElementType = (uint)runtimeTypeSystemContract.GetSignatureCorElementType(element);

                // Validate the element type handles for arrays of arrays
                while (runtimeTypeSystemContract.IsArray(element, out _))
                {
                    element = runtimeTypeSystemContract.GetTypeParam(element);
                }
            }
            else
            {
                data->ObjectType = DacpObjectType.OBJ_OTHER;
            }

            // TODO: [cdac] Get RCW and CCW from interop info on sync block
            if (_target.ReadGlobal<byte>(Constants.Globals.FeatureCOMInterop) != 0)
                return HResults.E_NOTIMPL;

        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }

    public unsafe int GetObjectExceptionData(ulong objectAddress, DacpExceptionObjectData* data)
    {
        try
        {
            Contracts.IException contract = _target.Contracts.Exception;
            Contracts.ExceptionData exceptionData = contract.GetExceptionData(objectAddress);
            data->Message = exceptionData.Message;
            data->InnerException = exceptionData.InnerException;
            data->StackTrace = exceptionData.StackTrace;
            data->WatsonBuckets = exceptionData.WatsonBuckets;
            data->StackTraceString = exceptionData.StackTraceString;
            data->RemoteStackTraceString = exceptionData.RemoteStackTraceString;
            data->HResult = exceptionData.HResult;
            data->XCode = exceptionData.XCode;
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }

    public unsafe int GetObjectStringData(ulong obj, uint count, char* stringData, uint* pNeeded)
    {
        try
        {
            Contracts.IObject contract = _target.Contracts.Object;
            string str = contract.GetStringValue(obj);
            CopyStringToTargetBuffer(stringData, count, pNeeded, str);
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }
    public unsafe int GetOOMData(ulong oomAddr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetOOMStaticData(void* data) => HResults.E_NOTIMPL;
    public unsafe int GetPEFileBase(ulong addr, ulong* peBase) => HResults.E_NOTIMPL;
    public unsafe int GetPEFileName(ulong addr, uint count, char* fileName, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetPrivateBinPaths(ulong appDomain, int count, char* paths, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetRCWData(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetRCWInterfaces(ulong rcw, uint count, void* interfaces, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetRegisterName(int regName, uint count, char* buffer, uint* pNeeded) => HResults.E_NOTIMPL;
    public unsafe int GetStackLimits(ulong threadPtr, ulong* lower, ulong* upper, ulong* fp) => HResults.E_NOTIMPL;
    public unsafe int GetStackReferences(int osThreadID, void** ppEnum) => HResults.E_NOTIMPL;
    public unsafe int GetStressLogAddress(ulong* stressLog) => HResults.E_NOTIMPL;
    public unsafe int GetSyncBlockCleanupData(ulong addr, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetSyncBlockData(uint number, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetThreadAllocData(ulong thread, void* data) => HResults.E_NOTIMPL;

    public unsafe int GetThreadData(ulong thread, DacpThreadData* data)
    {
        try
        {
            Contracts.IThread contract = _target.Contracts.Thread;
            Contracts.ThreadData threadData = contract.GetThreadData(thread);
            data->corThreadId = (int)threadData.Id;
            data->osThreadId = (int)threadData.OSId.Value;
            data->state = (int)threadData.State;
            data->preemptiveGCDisabled = (uint)(threadData.PreemptiveGCDisabled ? 1 : 0);
            data->allocContextPtr = threadData.AllocContextPointer;
            data->allocContextLimit = threadData.AllocContextLimit;
            data->fiberData = 0;    // Always set to 0 - fibers are no longer supported

            TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
            TargetPointer appDomain = _target.ReadPointer(appDomainPointer);
            data->context = appDomain;
            data->domain = appDomain;

            data->lockCount = -1;   // Always set to -1 - lock count was .NET Framework and no longer needed
            data->pFrame = threadData.Frame;
            data->firstNestedException = threadData.FirstNestedException;
            data->teb = threadData.TEB;
            data->lastThrownObjectHandle = threadData.LastThrownObjectHandle;
            data->nextThread = threadData.NextThread;
        }
        catch (global::System.Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }
    public unsafe int GetThreadFromThinlockID(uint thinLockId, ulong* pThread) => HResults.E_NOTIMPL;
    public unsafe int GetThreadLocalModuleData(ulong thread, uint index, void* data) => HResults.E_NOTIMPL;
    public unsafe int GetThreadpoolData(void* data) => HResults.E_NOTIMPL;

    public unsafe int GetThreadStoreData(DacpThreadStoreData* data)
    {
        try
        {
            Contracts.IThread thread = _target.Contracts.Thread;
            Contracts.ThreadStoreData threadStoreData = thread.GetThreadStoreData();
            data->threadCount = threadStoreData.ThreadCount;
            data->firstThread = threadStoreData.FirstThread;
            data->finalizerThread = threadStoreData.FinalizerThread;
            data->gcThread = threadStoreData.GCThread;

            Contracts.ThreadStoreCounts threadCounts = thread.GetThreadCounts();
            data->unstartedThreadCount = threadCounts.UnstartedThreadCount;
            data->backgroundThreadCount = threadCounts.BackgroundThreadCount;
            data->pendingThreadCount = threadCounts.PendingThreadCount;
            data->deadThreadCount = threadCounts.DeadThreadCount;

            data->fHostConfig = 0; // Always 0 for non-Framework
        }
        catch (global::System.Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }

    public unsafe int GetTLSIndex(uint* pIndex) => HResults.E_NOTIMPL;

    public unsafe int GetUsefulGlobals(DacpUsefulGlobalsData* data)
    {
        try
        {
            data->ArrayMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.ObjectArrayMethodTable));
            data->StringMethodTable = _stringMethodTable;
            data->ObjectMethodTable = _objectMethodTable;
            data->ExceptionMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.ExceptionMethodTable));
            data->FreeMethodTable = _target.ReadPointer(
                _target.ReadGlobalPointer(Constants.Globals.FreeObjectMethodTable));
        }
        catch (System.Exception ex)
        {
            return ex.HResult;
        }

        return HResults.S_OK;
    }

    public unsafe int GetWorkRequestData(ulong addrWorkRequest, void* data) => HResults.E_NOTIMPL;
    public unsafe int IsRCWDCOMProxy(ulong rcwAddress, int* inDCOMProxy) => HResults.E_NOTIMPL;
    public unsafe int TraverseEHInfo(ulong ip, void* pCallback, void* token) => HResults.E_NOTIMPL;
    public unsafe int TraverseLoaderHeap(ulong loaderHeapAddr, void* pCallback) => HResults.E_NOTIMPL;
    public unsafe int TraverseModuleMap(int mmt, ulong moduleAddr, void* pCallback, void* token) => HResults.E_NOTIMPL;
    public unsafe int TraverseRCWCleanupList(ulong cleanupListPtr, void* pCallback, void* token) => HResults.E_NOTIMPL;
    public unsafe int TraverseVirtCallStubHeap(ulong pAppDomain, int heaptype, void* pCallback) => HResults.E_NOTIMPL;
}
