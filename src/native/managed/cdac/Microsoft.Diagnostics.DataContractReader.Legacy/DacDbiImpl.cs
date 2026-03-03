// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[GeneratedComClass]
public sealed unsafe class DacDbiImpl : IDacDbiInterfaceControl
{
    private readonly IDacDbiInterfaceControl? _legacy;

    public DacDbiImpl(Target target, object? legacyObj)
    {
        _ = target;
        _legacy = legacyObj as IDacDbiInterfaceControl;
    }

    public int CheckDbiVersion(DbiVersion* pVersion) => _legacy is not null ? _legacy.CheckDbiVersion(pVersion) : HResults.E_NOTIMPL;

    public int FlushCache() => _legacy is not null ? _legacy.FlushCache() : HResults.E_NOTIMPL;

    public int DacSetTargetConsistencyChecks(bool fEnableAsserts) => _legacy is not null ? _legacy.DacSetTargetConsistencyChecks(fEnableAsserts) : HResults.E_NOTIMPL;

    public int Destroy() => _legacy is not null ? _legacy.Destroy() : HResults.E_NOTIMPL;

    public int IsLeftSideInitialized(int* pResult) => _legacy is not null ? _legacy.IsLeftSideInitialized(pResult) : HResults.E_NOTIMPL;

    public int GetAppDomainFromId(uint appdomainId, ulong* pRetVal) => _legacy is not null ? _legacy.GetAppDomainFromId(appdomainId, pRetVal) : HResults.E_NOTIMPL;

    public int GetAppDomainId(ulong vmAppDomain, uint* pRetVal) => _legacy is not null ? _legacy.GetAppDomainId(vmAppDomain, pRetVal) : HResults.E_NOTIMPL;

    public int GetAppDomainObject(ulong vmAppDomain, ulong* pRetVal) => _legacy is not null ? _legacy.GetAppDomainObject(vmAppDomain, pRetVal) : HResults.E_NOTIMPL;

    public int GetAssemblyFromDomainAssembly(ulong vmDomainAssembly, ulong* vmAssembly) => _legacy is not null ? _legacy.GetAssemblyFromDomainAssembly(vmDomainAssembly, vmAssembly) : HResults.E_NOTIMPL;

    public int IsAssemblyFullyTrusted(ulong vmDomainAssembly, int* pResult) => _legacy is not null ? _legacy.IsAssemblyFullyTrusted(vmDomainAssembly, pResult) : HResults.E_NOTIMPL;

    public int GetAppDomainFullName(ulong vmAppDomain, nint pStrName) => _legacy is not null ? _legacy.GetAppDomainFullName(vmAppDomain, pStrName) : HResults.E_NOTIMPL;

    public int GetModuleSimpleName(ulong vmModule, nint pStrFilename) => _legacy is not null ? _legacy.GetModuleSimpleName(vmModule, pStrFilename) : HResults.E_NOTIMPL;

    public int GetAssemblyPath(ulong vmAssembly, nint pStrFilename, int* pResult) => _legacy is not null ? _legacy.GetAssemblyPath(vmAssembly, pStrFilename, pResult) : HResults.E_NOTIMPL;

    public int ResolveTypeReference(nint pTypeRefInfo, nint pTargetRefInfo) => _legacy is not null ? _legacy.ResolveTypeReference(pTypeRefInfo, pTargetRefInfo) : HResults.E_NOTIMPL;

    public int GetModulePath(ulong vmModule, nint pStrFilename, int* pResult) => _legacy is not null ? _legacy.GetModulePath(vmModule, pStrFilename, pResult) : HResults.E_NOTIMPL;

    public int GetMetadata(ulong vmModule, nint pTargetBuffer) => _legacy is not null ? _legacy.GetMetadata(vmModule, pTargetBuffer) : HResults.E_NOTIMPL;

    public int GetSymbolsBuffer(ulong vmModule, nint pTargetBuffer, int* pSymbolFormat) => _legacy is not null ? _legacy.GetSymbolsBuffer(vmModule, pTargetBuffer, pSymbolFormat) : HResults.E_NOTIMPL;

    public int GetModuleData(ulong vmModule, nint pData) => _legacy is not null ? _legacy.GetModuleData(vmModule, pData) : HResults.E_NOTIMPL;

