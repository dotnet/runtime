// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;

using Microsoft.Diagnostics.DataContractReader.Contracts;
using Data = Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe class DacDbiImpl : IDacDbiInterface
{
    private readonly Target _target;
    private readonly IDacDbiInterface? _legacy;

    // Call IStringHolder::AssignCopy(const WCHAR* psz).
    // IStringHolder vtable: slot 0 = AssignCopy (no virtual destructor).
    private static int StringHolderAssignCopy(nint pStringHolder, string value)
    {
        fixed (char* pStr = value)
        {
            nint vtable = *(nint*)pStringHolder;
            // On Linux x64: default calling convention, this is first arg.
            // On Windows x64: same (x64 has uniform calling convention).
            var assignCopy = (delegate* unmanaged<nint, char*, int>)(*(nint*)vtable);
            return assignCopy(pStringHolder, pStr);
        }
    }

    public DacDbiImpl(Target target, object? legacyObj)
    {
        _target = target;
        _legacy = legacyObj as IDacDbiInterface;
    }

    public int CheckDbiVersion(DbiVersion* pVersion) => _legacy is not null ? _legacy.CheckDbiVersion(pVersion) : HResults.E_NOTIMPL;

    public int FlushCache() => _legacy is not null ? _legacy.FlushCache() : HResults.E_NOTIMPL;

    public int DacSetTargetConsistencyChecks(bool fEnableAsserts) => _legacy is not null ? _legacy.DacSetTargetConsistencyChecks(fEnableAsserts) : HResults.E_NOTIMPL;

    public int Destroy() => _legacy is not null ? _legacy.Destroy() : HResults.E_NOTIMPL;

    public int IsLeftSideInitialized(int* pResult)
    {
        TargetPointer debuggerPtr = _target.ReadGlobalPointer(Constants.Globals.Debugger);
        if (debuggerPtr != TargetPointer.Null)
        {
            Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerPtr);
            *pResult = debugger.LeftSideInitialized != 0 ? 1 : 0;
        }
        else
        {
            *pResult = 0;
        }
#if DEBUG
        if (_legacy is not null)
        {
            int legacyResult;
            int legacyHr = _legacy.IsLeftSideInitialized(&legacyResult);
            Debug.Assert(legacyHr == HResults.S_OK);
            Debug.Assert(legacyResult == *pResult, $"IsLeftSideInitialized mismatch: cDAC={*pResult} legacy={legacyResult}");
        }
#endif
        return HResults.S_OK;
    }

    public int GetAppDomainFromId(uint appdomainId, ulong* pRetVal)
    {
        // In .NET Core (single AppDomain), the only valid appdomainId is DefaultADID=1.
        // Return the global AppDomain pointer.
        TargetPointer appDomain = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
        *pRetVal = appDomain.Value;
#if DEBUG
        if (_legacy is not null)
        {
            ulong legacyResult;
            int legacyHr = _legacy.GetAppDomainFromId(appdomainId, &legacyResult);
            Debug.Assert(legacyHr == HResults.S_OK);
            Debug.Assert(legacyResult == *pRetVal, $"GetAppDomainFromId mismatch: cDAC={*pRetVal:x} legacy={legacyResult:x}");
        }
#endif
        return HResults.S_OK;
    }

    public int GetAppDomainId(ulong vmAppDomain, uint* pRetVal)
    {
        // Native implementation returns 0 for null, DefaultADID (1) otherwise
        *pRetVal = (vmAppDomain == 0) ? 0u : 1u;
        return HResults.S_OK;
    }

    public int GetAppDomainObject(ulong vmAppDomain, ulong* pRetVal)
    {
        // AppDomain::GetRawExposedObjectHandleForDebugger() always returns NULL in .NET Core
        *pRetVal = 0;
        return HResults.S_OK;
    }

    public int GetAssemblyFromDomainAssembly(ulong vmDomainAssembly, ulong* vmAssembly)
    {
        // In .NET Core, DomainAssembly is essentially Module.
        // Use the Loader contract to read Module.Assembly.
        ILoader loader = _target.Contracts.Loader;
        ModuleHandle handle = new ModuleHandle(new TargetPointer(vmDomainAssembly));
        TargetPointer assembly = loader.GetAssembly(handle);
        *vmAssembly = assembly.Value;
#if DEBUG
        if (_legacy is not null)
        {
            ulong legacyResult;
            int legacyHr = _legacy.GetAssemblyFromDomainAssembly(vmDomainAssembly, &legacyResult);
            Debug.Assert(legacyHr == HResults.S_OK);
            Debug.Assert(legacyResult == *vmAssembly, $"GetAssemblyFromDomainAssembly mismatch: cDAC={*vmAssembly:x} legacy={legacyResult:x}");
        }
#endif
        return HResults.S_OK;
    }

    public int IsAssemblyFullyTrusted(ulong vmDomainAssembly, int* pResult)
    {
        // Native implementation always returns TRUE (full trust is the only mode)
        *pResult = 1;
        return HResults.S_OK;
    }

    public int GetAppDomainFullName(ulong vmAppDomain, nint pStrName)
    {
        string name = _target.Contracts.Loader.GetAppDomainFriendlyName();
        return StringHolderAssignCopy(pStrName, name);
    }

    public int GetModuleSimpleName(ulong vmModule, nint pStrFilename) => _legacy is not null ? _legacy.GetModuleSimpleName(vmModule, pStrFilename) : HResults.E_NOTIMPL;

    public int GetAssemblyPath(ulong vmAssembly, nint pStrFilename, int* pResult)
    {
        ILoader loader = _target.Contracts.Loader;
        ModuleHandle mh = loader.GetModuleHandleFromAssemblyPtr(new TargetPointer(vmAssembly));
        string path = loader.GetPath(mh);
        int hr = StringHolderAssignCopy(pStrFilename, path ?? string.Empty);
        if (hr < 0) return hr;
        *pResult = string.IsNullOrEmpty(path) ? 0 : 1;
        return HResults.S_OK;
    }

    public int ResolveTypeReference(nint pTypeRefInfo, nint pTargetRefInfo) => _legacy is not null ? _legacy.ResolveTypeReference(pTypeRefInfo, pTargetRefInfo) : HResults.E_NOTIMPL;

    public int GetModulePath(ulong vmModule, nint pStrFilename, int* pResult)
    {
        ILoader loader = _target.Contracts.Loader;
        ModuleHandle mh = loader.GetModuleHandleFromModulePtr(new TargetPointer(vmModule));
        string path = loader.GetPath(mh);
        if (!string.IsNullOrEmpty(path))
        {
            int hr = StringHolderAssignCopy(pStrFilename, path);
            if (hr < 0) return hr;
            *pResult = 1; // TRUE
        }
        else
        {
            StringHolderAssignCopy(pStrFilename, string.Empty);
            *pResult = 0; // FALSE
        }
        return HResults.S_OK;
    }

    public int GetMetadata(ulong vmModule, nint pTargetBuffer) => _legacy is not null ? _legacy.GetMetadata(vmModule, pTargetBuffer) : HResults.E_NOTIMPL;

    public int GetSymbolsBuffer(ulong vmModule, nint pTargetBuffer, int* pSymbolFormat)
    {
        // TargetBuffer: { CORDB_ADDRESS pAddress(8); ULONG cbSize(4); }
        // SymbolFormat: kSymbolFormatNone=0, kSymbolFormatPDB=1
        *(ulong*)pTargetBuffer = 0;
        *(uint*)(pTargetBuffer + 8) = 0;
        *pSymbolFormat = 0; // kSymbolFormatNone

        ILoader loader = _target.Contracts.Loader;
        ModuleHandle mh = loader.GetModuleHandleFromModulePtr(new TargetPointer(vmModule));
        if (loader.TryGetSymbolStream(mh, out TargetPointer buffer, out uint size) && size > 0)
        {
            *(ulong*)pTargetBuffer = buffer.Value;
            *(uint*)(pTargetBuffer + 8) = size;
            *pSymbolFormat = 1; // kSymbolFormatPDB
        }
        return HResults.S_OK;
    }

    public int GetModuleData(ulong vmModule, nint pData)
    {
        // ModuleInfo: { vmAssembly(8), pPEBaseAddress(8), vmPEAssembly(8), nPESize(4), fIsDynamic(4), fInMemory(4) }
        ILoader loader = _target.Contracts.Loader;
        ModuleHandle mh = loader.GetModuleHandleFromModulePtr(new TargetPointer(vmModule));

        // Zero-init the struct
        new Span<byte>((void*)pData, 40).Clear();

        ulong* p = (ulong*)pData;
        // vmAssembly
        p[0] = loader.GetAssembly(mh).Value;
        // vmPEAssembly
        TargetPointer peAssembly = loader.GetPEAssembly(mh);
        p[2] = peAssembly.Value;

        // pPEBaseAddress, nPESize
        bool isDynamic = (loader.GetFlags(mh) & ModuleFlags.ReflectionEmit) != 0;
        if (!isDynamic && loader.TryGetLoadedImageContents(mh, out TargetPointer baseAddr, out uint size, out uint _))
        {
            p[1] = baseAddr.Value; // pPEBaseAddress
            *(uint*)(pData + 24) = size; // nPESize
        }

        // fIsDynamic
        *(int*)(pData + 28) = isDynamic ? 1 : 0;

        // fInMemory - true if module has no path
        string path = loader.GetPath(mh);
        *(int*)(pData + 32) = string.IsNullOrEmpty(path) ? 1 : 0;

        return HResults.S_OK;
    }

    public unsafe int GetDomainAssemblyData(ulong vmDomainAssembly, nint pData)
    {
        // DomainAssemblyInfo: { VMPTR_AppDomain vmAppDomain; VMPTR_DomainAssembly vmDomainAssembly; }
        // Zero-init then fill
        ulong* p = (ulong*)pData;
        p[0] = _target.ReadGlobalPointer(Constants.Globals.AppDomain).Value;
        p[1] = vmDomainAssembly;
        return HResults.S_OK;
    }

    public unsafe int GetModuleForDomainAssembly(ulong vmDomainAssembly, ulong* pModule)
    {
        // DomainAssembly → Assembly → Module
        Data.DomainAssembly da = _target.ProcessedData.GetOrAdd<Data.DomainAssembly>(new TargetPointer(vmDomainAssembly));
        ILoader loader = _target.Contracts.Loader;
        ModuleHandle mh = loader.GetModuleHandleFromAssemblyPtr(da.Assembly);
        *pModule = loader.GetModule(mh).Value;
        return HResults.S_OK;
    }

    public int GetAddressType(ulong address, int* pRetVal) => _legacy is not null ? _legacy.GetAddressType(address, pRetVal) : HResults.E_NOTIMPL;

    public int IsTransitionStub(ulong address, int* pResult)
    {
        // On Unix/Linux, this is E_NOTIMPL (only used for VS mixed-mode debugging on Windows)
        return HResults.E_NOTIMPL;
    }

    public int GetCompilerFlags(ulong vmDomainAssembly, int* pfAllowJITOpts, int* pfEnableEnC) => _legacy is not null ? _legacy.GetCompilerFlags(vmDomainAssembly, pfAllowJITOpts, pfEnableEnC) : HResults.E_NOTIMPL;

    public int SetCompilerFlags(ulong vmDomainAssembly, int fAllowJitOpts, int fEnableEnC) => _legacy is not null ? _legacy.SetCompilerFlags(vmDomainAssembly, fAllowJitOpts, fEnableEnC) : HResults.E_NOTIMPL;

    public int EnumerateAppDomains(nint fpCallback, nint pUserData)
    {
        // Single AppDomain in .NET Core - call callback once with the global AppDomain
        TargetPointer appDomain = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
        ((delegate* unmanaged<ulong, nint, void>)fpCallback)(appDomain.Value, pUserData);
        return HResults.S_OK;
    }

    public unsafe int EnumerateAssembliesInAppDomain(ulong vmAppDomain, nint fpCallback, nint pUserData)
    {
        // Enumerate all loaded modules via ILoader and call back with each DomainAssembly
        ILoader loader = _target.Contracts.Loader;
        var flags = AssemblyIterationFlags.IncludeLoading | AssemblyIterationFlags.IncludeLoaded | AssemblyIterationFlags.IncludeExecution;
        foreach (ModuleHandle mh in loader.GetModuleHandles(new TargetPointer(vmAppDomain), flags))
        {
            TargetPointer moduleAddr = loader.GetModule(mh);
            Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(moduleAddr);
            ((delegate* unmanaged<ulong, nint, void>)fpCallback)(module.DomainAssembly.Value, pUserData);
        }
        return HResults.S_OK;
    }

    public unsafe int EnumerateModulesInAssembly(ulong vmAssembly, nint fpCallback, nint pUserData)
    {
        // In .NET Core, each assembly has exactly one module. The callback receives
        // the DomainAssembly pointer (same as input).
        ILoader loader = _target.Contracts.Loader;
        // Verify the assembly is loaded by reading the DomainAssembly → Assembly → Module chain
        Data.DomainAssembly da = _target.ProcessedData.GetOrAdd<Data.DomainAssembly>(new TargetPointer(vmAssembly));
        ModuleHandle mh = loader.GetModuleHandleFromAssemblyPtr(da.Assembly);
        if (loader.IsAssemblyLoaded(mh))
        {
            ((delegate* unmanaged<ulong, nint, void>)fpCallback)(vmAssembly, pUserData);
        }
        return HResults.S_OK;
    }

    public int RequestSyncAtEvent() => _legacy is not null ? _legacy.RequestSyncAtEvent() : HResults.E_NOTIMPL;

    public int SetSendExceptionsOutsideOfJMC(int sendExceptionsOutsideOfJMC) => _legacy is not null ? _legacy.SetSendExceptionsOutsideOfJMC(sendExceptionsOutsideOfJMC) : HResults.E_NOTIMPL;

    public int MarkDebuggerAttachPending() => _legacy is not null ? _legacy.MarkDebuggerAttachPending() : HResults.E_NOTIMPL;

    public int MarkDebuggerAttached(int fAttached) => _legacy is not null ? _legacy.MarkDebuggerAttached(fAttached) : HResults.E_NOTIMPL;

    public int Hijack(ulong vmThread, uint dwThreadId, nint pRecord, nint pOriginalContext, uint cbSizeContext, int reason, nint pUserData, ulong* pRemoteContextAddr) => _legacy is not null ? _legacy.Hijack(vmThread, dwThreadId, pRecord, pOriginalContext, cbSizeContext, reason, pUserData, pRemoteContextAddr) : HResults.E_NOTIMPL;

    public int EnumerateThreads(nint fpCallback, nint pUserData)
    {
        const uint TS_Dead = 0x800;
        const uint TS_Unstarted = 0x400;

        IThread threadContract = _target.Contracts.Thread;
        ThreadStoreData threadStore = threadContract.GetThreadStoreData();
        TargetPointer currentThread = threadStore.FirstThread;

        while (currentThread != TargetPointer.Null)
        {
            ThreadData threadData = threadContract.GetThreadData(currentThread);
            bool isDead = (threadData.State & TS_Dead) != 0;
            bool isUnstarted = (threadData.State & TS_Unstarted) != 0;

            if (!isDead && !isUnstarted)
            {
                // Call native callback: void(*)(VMPTR_Thread vmThread, void* pUserData)
                ((delegate* unmanaged<ulong, nint, void>)fpCallback)(currentThread.Value, pUserData);
            }

            currentThread = threadData.NextThread;
        }
        return HResults.S_OK;
    }

    public int IsThreadMarkedDead(ulong vmThread, byte* pResult)
    {
        int hr = HResults.S_OK;
        try
        {
            IThread threadContract = _target.Contracts.Thread;
            ThreadData threadData = threadContract.GetThreadData(new TargetPointer(vmThread));
            // TS_Dead = 0x00000800 (from threads.h)
            const uint TS_Dead = 0x00000800;
            *pResult = ((uint)threadData.State & TS_Dead) != 0 ? (byte)1 : (byte)0;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            byte resultLocal;
            int hrLocal = _legacy.IsThreadMarkedDead(vmThread, &resultLocal);
            Debug.Assert(hrLocal == hr, $"[DacDbi] IsThreadMarkedDead cDAC hr: 0x{hr:x}, DAC hr: 0x{hrLocal:x}");
            if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
            {
                Debug.Assert(*pResult == resultLocal, $"[DacDbi] IsThreadMarkedDead cDAC: {*pResult}, DAC: {resultLocal}");
            }
        }
#endif
        return hr;
    }

    public int GetThreadHandle(ulong vmThread, nint pRetVal) => _legacy is not null ? _legacy.GetThreadHandle(vmThread, pRetVal) : HResults.E_NOTIMPL;

    public int GetThreadObject(ulong vmThread, ulong* pRetVal)
    {
        const uint TS_Dead = 0x800;
        const uint TS_Unstarted = 0x400;
        const uint TS_Detached = 0x0040;

        IThread threadContract = _target.Contracts.Thread;
        ThreadData threadData = threadContract.GetThreadData(new TargetPointer(vmThread));

        if ((threadData.State & TS_Dead) != 0 ||
            (threadData.State & TS_Unstarted) != 0 ||
            (threadData.State & TS_Detached) != 0)
        {
            return unchecked((int)0x8013132d); // CORDBG_E_BAD_THREAD_STATE
        }

        // Read ExposedObject (named "GCHandle" in data descriptor)
        Target.TypeInfo type = _target.GetTypeInfo(DataType.Thread);
        TargetPointer exposedObject = _target.ReadPointer(new TargetPointer(vmThread) + (ulong)type.Fields["GCHandle"].Offset);
        *pRetVal = exposedObject.Value;
#if DEBUG
        if (_legacy is not null)
        {
            ulong legacyResult;
            int legacyHr = _legacy.GetThreadObject(vmThread, &legacyResult);
            Debug.Assert(legacyHr == HResults.S_OK || legacyHr == unchecked((int)0x8013132d));
            if (legacyHr == HResults.S_OK)
                Debug.Assert(legacyResult == *pRetVal, $"GetThreadObject mismatch: cDAC={*pRetVal:x} legacy={legacyResult:x}");
        }
#endif
        return HResults.S_OK;
    }

    public int GetThreadAllocInfo(ulong vmThread, nint pThreadAllocInfo)
    {
        // DacThreadAllocInfo: { ulong m_allocBytesSOH; ulong m_allocBytesUOH; }
        IThread threadContract = _target.Contracts.Thread;
        ThreadData threadData = threadContract.GetThreadData(new TargetPointer(vmThread));
        ulong* pInfo = (ulong*)pThreadAllocInfo;

        // Get raw alloc bytes and LOH bytes from the contract
        threadContract.GetThreadAllocContext(new TargetPointer(vmThread), out long allocBytes, out long allocBytesLoh);

        // SOH = alloc_bytes - (alloc_limit - alloc_ptr), matching native behavior
        long unused = (long)(threadData.AllocContextLimit.Value - threadData.AllocContextPointer.Value);
        pInfo[0] = (ulong)(allocBytes - unused);
        pInfo[1] = (ulong)allocBytesLoh;
        return HResults.S_OK;
    }

    public int SetDebugState(ulong vmThread, int debugState) => _legacy is not null ? _legacy.SetDebugState(vmThread, debugState) : HResults.E_NOTIMPL;

    public int HasUnhandledException(ulong vmThread, int* pResult) => _legacy is not null ? _legacy.HasUnhandledException(vmThread, pResult) : HResults.E_NOTIMPL;

    public int GetUserState(ulong vmThread, int* pRetVal) => _legacy is not null ? _legacy.GetUserState(vmThread, pRetVal) : HResults.E_NOTIMPL;

    public int GetPartialUserState(ulong vmThread, int* pRetVal)
    {
        // Thread state flags
        const uint TS_Background = 0x200;
        const uint TS_Unstarted = 0x400;
        const uint TS_Dead = 0x800;
        const uint TS_Interruptible = 0x2000000;
        const uint TS_TPWorkerThread = 0x1000000;
        // ThreadStateNC flags
        const uint TSNC_DebuggerSleepWaitJoin = 0x04000000;
        // CorDebugUserState
        const int USER_BACKGROUND = 0x4;
        const int USER_UNSTARTED = 0x8;
        const int USER_STOPPED = 0x10;
        const int USER_WAIT_SLEEP_JOIN = 0x20;
        const int USER_THREADPOOL = 0x100;

        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(new TargetPointer(vmThread));
        uint ts = thread.State;
        uint tsnc = thread.StateNC;
        int result = 0;

        if ((ts & TS_Background) != 0) result |= USER_BACKGROUND;
        if ((ts & TS_Unstarted) != 0) result |= USER_UNSTARTED;
        if ((ts & TS_Dead) != 0) result |= USER_STOPPED;
        if ((ts & TS_Interruptible) != 0 || (tsnc & TSNC_DebuggerSleepWaitJoin) != 0)
            result |= USER_WAIT_SLEEP_JOIN;
        if ((ts & TS_TPWorkerThread) != 0) result |= USER_THREADPOOL;

        *pRetVal = result;
        return HResults.S_OK;
    }

    public int GetConnectionID(ulong vmThread, uint* pRetVal)
    {
        // Native implementation always returns INVALID_CONNECTION_ID (0)
        *pRetVal = 0;
        return HResults.S_OK;
    }

    public int GetTaskID(ulong vmThread, ulong* pRetVal)
    {
        // Native implementation always returns INVALID_TASK_ID (0)
        *pRetVal = 0;
        return HResults.S_OK;
    }

    public int TryGetVolatileOSThreadID(ulong vmThread, uint* pRetVal)
    {
        int hr = HResults.S_OK;
        try
        {
            IThread threadContract = _target.Contracts.Thread;
            ThreadData threadData = threadContract.GetThreadData(new TargetPointer(vmThread));
            // SWITCHED_OUT_FIBER_OSID is a magic cookie; return 0 in that case
            const uint SWITCHED_OUT_FIBER_OSID = 0xbaadf00d;
            uint osId = (uint)threadData.OSId.Value;
            *pRetVal = (osId == SWITCHED_OUT_FIBER_OSID) ? 0u : osId;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            uint resultLocal;
            int hrLocal = _legacy.TryGetVolatileOSThreadID(vmThread, &resultLocal);
            Debug.Assert(hrLocal == hr, $"[DacDbi] TryGetVolatileOSThreadID cDAC hr: 0x{hr:x}, DAC hr: 0x{hrLocal:x}");
            if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
            {
                Debug.Assert(*pRetVal == resultLocal, $"[DacDbi] TryGetVolatileOSThreadID cDAC: {*pRetVal}, DAC: {resultLocal}");
            }
        }
#endif
        return hr;
    }

    public int GetUniqueThreadID(ulong vmThread, uint* pRetVal)
    {
        int hr = HResults.S_OK;
        try
        {
            IThread threadContract = _target.Contracts.Thread;
            ThreadData threadData = threadContract.GetThreadData(new TargetPointer(vmThread));
            *pRetVal = (uint)threadData.OSId.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            uint resultLocal;
            int hrLocal = _legacy.GetUniqueThreadID(vmThread, &resultLocal);
            Debug.Assert(hrLocal == hr, $"[DacDbi] GetUniqueThreadID cDAC hr: 0x{hr:x}, DAC hr: 0x{hrLocal:x}");
            if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
            {
                Debug.Assert(*pRetVal == resultLocal, $"[DacDbi] GetUniqueThreadID cDAC: {*pRetVal}, DAC: {resultLocal}");
            }
        }
#endif
        return hr;
    }

    public int GetCurrentException(ulong vmThread, ulong* pRetVal) => _legacy is not null ? _legacy.GetCurrentException(vmThread, pRetVal) : HResults.E_NOTIMPL;

    public int GetObjectForCCW(ulong ccwPtr, ulong* pRetVal) => _legacy is not null ? _legacy.GetObjectForCCW(ccwPtr, pRetVal) : HResults.E_NOTIMPL;

    public int GetCurrentCustomDebuggerNotification(ulong vmThread, ulong* pRetVal) => _legacy is not null ? _legacy.GetCurrentCustomDebuggerNotification(vmThread, pRetVal) : HResults.E_NOTIMPL;

    public int GetCurrentAppDomain(ulong* pRetVal)
    {
        int hr = HResults.S_OK;
        try
        {
            TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
            *pRetVal = _target.ReadPointer(appDomainPointer).Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong resultLocal;
            int hrLocal = _legacy.GetCurrentAppDomain(&resultLocal);
            Debug.Assert(hrLocal == hr, $"[DacDbi] GetCurrentAppDomain cDAC hr: 0x{hr:x}, DAC hr: 0x{hrLocal:x}");
            if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
            {
                Debug.Assert(*pRetVal == resultLocal, $"[DacDbi] GetCurrentAppDomain cDAC: 0x{*pRetVal:x}, DAC: 0x{resultLocal:x}");
            }
        }
#endif
        return hr;
    }

    public int ResolveAssembly(ulong vmScope, uint tkAssemblyRef, ulong* pRetVal) => _legacy is not null ? _legacy.ResolveAssembly(vmScope, tkAssemblyRef, pRetVal) : HResults.E_NOTIMPL;

    public int GetNativeCodeSequencePointsAndVarInfo(ulong vmMethodDesc, ulong startAddress, int fCodeAvailable, nint pNativeVarData, nint pSequencePoints) => _legacy is not null ? _legacy.GetNativeCodeSequencePointsAndVarInfo(vmMethodDesc, startAddress, fCodeAvailable, pNativeVarData, pSequencePoints) : HResults.E_NOTIMPL;

    public int GetManagedStoppedContext(ulong vmThread, ulong* pRetVal) => _legacy is not null ? _legacy.GetManagedStoppedContext(vmThread, pRetVal) : HResults.E_NOTIMPL;

    public int CreateStackWalk(ulong vmThread, nint pInternalContextBuffer, nint ppSFIHandle) => _legacy is not null ? _legacy.CreateStackWalk(vmThread, pInternalContextBuffer, ppSFIHandle) : HResults.E_NOTIMPL;

    public int DeleteStackWalk(ulong ppSFIHandle) => _legacy is not null ? _legacy.DeleteStackWalk(ppSFIHandle) : HResults.E_NOTIMPL;

    public int GetStackWalkCurrentContext(ulong pSFIHandle, nint pContext) => _legacy is not null ? _legacy.GetStackWalkCurrentContext(pSFIHandle, pContext) : HResults.E_NOTIMPL;

    public int SetStackWalkCurrentContext(ulong vmThread, ulong pSFIHandle, int flag, nint pContext) => _legacy is not null ? _legacy.SetStackWalkCurrentContext(vmThread, pSFIHandle, flag, pContext) : HResults.E_NOTIMPL;

    public int UnwindStackWalkFrame(ulong pSFIHandle, int* pResult) => _legacy is not null ? _legacy.UnwindStackWalkFrame(pSFIHandle, pResult) : HResults.E_NOTIMPL;

    public int CheckContext(ulong vmThread, nint pContext) => _legacy is not null ? _legacy.CheckContext(vmThread, pContext) : HResults.E_NOTIMPL;

    public int GetStackWalkCurrentFrameInfo(ulong pSFIHandle, nint pFrameData, int* pRetVal) => _legacy is not null ? _legacy.GetStackWalkCurrentFrameInfo(pSFIHandle, pFrameData, pRetVal) : HResults.E_NOTIMPL;

    public int GetCountOfInternalFrames(ulong vmThread, uint* pRetVal) => _legacy is not null ? _legacy.GetCountOfInternalFrames(vmThread, pRetVal) : HResults.E_NOTIMPL;

    public int EnumerateInternalFrames(ulong vmThread, nint fpCallback, nint pUserData) => _legacy is not null ? _legacy.EnumerateInternalFrames(vmThread, fpCallback, pUserData) : HResults.E_NOTIMPL;

    public int IsMatchingParentFrame(ulong fpToCheck, ulong fpParent, int* pResult) => _legacy is not null ? _legacy.IsMatchingParentFrame(fpToCheck, fpParent, pResult) : HResults.E_NOTIMPL;

    public int GetStackParameterSize(ulong controlPC, uint* pRetVal) => _legacy is not null ? _legacy.GetStackParameterSize(controlPC, pRetVal) : HResults.E_NOTIMPL;

    public int GetFramePointer(ulong pSFIHandle, ulong* pRetVal) => _legacy is not null ? _legacy.GetFramePointer(pSFIHandle, pRetVal) : HResults.E_NOTIMPL;

    public int IsLeafFrame(ulong vmThread, nint pContext, int* pResult) => _legacy is not null ? _legacy.IsLeafFrame(vmThread, pContext, pResult) : HResults.E_NOTIMPL;

    public int GetContext(ulong vmThread, nint pContextBuffer) => _legacy is not null ? _legacy.GetContext(vmThread, pContextBuffer) : HResults.E_NOTIMPL;

    public int ConvertContextToDebuggerRegDisplay(nint pInContext, nint pOutDRD, int fActive) => _legacy is not null ? _legacy.ConvertContextToDebuggerRegDisplay(pInContext, pOutDRD, fActive) : HResults.E_NOTIMPL;

    public int IsDiagnosticsHiddenOrLCGMethod(ulong vmMethodDesc, int* pRetVal) => _legacy is not null ? _legacy.IsDiagnosticsHiddenOrLCGMethod(vmMethodDesc, pRetVal) : HResults.E_NOTIMPL;

    public int GetVarArgSig(ulong VASigCookieAddr, ulong* pArgBase, nint pRetVal) => _legacy is not null ? _legacy.GetVarArgSig(VASigCookieAddr, pArgBase, pRetVal) : HResults.E_NOTIMPL;

    public int RequiresAlign8(ulong thExact, int* pResult)
    {
        // FEATURE_64BIT_ALIGNMENT is only defined on ARM/WASM, not x64
        return HResults.E_NOTIMPL;
    }

    public int ResolveExactGenericArgsToken(uint dwExactGenericArgsTokenIndex, ulong rawToken, ulong* pRetVal) => _legacy is not null ? _legacy.ResolveExactGenericArgsToken(dwExactGenericArgsTokenIndex, rawToken, pRetVal) : HResults.E_NOTIMPL;

    public int GetILCodeAndSig(ulong vmDomainAssembly, uint functionToken, nint pCodeInfo, uint* pLocalSigToken) => _legacy is not null ? _legacy.GetILCodeAndSig(vmDomainAssembly, functionToken, pCodeInfo, pLocalSigToken) : HResults.E_NOTIMPL;

    public int GetNativeCodeInfo(ulong vmDomainAssembly, uint functionToken, nint pCodeInfo) => _legacy is not null ? _legacy.GetNativeCodeInfo(vmDomainAssembly, functionToken, pCodeInfo) : HResults.E_NOTIMPL;

    public int GetNativeCodeInfoForAddr(ulong codeAddress, nint pCodeInfo, ulong* pVmModule, uint* pFunctionToken) => _legacy is not null ? _legacy.GetNativeCodeInfoForAddr(codeAddress, pCodeInfo, pVmModule, pFunctionToken) : HResults.E_NOTIMPL;

    public int IsValueType(ulong th, int* pResult) => _legacy is not null ? _legacy.IsValueType(th, pResult) : HResults.E_NOTIMPL;

    public int HasTypeParams(ulong th, int* pResult) => _legacy is not null ? _legacy.HasTypeParams(th, pResult) : HResults.E_NOTIMPL;

    public int GetClassInfo(ulong vmAppDomain, ulong thExact, nint pData) => _legacy is not null ? _legacy.GetClassInfo(vmAppDomain, thExact, pData) : HResults.E_NOTIMPL;

    public int GetInstantiationFieldInfo(ulong vmDomainAssembly, ulong vmThExact, ulong vmThApprox, nint pFieldList, nuint* pObjectSize) => _legacy is not null ? _legacy.GetInstantiationFieldInfo(vmDomainAssembly, vmThExact, vmThApprox, pFieldList, pObjectSize) : HResults.E_NOTIMPL;

    public int TypeHandleToExpandedTypeInfo(int boxed, ulong vmAppDomain, ulong vmTypeHandle, nint pTypeInfo) => _legacy is not null ? _legacy.TypeHandleToExpandedTypeInfo(boxed, vmAppDomain, vmTypeHandle, pTypeInfo) : HResults.E_NOTIMPL;

    public int GetObjectExpandedTypeInfo(int boxed, ulong vmAppDomain, ulong addr, nint pTypeInfo) => _legacy is not null ? _legacy.GetObjectExpandedTypeInfo(boxed, vmAppDomain, addr, pTypeInfo) : HResults.E_NOTIMPL;

    public int GetObjectExpandedTypeInfoFromID(int boxed, ulong vmAppDomain, nint id, nint pTypeInfo) => _legacy is not null ? _legacy.GetObjectExpandedTypeInfoFromID(boxed, vmAppDomain, id, pTypeInfo) : HResults.E_NOTIMPL;

    public int GetTypeHandle(ulong vmModule, uint metadataToken, ulong* pRetVal) => _legacy is not null ? _legacy.GetTypeHandle(vmModule, metadataToken, pRetVal) : HResults.E_NOTIMPL;

    public int GetApproxTypeHandle(nint pTypeData, ulong* pRetVal) => _legacy is not null ? _legacy.GetApproxTypeHandle(pTypeData, pRetVal) : HResults.E_NOTIMPL;

    public int GetExactTypeHandle(nint pTypeData, nint pArgInfo, ulong* vmTypeHandle) => _legacy is not null ? _legacy.GetExactTypeHandle(pTypeData, pArgInfo, vmTypeHandle) : HResults.E_NOTIMPL;

    public int GetMethodDescParams(ulong vmAppDomain, ulong vmMethodDesc, ulong genericsToken, uint* pcGenericClassTypeParams, nint pGenericTypeParams) => _legacy is not null ? _legacy.GetMethodDescParams(vmAppDomain, vmMethodDesc, genericsToken, pcGenericClassTypeParams, pGenericTypeParams) : HResults.E_NOTIMPL;

    public int GetThreadStaticAddress(ulong vmField, ulong vmRuntimeThread, ulong* pRetVal) => _legacy is not null ? _legacy.GetThreadStaticAddress(vmField, vmRuntimeThread, pRetVal) : HResults.E_NOTIMPL;

    public int GetCollectibleTypeStaticAddress(ulong vmField, ulong vmAppDomain, ulong* pRetVal) => _legacy is not null ? _legacy.GetCollectibleTypeStaticAddress(vmField, vmAppDomain, pRetVal) : HResults.E_NOTIMPL;

    public int GetEnCHangingFieldInfo(nint pEnCFieldInfo, nint pFieldData, int* pfStatic) => _legacy is not null ? _legacy.GetEnCHangingFieldInfo(pEnCFieldInfo, pFieldData, pfStatic) : HResults.E_NOTIMPL;

    public int GetTypeHandleParams(ulong vmAppDomain, ulong vmTypeHandle, nint pParams) => _legacy is not null ? _legacy.GetTypeHandleParams(vmAppDomain, vmTypeHandle, pParams) : HResults.E_NOTIMPL;

    public int GetSimpleType(ulong vmAppDomain, int simpleType, uint* pMetadataToken, ulong* pVmModule, ulong* pVmDomainAssembly) => _legacy is not null ? _legacy.GetSimpleType(vmAppDomain, simpleType, pMetadataToken, pVmModule, pVmDomainAssembly) : HResults.E_NOTIMPL;

    public int IsExceptionObject(ulong vmObject, int* pResult) => _legacy is not null ? _legacy.IsExceptionObject(vmObject, pResult) : HResults.E_NOTIMPL;

    public int GetStackFramesFromException(ulong vmObject, nint dacStackFrames) => _legacy is not null ? _legacy.GetStackFramesFromException(vmObject, dacStackFrames) : HResults.E_NOTIMPL;

    public int IsRcw(ulong vmObject, int* pResult)
    {
        // FEATURE_COMINTEROP is not defined on Linux; always returns FALSE
        *pResult = 0;
        return HResults.S_OK;
    }

    public int GetRcwCachedInterfaceTypes(ulong vmObject, ulong vmAppDomain, int bIInspectableOnly, nint pDacInterfaces) => _legacy is not null ? _legacy.GetRcwCachedInterfaceTypes(vmObject, vmAppDomain, bIInspectableOnly, pDacInterfaces) : HResults.E_NOTIMPL;

    public int GetRcwCachedInterfacePointers(ulong vmObject, int bIInspectableOnly, nint pDacItfPtrs) => _legacy is not null ? _legacy.GetRcwCachedInterfacePointers(vmObject, bIInspectableOnly, pDacItfPtrs) : HResults.E_NOTIMPL;

    public int GetCachedWinRTTypesForIIDs(ulong vmAppDomain, nint iids, nint pTypes) => _legacy is not null ? _legacy.GetCachedWinRTTypesForIIDs(vmAppDomain, iids, pTypes) : HResults.E_NOTIMPL;

    public int GetCachedWinRTTypes(ulong vmAppDomain, nint piids, nint pTypes) => _legacy is not null ? _legacy.GetCachedWinRTTypes(vmAppDomain, piids, pTypes) : HResults.E_NOTIMPL;

    public int GetTypedByRefInfo(ulong pTypedByRef, ulong vmAppDomain, nint pObjectData) => _legacy is not null ? _legacy.GetTypedByRefInfo(pTypedByRef, vmAppDomain, pObjectData) : HResults.E_NOTIMPL;

    public int GetStringData(ulong objectAddress, nint pObjectData) => _legacy is not null ? _legacy.GetStringData(objectAddress, pObjectData) : HResults.E_NOTIMPL;

    public int GetArrayData(ulong objectAddress, nint pObjectData) => _legacy is not null ? _legacy.GetArrayData(objectAddress, pObjectData) : HResults.E_NOTIMPL;

    public int GetBasicObjectInfo(ulong objectAddress, int type, ulong vmAppDomain, nint pObjectData) => _legacy is not null ? _legacy.GetBasicObjectInfo(objectAddress, type, vmAppDomain, pObjectData) : HResults.E_NOTIMPL;

    public int TestCrst(ulong vmCrst) => _legacy is not null ? _legacy.TestCrst(vmCrst) : HResults.E_NOTIMPL;

    public int TestRWLock(ulong vmRWLock) => _legacy is not null ? _legacy.TestRWLock(vmRWLock) : HResults.E_NOTIMPL;

    public int GetDebuggerControlBlockAddress(ulong* pRetVal) => _legacy is not null ? _legacy.GetDebuggerControlBlockAddress(pRetVal) : HResults.E_NOTIMPL;

    public int GetObjectFromRefPtr(ulong ptr, ulong* pRetVal)
    {
        int hr = HResults.S_OK;
        try
        {
            // Dereference the ObjectRef pointer to get the actual object address
            TargetPointer objRef = _target.ReadPointer(new TargetPointer(ptr));
            *pRetVal = objRef.Value;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            ulong resultLocal;
            int hrLocal = _legacy.GetObjectFromRefPtr(ptr, &resultLocal);
            Debug.Assert(hrLocal == hr, $"[DacDbi] GetObjectFromRefPtr cDAC hr: 0x{hr:x}, DAC hr: 0x{hrLocal:x}");
            if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
            {
                Debug.Assert(*pRetVal == resultLocal, $"[DacDbi] GetObjectFromRefPtr cDAC: 0x{*pRetVal:x}, DAC: 0x{resultLocal:x}");
            }
        }
#endif
        return hr;
    }

    public int GetObject(ulong ptr, ulong* pRetVal)
    {
        // Native implementation wraps the address directly as a VMPTR_Object
        *pRetVal = ptr;
        return HResults.S_OK;
    }

    public int EnableNGENPolicy(int ePolicy)
    {
        // Native implementation returns E_NOTIMPL
        return HResults.E_NOTIMPL;
    }

    public int SetNGENCompilerFlags(uint dwFlags)
    {
        // Native implementation returns CORDBG_E_NGEN_NOT_SUPPORTED
        return unchecked((int)0x80131c14);
    }

    public int GetNGENCompilerFlags(uint* pdwFlags)
    {
        // Native implementation returns CORDBG_E_NGEN_NOT_SUPPORTED
        return unchecked((int)0x80131c14);
    }

    public int GetVmObjectHandle(ulong handleAddress, ulong* pRetVal)
    {
        // Native implementation wraps the address directly as a VMPTR_OBJECTHANDLE
        *pRetVal = handleAddress;
        return HResults.S_OK;
    }

    public int IsVmObjectHandleValid(ulong vmHandle, int* pResult) => _legacy is not null ? _legacy.IsVmObjectHandleValid(vmHandle, pResult) : HResults.E_NOTIMPL;

    public int IsWinRTModule(ulong vmModule, int* isWinRT)
    {
        // Native implementation always returns FALSE
        *isWinRT = 0;
        return HResults.S_OK;
    }

    public int GetAppDomainIdFromVmObjectHandle(ulong vmHandle, uint* pRetVal)
    {
        // Native implementation always returns DefaultADID (1)
        *pRetVal = 1;
        return HResults.S_OK;
    }

    public int GetHandleAddressFromVmHandle(ulong vmHandle, ulong* pRetVal)
    {
        // Native implementation unwraps the VMPTR to get the raw address
        *pRetVal = vmHandle;
        return HResults.S_OK;
    }

    public int GetObjectContents(ulong obj, nint pRetVal) => _legacy is not null ? _legacy.GetObjectContents(obj, pRetVal) : HResults.E_NOTIMPL;

    public int GetThreadOwningMonitorLock(ulong vmObject, nint pRetVal) => _legacy is not null ? _legacy.GetThreadOwningMonitorLock(vmObject, pRetVal) : HResults.E_NOTIMPL;

    public int EnumerateMonitorEventWaitList(ulong vmObject, nint fpCallback, nint pUserData) => _legacy is not null ? _legacy.EnumerateMonitorEventWaitList(vmObject, fpCallback, pUserData) : HResults.E_NOTIMPL;

    public int GetAttachStateFlags(int* pRetVal) => _legacy is not null ? _legacy.GetAttachStateFlags(pRetVal) : HResults.E_NOTIMPL;

    public int GetMetaDataFileInfoFromPEFile(ulong vmPEAssembly, uint* dwTimeStamp, uint* dwImageSize, nint pStrFilename, byte* pResult) => _legacy is not null ? _legacy.GetMetaDataFileInfoFromPEFile(vmPEAssembly, dwTimeStamp, dwImageSize, pStrFilename, pResult) : HResults.E_NOTIMPL;

    public int IsThreadSuspendedOrHijacked(ulong vmThread, byte* pResult)
    {
        int hr = HResults.S_OK;
        try
        {
            IThread threadContract = _target.Contracts.Thread;
            ThreadData threadData = threadContract.GetThreadData(new TargetPointer(vmThread));
            uint state = (uint)threadData.State;
            // TS_SyncSuspended = 0x00080000, TS_Hijacked = 0x00000080 (from threads.h)
            const uint TS_SyncSuspended = 0x00080000;
            const uint TS_Hijacked = 0x00000080;
            *pResult = ((state & TS_SyncSuspended) != 0 || (state & TS_Hijacked) != 0) ? (byte)1 : (byte)0;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            byte resultLocal;
            int hrLocal = _legacy.IsThreadSuspendedOrHijacked(vmThread, &resultLocal);
            Debug.Assert(hrLocal == hr, $"[DacDbi] IsThreadSuspendedOrHijacked cDAC hr: 0x{hr:x}, DAC hr: 0x{hrLocal:x}");
            if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
            {
                Debug.Assert(*pResult == resultLocal, $"[DacDbi] IsThreadSuspendedOrHijacked cDAC: {*pResult}, DAC: {resultLocal}");
            }
        }
#endif
        return hr;
    }

    public int AreGCStructuresValid(byte* pResult)
    {
        int hr = HResults.S_OK;
        try
        {
            IGC gc = _target.Contracts.GC;
            *pResult = gc.GetGCStructuresValid() ? (byte)1 : (byte)0;
        }
        catch (System.Exception ex)
        {
            hr = ex.HResult;
        }
#if DEBUG
        if (_legacy is not null)
        {
            byte resultLocal;
            int hrLocal = _legacy.AreGCStructuresValid(&resultLocal);
            Debug.Assert(hrLocal == hr, $"[DacDbi] AreGCStructuresValid cDAC hr: 0x{hr:x}, DAC hr: 0x{hrLocal:x}");
            if (hr == HResults.S_OK && hrLocal == HResults.S_OK)
            {
                Debug.Assert(*pResult == resultLocal, $"[DacDbi] AreGCStructuresValid cDAC: {*pResult}, DAC: {resultLocal}");
            }
        }
#endif
        return hr;
    }

    public int CreateHeapWalk(nuint* pHandle) => _legacy is not null ? _legacy.CreateHeapWalk(pHandle) : HResults.E_NOTIMPL;

    public int DeleteHeapWalk(nuint handle) => _legacy is not null ? _legacy.DeleteHeapWalk(handle) : HResults.E_NOTIMPL;

    public int WalkHeap(nuint handle, uint count, nint objects, uint* fetched) => _legacy is not null ? _legacy.WalkHeap(handle, count, objects, fetched) : HResults.E_NOTIMPL;

    public int GetHeapSegments(nint pSegments) => _legacy is not null ? _legacy.GetHeapSegments(pSegments) : HResults.E_NOTIMPL;

    public int IsValidObject(ulong obj, byte* pResult) => _legacy is not null ? _legacy.IsValidObject(obj, pResult) : HResults.E_NOTIMPL;

    public int GetAppDomainForObject(ulong obj, ulong* pApp, ulong* pModule, ulong* pDomainAssembly, byte* pResult) => _legacy is not null ? _legacy.GetAppDomainForObject(obj, pApp, pModule, pDomainAssembly, pResult) : HResults.E_NOTIMPL;

    public int CreateRefWalk(nuint* pHandle, int walkStacks, int walkFQ, uint handleWalkMask) => _legacy is not null ? _legacy.CreateRefWalk(pHandle, walkStacks, walkFQ, handleWalkMask) : HResults.E_NOTIMPL;

    public int DeleteRefWalk(nuint handle) => _legacy is not null ? _legacy.DeleteRefWalk(handle) : HResults.E_NOTIMPL;

    public int WalkRefs(nuint handle, uint count, nint refs, uint* pFetched) => _legacy is not null ? _legacy.WalkRefs(handle, count, refs, pFetched) : HResults.E_NOTIMPL;

    public int GetTypeID(ulong obj, nint pType)
    {
        // COR_TYPEID: token1 = MethodTable address, token2 = 0
        IObject objectContract = _target.Contracts.Object;
        TargetPointer mt = objectContract.GetMethodTableAddress(new TargetPointer(obj));
        // COR_TYPEID is { uint64 token1; uint64 token2; }
        ulong* pTypeId = (ulong*)pType;
        pTypeId[0] = mt.Value;
        pTypeId[1] = 0;
#if DEBUG
        if (_legacy is not null)
        {
            // Allocate a COR_TYPEID-sized buffer for legacy comparison
            ulong* legacyBuf = stackalloc ulong[2];
            legacyBuf[0] = 0; legacyBuf[1] = 0;
            int legacyHr = _legacy.GetTypeID(obj, (nint)legacyBuf);
            Debug.Assert(legacyHr == HResults.S_OK);
            Debug.Assert(legacyBuf[0] == pTypeId[0], $"GetTypeID mismatch: cDAC={pTypeId[0]:x} legacy={legacyBuf[0]:x}");
        }
#endif
        return HResults.S_OK;
    }

    public int GetTypeIDForType(ulong vmTypeHandle, nint pId)
    {
        // COR_TYPEID.token1 = MethodTable address from TypeHandle, token2 = 0
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        TypeHandle th = rts.GetTypeHandle(new TargetPointer(vmTypeHandle));
        TargetPointer mt = rts.GetCanonicalMethodTable(th);
        ulong* pTypeId = (ulong*)pId;
        pTypeId[0] = mt.Value;
        pTypeId[1] = 0;
#if DEBUG
        if (_legacy is not null)
        {
            ulong* legacyBuf = stackalloc ulong[2];
            legacyBuf[0] = 0; legacyBuf[1] = 0;
            int legacyHr = _legacy.GetTypeIDForType(vmTypeHandle, (nint)legacyBuf);
            Debug.Assert(legacyHr == HResults.S_OK);
            Debug.Assert(legacyBuf[0] == pTypeId[0], $"GetTypeIDForType mismatch: cDAC={pTypeId[0]:x} legacy={legacyBuf[0]:x}");
        }
#endif
        return HResults.S_OK;
    }

    public int GetObjectFields(nint id, uint celt, nint layout, uint* pceltFetched) => _legacy is not null ? _legacy.GetObjectFields(id, celt, layout, pceltFetched) : HResults.E_NOTIMPL;

    public int GetTypeLayout(nint id, nint pLayout) => _legacy is not null ? _legacy.GetTypeLayout(id, pLayout) : HResults.E_NOTIMPL;

    public int GetArrayLayout(nint id, nint pLayout) => _legacy is not null ? _legacy.GetArrayLayout(id, pLayout) : HResults.E_NOTIMPL;

    public int GetGCHeapInformation(nint pHeapInfo) => _legacy is not null ? _legacy.GetGCHeapInformation(pHeapInfo) : HResults.E_NOTIMPL;

    public int GetPEFileMDInternalRW(ulong vmPEAssembly, ulong* pAddrMDInternalRW) => _legacy is not null ? _legacy.GetPEFileMDInternalRW(vmPEAssembly, pAddrMDInternalRW) : HResults.E_NOTIMPL;

    public int GetReJitInfo(ulong vmModule, uint methodTk, ulong* pReJitInfo)
    {
        // Deprecated: "You shouldn't be calling this - use GetActiveRejitILCodeVersionNode instead"
        return HResults.S_OK;
    }

    public int GetReJitInfo(ulong vmMethod, ulong codeStartAddress, ulong* pReJitInfo)
    {
        // Deprecated: "You shouldn't be calling this - use GetNativeCodeVersionNode instead"
        return HResults.S_OK;
    }

    public int GetSharedReJitInfo(ulong vmReJitInfo, ulong* pSharedReJitInfo)
    {
        // Deprecated: "You shouldn't be calling this - use GetILCodeVersionNode instead"
        return HResults.S_OK;
    }

    public int GetSharedReJitInfoData(ulong sharedReJitInfo, nint pData)
    {
        // Deprecated: "You shouldn't be calling this - use GetILCodeVersionNodeData instead"
        return HResults.S_OK;
    }

    public int AreOptimizationsDisabled(ulong vmModule, uint methodTk, int* pOptimizationsDisabled) => _legacy is not null ? _legacy.AreOptimizationsDisabled(vmModule, methodTk, pOptimizationsDisabled) : HResults.E_NOTIMPL;

    public int GetDefinesBitField(uint* pDefines)
    {
        if (pDefines == null) return HResults.E_INVALIDARG;
        TargetPointer debuggerPtr = _target.ReadGlobalPointer(Constants.Globals.Debugger);
        if (debuggerPtr == TargetPointer.Null)
            return unchecked((int)0x80131c23); // CORDBG_E_NOTREADY
        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerPtr);
        *pDefines = debugger.Defines;
