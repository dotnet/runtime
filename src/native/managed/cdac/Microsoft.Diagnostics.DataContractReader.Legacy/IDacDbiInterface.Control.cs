// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[StructLayout(LayoutKind.Sequential)]
public struct DbiVersion
{
    public uint m_dwFormat;
    public uint m_dwDbiVersion;
    public uint m_dwProtocolBreakingChangeCounter;
    public uint m_dwReservedMustBeZero1;
}

// Name-surface projection of IDacDbiInterface in native method order for COM binding validation.
// Parameter shapes are intentionally coarse placeholders and will be refined with method implementation work.
[ComImport]
[Guid("B7A6D3F5-6B46-4DD4-8AF1-0D4A2AFB98C1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public unsafe interface IDacDbiInterfaceControl
{
    [PreserveSig]
    int CheckDbiVersion(nint p0);

    [PreserveSig]
    int FlushCache();

    [PreserveSig]
    int DacSetTargetConsistencyChecks(int p0);

    [PreserveSig]
    int Destroy();

    [PreserveSig]
    int IsLeftSideInitialized(nint p0);

    [PreserveSig]
    int GetAppDomainFromId(uint p0, nint p1);

    [PreserveSig]
    int GetAppDomainId(ulong p0, nint p1);

    [PreserveSig]
    int GetAppDomainObject(ulong p0, nint p1);

    [PreserveSig]
    int GetAssemblyFromDomainAssembly(ulong p0, nint p1);

    [PreserveSig]
    int IsAssemblyFullyTrusted(ulong p0, nint p1);

    [PreserveSig]
    int GetAppDomainFullName(ulong p0, nint p1);

    [PreserveSig]
    int GetModuleSimpleName(ulong p0, nint p1);

    [PreserveSig]
    int GetAssemblyPath(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int ResolveTypeReference(nint p0, nint p1);

    [PreserveSig]
    int GetModulePath(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int GetMetadata(ulong p0, nint p1);

    [PreserveSig]
    int GetSymbolsBuffer(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int GetModuleData(ulong p0, nint p1);

    [PreserveSig]
    int GetDomainAssemblyData(ulong p0, nint p1);

    [PreserveSig]
    int GetModuleForDomainAssembly(ulong p0, nint p1);

    [PreserveSig]
    int GetAddressType(ulong p0, nint p1);

    [PreserveSig]
    int IsTransitionStub(ulong p0, nint p1);

    [PreserveSig]
    int GetCompilerFlags(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int SetCompilerFlags(ulong p0, int p1, int p2);

    [PreserveSig]
    int EnumerateAppDomains(nint p0, nint p1);

    [PreserveSig]
    int EnumerateAssembliesInAppDomain(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int EnumerateModulesInAssembly(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int RequestSyncAtEvent();

    [PreserveSig]
    int SetSendExceptionsOutsideOfJMC(int p0);

    [PreserveSig]
    int MarkDebuggerAttachPending();

    [PreserveSig]
    int MarkDebuggerAttached(int p0);

    [PreserveSig]
    int Hijack(ulong p0, uint p1, nint p2, nint p3, uint p4, nint p5, nint p6, nint p7);

    [PreserveSig]
    int EnumerateThreads(nint p0, nint p1);

    [PreserveSig]
    int IsThreadMarkedDead(ulong p0, nint p1);

    [PreserveSig]
    int GetThreadHandle(ulong p0, nint p1);

    [PreserveSig]
    int GetThreadObject(ulong p0, nint p1);

    [PreserveSig]
    int GetThreadAllocInfo(ulong p0, nint p1);

    [PreserveSig]
    int SetDebugState(ulong p0, int p1);

    [PreserveSig]
    int HasUnhandledException(ulong p0, nint p1);

    [PreserveSig]
    int GetUserState(ulong p0, nint p1);

    [PreserveSig]
    int GetPartialUserState(ulong p0, nint p1);

    [PreserveSig]
    int GetConnectionID(ulong p0, nint p1);

    [PreserveSig]
    int GetTaskID(ulong p0, nint p1);

    [PreserveSig]
    int TryGetVolatileOSThreadID(ulong p0, nint p1);

    [PreserveSig]
    int GetUniqueThreadID(ulong p0, nint p1);

    [PreserveSig]
    int GetCurrentException(ulong p0, nint p1);

    [PreserveSig]
    int GetObjectForCCW(ulong p0, nint p1);

    [PreserveSig]
    int GetCurrentCustomDebuggerNotification(ulong p0, nint p1);

    [PreserveSig]
    int GetCurrentAppDomain(nint p0);

    [PreserveSig]
    int ResolveAssembly(ulong p0, uint p1, nint p2);

    [PreserveSig]
    int GetNativeCodeSequencePointsAndVarInfo(ulong p0, ulong p1, int p2, nint p3, nint p4);

    [PreserveSig]
    int GetManagedStoppedContext(ulong p0, nint p1);

    [PreserveSig]
    int CreateStackWalk(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int DeleteStackWalk(ulong p0);

    [PreserveSig]
    int GetStackWalkCurrentContext(ulong p0, nint p1);

    [PreserveSig]
    int SetStackWalkCurrentContext(ulong p0, ulong p1, int p2, nint p3);

    [PreserveSig]
    int UnwindStackWalkFrame(ulong p0, nint p1);

    [PreserveSig]
    int CheckContext(ulong p0, nint p1);

    [PreserveSig]
    int GetStackWalkCurrentFrameInfo(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int GetCountOfInternalFrames(ulong p0, nint p1);

    [PreserveSig]
    int EnumerateInternalFrames(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int IsMatchingParentFrame(ulong p0, ulong p1, nint p2);

    [PreserveSig]
    int GetStackParameterSize(ulong p0, nint p1);

    [PreserveSig]
    int GetFramePointer(ulong p0, nint p1);

    [PreserveSig]
    int IsLeafFrame(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int GetContext(ulong p0, nint p1);

    [PreserveSig]
    int ConvertContextToDebuggerRegDisplay(nint p0, nint p1, int p2);

    [PreserveSig]
    int IsDiagnosticsHiddenOrLCGMethod(ulong p0, nint p1);

    [PreserveSig]
    int GetVarArgSig(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int RequiresAlign8(ulong p0, nint p1);

    [PreserveSig]
    int ResolveExactGenericArgsToken(uint p0, nint p1, nint p2);

    [PreserveSig]
    int GetILCodeAndSig(ulong p0, uint p1, nint p2, nint p3);

    [PreserveSig]
    int GetNativeCodeInfo(ulong p0, uint p1, nint p2);

    [PreserveSig]
    int GetNativeCodeInfoForAddr(ulong p0, nint p1, nint p2, nint p3);

    [PreserveSig]
    int IsValueType(ulong p0, nint p1);

    [PreserveSig]
    int HasTypeParams(ulong p0, nint p1);

    [PreserveSig]
    int GetClassInfo(ulong p0, ulong p1, nint p2);

    [PreserveSig]
    int GetInstantiationFieldInfo(ulong p0, ulong p1, ulong p2, nint p3, nint p4);

    [PreserveSig]
    int TypeHandleToExpandedTypeInfo(nint p0, ulong p1, ulong p2, nint p3);

    [PreserveSig]
    int GetObjectExpandedTypeInfo(nint p0, ulong p1, ulong p2, nint p3);

    [PreserveSig]
    int GetObjectExpandedTypeInfoFromID(nint p0, ulong p1, nint p2, nint p3);

    [PreserveSig]
    int GetTypeHandle(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int GetApproxTypeHandle(nint p0, nint p1);

    [PreserveSig]
    int GetExactTypeHandle(nint p0, nint p1, nint p2);

    [PreserveSig]
    int GetMethodDescParams(ulong p0, ulong p1, nint p2, nint p3, nint p4);

    [PreserveSig]
    int GetThreadStaticAddress(ulong p0, ulong p1, nint p2);

    [PreserveSig]
    int GetCollectibleTypeStaticAddress(ulong p0, ulong p1, nint p2);

    [PreserveSig]
    int GetEnCHangingFieldInfo(nint p0, nint p1, nint p2);

    [PreserveSig]
    int GetTypeHandleParams(ulong p0, ulong p1, nint p2);

    [PreserveSig]
    int GetSimpleType(ulong p0, int p1, nint p2, nint p3, nint p4);

    [PreserveSig]
    int IsExceptionObject(ulong p0, nint p1);

    [PreserveSig]
    int GetStackFramesFromException(ulong p0, nint p1);

    [PreserveSig]
    int IsRcw(ulong p0, nint p1);

    [PreserveSig]
    int GetRcwCachedInterfaceTypes(ulong p0, ulong p1, int p2, nint p3);

    [PreserveSig]
    int GetRcwCachedInterfacePointers(ulong p0, int p1, nint p2);

    [PreserveSig]
    int GetCachedWinRTTypesForIIDs(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int GetCachedWinRTTypes(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int GetTypedByRefInfo(ulong p0, ulong p1, nint p2);

    [PreserveSig]
    int GetStringData(ulong p0, nint p1);

    [PreserveSig]
    int GetArrayData(ulong p0, nint p1);

    [PreserveSig]
    int GetBasicObjectInfo(ulong p0, int p1, ulong p2, nint p3);

    [PreserveSig]
    int TestCrst(ulong p0);

    [PreserveSig]
    int TestRWLock(ulong p0);

    [PreserveSig]
    int GetDebuggerControlBlockAddress(nint p0);

    [PreserveSig]
    int GetObjectFromRefPtr(ulong p0, nint p1);

    [PreserveSig]
    int GetObject(ulong p0, nint p1);

    [PreserveSig]
    int EnableNGENPolicy(int p0);

    [PreserveSig]
    int SetNGENCompilerFlags(uint p0);

    [PreserveSig]
    int GetNGENCompilerFlags(nint p0);

    [PreserveSig]
    int GetVmObjectHandle(ulong p0, nint p1);

    [PreserveSig]
    int IsVmObjectHandleValid(ulong p0, nint p1);

    [PreserveSig]
    int IsWinRTModule(ulong p0, nint p1);

    [PreserveSig]
    int GetAppDomainIdFromVmObjectHandle(ulong p0, nint p1);

    [PreserveSig]
    int GetHandleAddressFromVmHandle(ulong p0, nint p1);

    [PreserveSig]
    int GetObjectContents(ulong p0, nint p1);

    [PreserveSig]
    int GetThreadOwningMonitorLock(ulong p0, nint p1);

    [PreserveSig]
    int EnumerateMonitorEventWaitList(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int GetAttachStateFlags(nint p0);

    [PreserveSig]
    int GetMetaDataFileInfoFromPEFile(ulong p0, nint p1, nint p2, nint p3, nint p4);

    [PreserveSig]
    int IsThreadSuspendedOrHijacked(ulong p0, nint p1);

    [PreserveSig]
    int AreGCStructuresValid(nint p0);

    [PreserveSig]
    int CreateHeapWalk(nint p0);

    [PreserveSig]
    int DeleteHeapWalk(ulong p0);

    [PreserveSig]
    int WalkHeap(ulong p0, uint p1, nint p2, nint p3);

    [PreserveSig]
    int GetHeapSegments(nint p0);

    [PreserveSig]
    int IsValidObject(ulong p0, nint p1);

    [PreserveSig]
    int GetAppDomainForObject(ulong p0, nint p1, nint p2, nint p3, nint p4);

    [PreserveSig]
    int CreateRefWalk(nint p0, int p1, int p2, uint p3);

    [PreserveSig]
    int DeleteRefWalk(ulong p0);

    [PreserveSig]
    int WalkRefs(ulong p0, uint p1, nint p2, nint p3);

    [PreserveSig]
    int GetTypeID(ulong p0, nint p1);

    [PreserveSig]
    int GetTypeIDForType(ulong p0, nint p1);

    [PreserveSig]
    int GetObjectFields(nint p0, uint p1, nint p2, nint p3);

    [PreserveSig]
    int GetTypeLayout(nint p0, nint p1);

    [PreserveSig]
    int GetArrayLayout(nint p0, nint p1);

    [PreserveSig]
    int GetGCHeapInformation(nint p0);

    [PreserveSig]
    int GetPEFileMDInternalRW(ulong p0, nint p1);

    [PreserveSig]
    int GetReJitInfo(ulong p0, uint p1, nint p2);

    [PreserveSig]
    int GetReJitInfo(ulong p0, ulong p1, nint p2);

    [PreserveSig]
    int GetSharedReJitInfo(ulong p0, nint p1);

    [PreserveSig]
    int GetSharedReJitInfoData(ulong p0, nint p1);

    [PreserveSig]
    int AreOptimizationsDisabled(ulong p0, uint p1, nint p2);

    [PreserveSig]
    int GetDefinesBitField(nint p0);

    [PreserveSig]
    int GetMDStructuresVersion(nint p0);

    [PreserveSig]
    int GetActiveRejitILCodeVersionNode(ulong p0, uint p1, nint p2);

    [PreserveSig]
    int GetNativeCodeVersionNode(ulong p0, ulong p1, nint p2);

    [PreserveSig]
    int GetILCodeVersionNode(ulong p0, nint p1);

    [PreserveSig]
    int GetILCodeVersionNodeData(ulong p0, nint p1);

    [PreserveSig]
    int EnableGCNotificationEvents(int p0);

    [PreserveSig]
    int IsDelegate(ulong p0, nint p1);

    [PreserveSig]
    int GetDelegateType(ulong p0, nint p1);

    [PreserveSig]
    int GetDelegateFunctionData(int p0, ulong p1, nint p2, nint p3);

    [PreserveSig]
    int GetDelegateTargetObject(int p0, ulong p1, nint p2, nint p3);

    [PreserveSig]
    int GetLoaderHeapMemoryRanges(nint p0);

    [PreserveSig]
    int IsModuleMapped(ulong p0, nint p1);

    [PreserveSig]
    int MetadataUpdatesApplied(nint p0);

    [PreserveSig]
    int GetDomainAssemblyFromModule(ulong p0, nint p1);

    [PreserveSig]
    int ParseContinuation(ulong p0, nint p1, nint p2, nint p3);

    [PreserveSig]
    int GetAsyncLocals(ulong p0, ulong p1, uint p2, nint p3);

    [PreserveSig]
    int GetGenericArgTokenIndex(ulong p0, nint p1);

}