    public int GetDomainAssemblyData(ulong vmDomainAssembly, nint pData) => _legacy is not null ? _legacy.GetDomainAssemblyData(vmDomainAssembly, pData) : HResults.E_NOTIMPL;

    public int GetModuleForDomainAssembly(ulong vmDomainAssembly, ulong* pModule) => _legacy is not null ? _legacy.GetModuleForDomainAssembly(vmDomainAssembly, pModule) : HResults.E_NOTIMPL;

    public int GetAddressType(ulong address, int* pRetVal) => _legacy is not null ? _legacy.GetAddressType(address, pRetVal) : HResults.E_NOTIMPL;

    public int IsTransitionStub(ulong address, int* pResult) => _legacy is not null ? _legacy.IsTransitionStub(address, pResult) : HResults.E_NOTIMPL;

    public int GetCompilerFlags(ulong vmDomainAssembly, int* pfAllowJITOpts, int* pfEnableEnC) => _legacy is not null ? _legacy.GetCompilerFlags(vmDomainAssembly, pfAllowJITOpts, pfEnableEnC) : HResults.E_NOTIMPL;

    public int SetCompilerFlags(ulong vmDomainAssembly, int fAllowJitOpts, int fEnableEnC) => _legacy is not null ? _legacy.SetCompilerFlags(vmDomainAssembly, fAllowJitOpts, fEnableEnC) : HResults.E_NOTIMPL;

    public int EnumerateAppDomains(nint fpCallback, nint pUserData) => _legacy is not null ? _legacy.EnumerateAppDomains(fpCallback, pUserData) : HResults.E_NOTIMPL;

    public int EnumerateAssembliesInAppDomain(ulong p0, nint p1, nint p2) => _legacy is not null ? _legacy.EnumerateAssembliesInAppDomain(p0, p1, p2) : HResults.E_NOTIMPL;

    public int EnumerateModulesInAssembly(ulong p0, nint p1, nint p2) => _legacy is not null ? _legacy.EnumerateModulesInAssembly(p0, p1, p2) : HResults.E_NOTIMPL;

    public int RequestSyncAtEvent() => _legacy is not null ? _legacy.RequestSyncAtEvent() : HResults.E_NOTIMPL;

    public int SetSendExceptionsOutsideOfJMC(int p0) => _legacy is not null ? _legacy.SetSendExceptionsOutsideOfJMC(p0) : HResults.E_NOTIMPL;

    public int MarkDebuggerAttachPending() => _legacy is not null ? _legacy.MarkDebuggerAttachPending() : HResults.E_NOTIMPL;

    public int MarkDebuggerAttached(int p0) => _legacy is not null ? _legacy.MarkDebuggerAttached(p0) : HResults.E_NOTIMPL;

    public int Hijack(ulong p0, uint p1, nint p2, nint p3, uint p4, nint p5, nint p6, nint p7) => _legacy is not null ? _legacy.Hijack(p0, p1, p2, p3, p4, p5, p6, p7) : HResults.E_NOTIMPL;

    public int EnumerateThreads(nint p0, nint p1) => _legacy is not null ? _legacy.EnumerateThreads(p0, p1) : HResults.E_NOTIMPL;

    public int IsThreadMarkedDead(ulong p0, nint p1) => _legacy is not null ? _legacy.IsThreadMarkedDead(p0, p1) : HResults.E_NOTIMPL;

    public int GetThreadHandle(ulong p0, nint p1) => _legacy is not null ? _legacy.GetThreadHandle(p0, p1) : HResults.E_NOTIMPL;

    public int GetThreadObject(ulong p0, nint p1) => _legacy is not null ? _legacy.GetThreadObject(p0, p1) : HResults.E_NOTIMPL;

    public int GetThreadAllocInfo(ulong p0, nint p1) => _legacy is not null ? _legacy.GetThreadAllocInfo(p0, p1) : HResults.E_NOTIMPL;

    public int SetDebugState(ulong p0, int p1) => _legacy is not null ? _legacy.SetDebugState(p0, p1) : HResults.E_NOTIMPL;

    public int HasUnhandledException(ulong p0, nint p1) => _legacy is not null ? _legacy.HasUnhandledException(p0, p1) : HResults.E_NOTIMPL;