#if DEBUG
        if (_legacy is not null)
        {
            uint legacyResult;
            int legacyHr = _legacy.GetDefinesBitField(&legacyResult);
            Debug.Assert(legacyHr == HResults.S_OK);
            Debug.Assert(legacyResult == *pDefines, $"GetDefinesBitField mismatch: cDAC={*pDefines:x} legacy={legacyResult:x}");
        }
#endif
        return HResults.S_OK;
    }

    public int GetMDStructuresVersion(uint* pMDStructuresVersion)
    {
        if (pMDStructuresVersion == null) return HResults.E_INVALIDARG;
        TargetPointer debuggerPtr = _target.ReadGlobalPointer(Constants.Globals.Debugger);
        if (debuggerPtr == TargetPointer.Null)
            return unchecked((int)0x80131c23); // CORDBG_E_NOTREADY
        Data.Debugger debugger = _target.ProcessedData.GetOrAdd<Data.Debugger>(debuggerPtr);
        *pMDStructuresVersion = debugger.MDStructuresVersion;
#if DEBUG
        if (_legacy is not null)
        {
            uint legacyResult;
            int legacyHr = _legacy.GetMDStructuresVersion(&legacyResult);
            Debug.Assert(legacyHr == HResults.S_OK);
            Debug.Assert(legacyResult == *pMDStructuresVersion, $"GetMDStructuresVersion mismatch: cDAC={*pMDStructuresVersion} legacy={legacyResult}");
        }
#endif
        return HResults.S_OK;
    }

    public int GetActiveRejitILCodeVersionNode(ulong vmModule, uint methodTk, ulong* pVmILCodeVersionNode) => _legacy is not null ? _legacy.GetActiveRejitILCodeVersionNode(vmModule, methodTk, pVmILCodeVersionNode) : HResults.E_NOTIMPL;

    public int GetNativeCodeVersionNode(ulong vmMethod, ulong codeStartAddress, ulong* pVmNativeCodeVersionNode) => _legacy is not null ? _legacy.GetNativeCodeVersionNode(vmMethod, codeStartAddress, pVmNativeCodeVersionNode) : HResults.E_NOTIMPL;

    public int GetILCodeVersionNode(ulong vmNativeCodeVersionNode, ulong* pVmILCodeVersionNode) => _legacy is not null ? _legacy.GetILCodeVersionNode(vmNativeCodeVersionNode, pVmILCodeVersionNode) : HResults.E_NOTIMPL;

    public int GetILCodeVersionNodeData(ulong ilCodeVersionNode, nint pData) => _legacy is not null ? _legacy.GetILCodeVersionNodeData(ilCodeVersionNode, pData) : HResults.E_NOTIMPL;

    public int EnableGCNotificationEvents(int fEnable) => _legacy is not null ? _legacy.EnableGCNotificationEvents(fEnable) : HResults.E_NOTIMPL;

    public int IsDelegate(ulong vmObject, int* pResult) => _legacy is not null ? _legacy.IsDelegate(vmObject, pResult) : HResults.E_NOTIMPL;

    public int GetDelegateType(ulong delegateObject, int* delegateType) => _legacy is not null ? _legacy.GetDelegateType(delegateObject, delegateType) : HResults.E_NOTIMPL;

    public int GetDelegateFunctionData(int delegateType, ulong delegateObject, ulong* ppFunctionDomainAssembly, uint* pMethodDef) => _legacy is not null ? _legacy.GetDelegateFunctionData(delegateType, delegateObject, ppFunctionDomainAssembly, pMethodDef) : HResults.E_NOTIMPL;

    public int GetDelegateTargetObject(int delegateType, ulong delegateObject, ulong* ppTargetObj, ulong* ppTargetAppDomain) => _legacy is not null ? _legacy.GetDelegateTargetObject(delegateType, delegateObject, ppTargetObj, ppTargetAppDomain) : HResults.E_NOTIMPL;

    public int GetLoaderHeapMemoryRanges(nint pRanges) => _legacy is not null ? _legacy.GetLoaderHeapMemoryRanges(pRanges) : HResults.E_NOTIMPL;

    public int IsModuleMapped(ulong pModule, int* isModuleMapped)
    {
        ILoader loader = _target.Contracts.Loader;
        ModuleHandle mh = loader.GetModuleHandleFromModulePtr(new TargetPointer(pModule));
        if (loader.TryGetLoadedImageContents(mh, out _, out _, out uint imageFlags))
        {
            *isModuleMapped = (imageFlags & 0x01) != 0 ? 1 : 0; // FLAG_MAPPED
            return HResults.S_OK;
        }
        *isModuleMapped = 0;
        return 1; // S_FALSE - no loaded image
    }

    public int MetadataUpdatesApplied(byte* pResult) => _legacy is not null ? _legacy.MetadataUpdatesApplied(pResult) : HResults.E_NOTIMPL;

    public unsafe int GetDomainAssemblyFromModule(ulong vmModule, ulong* pVmDomainAssembly)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(new TargetPointer(vmModule));
        *pVmDomainAssembly = module.DomainAssembly.Value;
        return HResults.S_OK;
    }

    public int ParseContinuation(ulong continuationAddress, ulong* pDiagnosticIP, ulong* pNextContinuation, uint* pState) => _legacy is not null ? _legacy.ParseContinuation(continuationAddress, pDiagnosticIP, pNextContinuation, pState) : HResults.E_NOTIMPL;

    public int GetAsyncLocals(ulong vmMethod, ulong codeAddr, uint state, nint pAsyncLocals) => _legacy is not null ? _legacy.GetAsyncLocals(vmMethod, codeAddr, state, pAsyncLocals) : HResults.E_NOTIMPL;

    public int GetGenericArgTokenIndex(ulong vmMethod, uint* pIndex) => _legacy is not null ? _legacy.GetGenericArgTokenIndex(vmMethod, pIndex) : HResults.E_NOTIMPL;

}