    public int GetUserState(ulong p0, nint p1) => _legacy is not null ? _legacy.GetUserState(p0, p1) : HResults.E_NOTIMPL;

    public int GetPartialUserState(ulong p0, nint p1) => _legacy is not null ? _legacy.GetPartialUserState(p0, p1) : HResults.E_NOTIMPL;

    public int GetConnectionID(ulong p0, nint p1) => _legacy is not null ? _legacy.GetConnectionID(p0, p1) : HResults.E_NOTIMPL;

    public int GetTaskID(ulong p0, nint p1) => _legacy is not null ? _legacy.GetTaskID(p0, p1) : HResults.E_NOTIMPL;

    public int TryGetVolatileOSThreadID(ulong p0, nint p1) => _legacy is not null ? _legacy.TryGetVolatileOSThreadID(p0, p1) : HResults.E_NOTIMPL;

    public int GetUniqueThreadID(ulong p0, nint p1) => _legacy is not null ? _legacy.GetUniqueThreadID(p0, p1) : HResults.E_NOTIMPL;

    public int GetCurrentException(ulong p0, nint p1) => _legacy is not null ? _legacy.GetCurrentException(p0, p1) : HResults.E_NOTIMPL;

    public int GetObjectForCCW(ulong p0, nint p1) => _legacy is not null ? _legacy.GetObjectForCCW(p0, p1) : HResults.E_NOTIMPL;

    public int GetCurrentCustomDebuggerNotification(ulong p0, nint p1) => _legacy is not null ? _legacy.GetCurrentCustomDebuggerNotification(p0, p1) : HResults.E_NOTIMPL;

    public int GetCurrentAppDomain(nint p0) => _legacy is not null ? _legacy.GetCurrentAppDomain(p0) : HResults.E_NOTIMPL;

    public int ResolveAssembly(ulong p0, uint p1, nint p2) => _legacy is not null ? _legacy.ResolveAssembly(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetNativeCodeSequencePointsAndVarInfo(ulong p0, ulong p1, int p2, nint p3, nint p4) => _legacy is not null ? _legacy.GetNativeCodeSequencePointsAndVarInfo(p0, p1, p2, p3, p4) : HResults.E_NOTIMPL;

    public int GetManagedStoppedContext(ulong p0, nint p1) => _legacy is not null ? _legacy.GetManagedStoppedContext(p0, p1) : HResults.E_NOTIMPL;

    public int CreateStackWalk(ulong p0, nint p1, nint p2) => _legacy is not null ? _legacy.CreateStackWalk(p0, p1, p2) : HResults.E_NOTIMPL;

    public int DeleteStackWalk(ulong p0) => _legacy is not null ? _legacy.DeleteStackWalk(p0) : HResults.E_NOTIMPL;

    public int GetStackWalkCurrentContext(ulong p0, nint p1) => _legacy is not null ? _legacy.GetStackWalkCurrentContext(p0, p1) : HResults.E_NOTIMPL;

    public int SetStackWalkCurrentContext(ulong p0, ulong p1, int p2, nint p3) => _legacy is not null ? _legacy.SetStackWalkCurrentContext(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int UnwindStackWalkFrame(ulong p0, nint p1) => _legacy is not null ? _legacy.UnwindStackWalkFrame(p0, p1) : HResults.E_NOTIMPL;

    public int CheckContext(ulong p0, nint p1) => _legacy is not null ? _legacy.CheckContext(p0, p1) : HResults.E_NOTIMPL;

    public int GetStackWalkCurrentFrameInfo(ulong p0, nint p1, nint p2) => _legacy is not null ? _legacy.GetStackWalkCurrentFrameInfo(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetCountOfInternalFrames(ulong p0, nint p1) => _legacy is not null ? _legacy.GetCountOfInternalFrames(p0, p1) : HResults.E_NOTIMPL;

    public int EnumerateInternalFrames(ulong p0, nint p1, nint p2) => _legacy is not null ? _legacy.EnumerateInternalFrames(p0, p1, p2) : HResults.E_NOTIMPL;

    public int IsMatchingParentFrame(ulong p0, ulong p1, nint p2) => _legacy is not null ? _legacy.IsMatchingParentFrame(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetStackParameterSize(ulong p0, nint p1) => _legacy is not null ? _legacy.GetStackParameterSize(p0, p1) : HResults.E_NOTIMPL;

    public int GetFramePointer(ulong p0, nint p1) => _legacy is not null ? _legacy.GetFramePointer(p0, p1) : HResults.E_NOTIMPL;

    public int IsLeafFrame(ulong p0, nint p1, nint p2) => _legacy is not null ? _legacy.IsLeafFrame(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetContext(ulong p0, nint p1) => _legacy is not null ? _legacy.GetContext(p0, p1) : HResults.E_NOTIMPL;

    public int ConvertContextToDebuggerRegDisplay(nint p0, nint p1, int p2) => _legacy is not null ? _legacy.ConvertContextToDebuggerRegDisplay(p0, p1, p2) : HResults.E_NOTIMPL;

    public int IsDiagnosticsHiddenOrLCGMethod(ulong p0, nint p1) => _legacy is not null ? _legacy.IsDiagnosticsHiddenOrLCGMethod(p0, p1) : HResults.E_NOTIMPL;

    public int GetVarArgSig(ulong p0, nint p1, nint p2) => _legacy is not null ? _legacy.GetVarArgSig(p0, p1, p2) : HResults.E_NOTIMPL;

    public int RequiresAlign8(ulong p0, nint p1) => _legacy is not null ? _legacy.RequiresAlign8(p0, p1) : HResults.E_NOTIMPL;

    public int ResolveExactGenericArgsToken(uint p0, nint p1, nint p2) => _legacy is not null ? _legacy.ResolveExactGenericArgsToken(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetILCodeAndSig(ulong p0, uint p1, nint p2, nint p3) => _legacy is not null ? _legacy.GetILCodeAndSig(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetNativeCodeInfo(ulong p0, uint p1, nint p2) => _legacy is not null ? _legacy.GetNativeCodeInfo(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetNativeCodeInfoForAddr(ulong p0, nint p1, nint p2, nint p3) => _legacy is not null ? _legacy.GetNativeCodeInfoForAddr(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int IsValueType(ulong p0, nint p1) => _legacy is not null ? _legacy.IsValueType(p0, p1) : HResults.E_NOTIMPL;

    public int HasTypeParams(ulong p0, nint p1) => _legacy is not null ? _legacy.HasTypeParams(p0, p1) : HResults.E_NOTIMPL;

    public int GetClassInfo(ulong p0, ulong p1, nint p2) => _legacy is not null ? _legacy.GetClassInfo(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetInstantiationFieldInfo(ulong p0, ulong p1, ulong p2, nint p3, nint p4) => _legacy is not null ? _legacy.GetInstantiationFieldInfo(p0, p1, p2, p3, p4) : HResults.E_NOTIMPL;

    public int TypeHandleToExpandedTypeInfo(nint p0, ulong p1, ulong p2, nint p3) => _legacy is not null ? _legacy.TypeHandleToExpandedTypeInfo(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetObjectExpandedTypeInfo(nint p0, ulong p1, ulong p2, nint p3) => _legacy is not null ? _legacy.GetObjectExpandedTypeInfo(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetObjectExpandedTypeInfoFromID(nint p0, ulong p1, nint p2, nint p3) => _legacy is not null ? _legacy.GetObjectExpandedTypeInfoFromID(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetTypeHandle(ulong p0, nint p1, nint p2) => _legacy is not null ? _legacy.GetTypeHandle(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetApproxTypeHandle(nint p0, nint p1) => _legacy is not null ? _legacy.GetApproxTypeHandle(p0, p1) : HResults.E_NOTIMPL;

    public int GetExactTypeHandle(nint p0, nint p1, nint p2) => _legacy is not null ? _legacy.GetExactTypeHandle(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetMethodDescParams(ulong p0, ulong p1, nint p2, nint p3, nint p4) => _legacy is not null ? _legacy.GetMethodDescParams(p0, p1, p2, p3, p4) : HResults.E_NOTIMPL;

    public int GetThreadStaticAddress(ulong p0, ulong p1, nint p2) => _legacy is not null ? _legacy.GetThreadStaticAddress(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetCollectibleTypeStaticAddress(ulong p0, ulong p1, nint p2) => _legacy is not null ? _legacy.GetCollectibleTypeStaticAddress(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetEnCHangingFieldInfo(nint p0, nint p1, nint p2) => _legacy is not null ? _legacy.GetEnCHangingFieldInfo(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetTypeHandleParams(ulong p0, ulong p1, nint p2) => _legacy is not null ? _legacy.GetTypeHandleParams(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetSimpleType(ulong p0, int p1, nint p2, nint p3, nint p4) => _legacy is not null ? _legacy.GetSimpleType(p0, p1, p2, p3, p4) : HResults.E_NOTIMPL;

    public int IsExceptionObject(ulong p0, nint p1) => _legacy is not null ? _legacy.IsExceptionObject(p0, p1) : HResults.E_NOTIMPL;

    public int GetStackFramesFromException(ulong p0, nint p1) => _legacy is not null ? _legacy.GetStackFramesFromException(p0, p1) : HResults.E_NOTIMPL;

    public int IsRcw(ulong p0, nint p1) => _legacy is not null ? _legacy.IsRcw(p0, p1) : HResults.E_NOTIMPL;

    public int GetRcwCachedInterfaceTypes(ulong p0, ulong p1, int p2, nint p3) => _legacy is not null ? _legacy.GetRcwCachedInterfaceTypes(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetRcwCachedInterfacePointers(ulong p0, int p1, nint p2) => _legacy is not null ? _legacy.GetRcwCachedInterfacePointers(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetCachedWinRTTypesForIIDs(ulong p0, nint p1, nint p2) => _legacy is not null ? _legacy.GetCachedWinRTTypesForIIDs(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetCachedWinRTTypes(ulong p0, nint p1, nint p2) => _legacy is not null ? _legacy.GetCachedWinRTTypes(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetTypedByRefInfo(ulong p0, ulong p1, nint p2) => _legacy is not null ? _legacy.GetTypedByRefInfo(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetStringData(ulong p0, nint p1) => _legacy is not null ? _legacy.GetStringData(p0, p1) : HResults.E_NOTIMPL;

    public int GetArrayData(ulong p0, nint p1) => _legacy is not null ? _legacy.GetArrayData(p0, p1) : HResults.E_NOTIMPL;

    public int GetBasicObjectInfo(ulong p0, int p1, ulong p2, nint p3) => _legacy is not null ? _legacy.GetBasicObjectInfo(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int TestCrst(ulong p0) => _legacy is not null ? _legacy.TestCrst(p0) : HResults.E_NOTIMPL;

    public int TestRWLock(ulong p0) => _legacy is not null ? _legacy.TestRWLock(p0) : HResults.E_NOTIMPL;

    public int GetDebuggerControlBlockAddress(nint p0) => _legacy is not null ? _legacy.GetDebuggerControlBlockAddress(p0) : HResults.E_NOTIMPL;

    public int GetObjectFromRefPtr(ulong p0, nint p1) => _legacy is not null ? _legacy.GetObjectFromRefPtr(p0, p1) : HResults.E_NOTIMPL;

    public int GetObject(ulong p0, nint p1) => _legacy is not null ? _legacy.GetObject(p0, p1) : HResults.E_NOTIMPL;

    public int EnableNGENPolicy(int p0) => _legacy is not null ? _legacy.EnableNGENPolicy(p0) : HResults.E_NOTIMPL;

    public int SetNGENCompilerFlags(uint p0) => _legacy is not null ? _legacy.SetNGENCompilerFlags(p0) : HResults.E_NOTIMPL;

    public int GetNGENCompilerFlags(nint p0) => _legacy is not null ? _legacy.GetNGENCompilerFlags(p0) : HResults.E_NOTIMPL;

    public int GetVmObjectHandle(ulong p0, nint p1) => _legacy is not null ? _legacy.GetVmObjectHandle(p0, p1) : HResults.E_NOTIMPL;

    public int IsVmObjectHandleValid(ulong p0, nint p1) => _legacy is not null ? _legacy.IsVmObjectHandleValid(p0, p1) : HResults.E_NOTIMPL;

    public int IsWinRTModule(ulong p0, nint p1) => _legacy is not null ? _legacy.IsWinRTModule(p0, p1) : HResults.E_NOTIMPL;

    public int GetAppDomainIdFromVmObjectHandle(ulong p0, nint p1) => _legacy is not null ? _legacy.GetAppDomainIdFromVmObjectHandle(p0, p1) : HResults.E_NOTIMPL;

    public int GetHandleAddressFromVmHandle(ulong p0, nint p1) => _legacy is not null ? _legacy.GetHandleAddressFromVmHandle(p0, p1) : HResults.E_NOTIMPL;

    public int GetObjectContents(ulong p0, nint p1) => _legacy is not null ? _legacy.GetObjectContents(p0, p1) : HResults.E_NOTIMPL;

    public int GetThreadOwningMonitorLock(ulong p0, nint p1) => _legacy is not null ? _legacy.GetThreadOwningMonitorLock(p0, p1) : HResults.E_NOTIMPL;

    public int EnumerateMonitorEventWaitList(ulong p0, nint p1, nint p2) => _legacy is not null ? _legacy.EnumerateMonitorEventWaitList(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetAttachStateFlags(nint p0) => _legacy is not null ? _legacy.GetAttachStateFlags(p0) : HResults.E_NOTIMPL;

    public int GetMetaDataFileInfoFromPEFile(ulong p0, nint p1, nint p2, nint p3, nint p4) => _legacy is not null ? _legacy.GetMetaDataFileInfoFromPEFile(p0, p1, p2, p3, p4) : HResults.E_NOTIMPL;

    public int IsThreadSuspendedOrHijacked(ulong p0, nint p1) => _legacy is not null ? _legacy.IsThreadSuspendedOrHijacked(p0, p1) : HResults.E_NOTIMPL;

    public int AreGCStructuresValid(nint p0) => _legacy is not null ? _legacy.AreGCStructuresValid(p0) : HResults.E_NOTIMPL;

    public int CreateHeapWalk(nint p0) => _legacy is not null ? _legacy.CreateHeapWalk(p0) : HResults.E_NOTIMPL;

    public int DeleteHeapWalk(ulong p0) => _legacy is not null ? _legacy.DeleteHeapWalk(p0) : HResults.E_NOTIMPL;

    public int WalkHeap(ulong p0, uint p1, nint p2, nint p3) => _legacy is not null ? _legacy.WalkHeap(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetHeapSegments(nint p0) => _legacy is not null ? _legacy.GetHeapSegments(p0) : HResults.E_NOTIMPL;

    public int IsValidObject(ulong p0, nint p1) => _legacy is not null ? _legacy.IsValidObject(p0, p1) : HResults.E_NOTIMPL;

    public int GetAppDomainForObject(ulong p0, nint p1, nint p2, nint p3, nint p4) => _legacy is not null ? _legacy.GetAppDomainForObject(p0, p1, p2, p3, p4) : HResults.E_NOTIMPL;

    public int CreateRefWalk(nint p0, int p1, int p2, uint p3) => _legacy is not null ? _legacy.CreateRefWalk(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int DeleteRefWalk(ulong p0) => _legacy is not null ? _legacy.DeleteRefWalk(p0) : HResults.E_NOTIMPL;

    public int WalkRefs(ulong p0, uint p1, nint p2, nint p3) => _legacy is not null ? _legacy.WalkRefs(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetTypeID(ulong p0, nint p1) => _legacy is not null ? _legacy.GetTypeID(p0, p1) : HResults.E_NOTIMPL;

    public int GetTypeIDForType(ulong p0, nint p1) => _legacy is not null ? _legacy.GetTypeIDForType(p0, p1) : HResults.E_NOTIMPL;

    public int GetObjectFields(nint p0, uint p1, nint p2, nint p3) => _legacy is not null ? _legacy.GetObjectFields(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetTypeLayout(nint p0, nint p1) => _legacy is not null ? _legacy.GetTypeLayout(p0, p1) : HResults.E_NOTIMPL;

    public int GetArrayLayout(nint p0, nint p1) => _legacy is not null ? _legacy.GetArrayLayout(p0, p1) : HResults.E_NOTIMPL;

    public int GetGCHeapInformation(nint p0) => _legacy is not null ? _legacy.GetGCHeapInformation(p0) : HResults.E_NOTIMPL;

    public int GetPEFileMDInternalRW(ulong p0, nint p1) => _legacy is not null ? _legacy.GetPEFileMDInternalRW(p0, p1) : HResults.E_NOTIMPL;

    public int GetReJitInfo(ulong p0, uint p1, nint p2) => _legacy is not null ? _legacy.GetReJitInfo(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetReJitInfo(ulong p0, ulong p1, nint p2) => _legacy is not null ? _legacy.GetReJitInfo(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetSharedReJitInfo(ulong p0, nint p1) => _legacy is not null ? _legacy.GetSharedReJitInfo(p0, p1) : HResults.E_NOTIMPL;

    public int GetSharedReJitInfoData(ulong p0, nint p1) => _legacy is not null ? _legacy.GetSharedReJitInfoData(p0, p1) : HResults.E_NOTIMPL;

    public int AreOptimizationsDisabled(ulong p0, uint p1, nint p2) => _legacy is not null ? _legacy.AreOptimizationsDisabled(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetDefinesBitField(nint p0) => _legacy is not null ? _legacy.GetDefinesBitField(p0) : HResults.E_NOTIMPL;

    public int GetMDStructuresVersion(nint p0) => _legacy is not null ? _legacy.GetMDStructuresVersion(p0) : HResults.E_NOTIMPL;

    public int GetActiveRejitILCodeVersionNode(ulong p0, uint p1, nint p2) => _legacy is not null ? _legacy.GetActiveRejitILCodeVersionNode(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetNativeCodeVersionNode(ulong p0, ulong p1, nint p2) => _legacy is not null ? _legacy.GetNativeCodeVersionNode(p0, p1, p2) : HResults.E_NOTIMPL;

    public int GetILCodeVersionNode(ulong p0, nint p1) => _legacy is not null ? _legacy.GetILCodeVersionNode(p0, p1) : HResults.E_NOTIMPL;

    public int GetILCodeVersionNodeData(ulong p0, nint p1) => _legacy is not null ? _legacy.GetILCodeVersionNodeData(p0, p1) : HResults.E_NOTIMPL;

    public int EnableGCNotificationEvents(int p0) => _legacy is not null ? _legacy.EnableGCNotificationEvents(p0) : HResults.E_NOTIMPL;

    public int IsDelegate(ulong p0, nint p1) => _legacy is not null ? _legacy.IsDelegate(p0, p1) : HResults.E_NOTIMPL;

    public int GetDelegateType(ulong p0, nint p1) => _legacy is not null ? _legacy.GetDelegateType(p0, p1) : HResults.E_NOTIMPL;

    public int GetDelegateFunctionData(int p0, ulong p1, nint p2, nint p3) => _legacy is not null ? _legacy.GetDelegateFunctionData(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetDelegateTargetObject(int p0, ulong p1, nint p2, nint p3) => _legacy is not null ? _legacy.GetDelegateTargetObject(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetLoaderHeapMemoryRanges(nint p0) => _legacy is not null ? _legacy.GetLoaderHeapMemoryRanges(p0) : HResults.E_NOTIMPL;

    public int IsModuleMapped(ulong p0, nint p1) => _legacy is not null ? _legacy.IsModuleMapped(p0, p1) : HResults.E_NOTIMPL;

    public int MetadataUpdatesApplied(nint p0) => _legacy is not null ? _legacy.MetadataUpdatesApplied(p0) : HResults.E_NOTIMPL;

    public int GetDomainAssemblyFromModule(ulong p0, nint p1) => _legacy is not null ? _legacy.GetDomainAssemblyFromModule(p0, p1) : HResults.E_NOTIMPL;

    public int ParseContinuation(ulong p0, nint p1, nint p2, nint p3) => _legacy is not null ? _legacy.ParseContinuation(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetAsyncLocals(ulong p0, ulong p1, uint p2, nint p3) => _legacy is not null ? _legacy.GetAsyncLocals(p0, p1, p2, p3) : HResults.E_NOTIMPL;

    public int GetGenericArgTokenIndex(ulong p0, nint p1) => _legacy is not null ? _legacy.GetGenericArgTokenIndex(p0, p1) : HResults.E_NOTIMPL;

}
