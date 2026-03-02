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

// Prefix projection of IDacDbiInterface used to validate COM binding in managed cDAC.
// The full interface surface is intentionally staged in follow-up changes.
[ComImport]
[Guid("B7A6D3F5-6B46-4DD4-8AF1-0D4A2AFB98C1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public unsafe interface IDacDbiInterfaceControl
{
    [PreserveSig]
    int CheckDbiVersion(DbiVersion* pVersion);

    [PreserveSig]
    int FlushCache();

    [PreserveSig]
    int DacSetTargetConsistencyChecks([MarshalAs(UnmanagedType.Bool)] bool fEnableAsserts);

    [PreserveSig]
    int Destroy();

    [PreserveSig]
    int IsLeftSideInitialized(int* pResult);

    [PreserveSig]
    int GetAppDomainFromId(uint appdomainId, ulong* pRetVal);

    [PreserveSig]
    int GetAppDomainId(ulong vmAppDomain, uint* pRetVal);

    [PreserveSig]
    int GetAppDomainObject(ulong vmAppDomain, ulong* pRetVal);

    [PreserveSig]
    int GetAppDomainFullName(ulong vmAppDomain, nint pStrName);

    [PreserveSig]
    int GetCompilerFlags(ulong vmDomainAssembly, int* pfAllowJITOpts, int* pfEnableEnC);

    [PreserveSig]
    int SetCompilerFlags(ulong vmDomainAssembly, int fAllowJitOpts, int fEnableEnC);

    [PreserveSig]
    int GetNativeCodeSequencePointsAndVarInfo(ulong vmMethodDesc, ulong startAddress, int fCodeAvailable, nint pNativeVarData, nint pSequencePoints);

    [PreserveSig]
    int IsThreadSuspendedOrHijacked(ulong vmThread, byte* pResult);

    [PreserveSig]
    int AreGCStructuresValid(byte* pResult);

    [PreserveSig]
    int CreateHeapWalk(nuint* pHandle);

    [PreserveSig]
    int DeleteHeapWalk(nuint handle);

    [PreserveSig]
    int WalkHeap(nuint handle, uint count, nint objects, uint* fetched);

    [PreserveSig]
    int GetHeapSegments(nint pSegments);

    [PreserveSig]
    int IsValidObject(ulong obj, byte* pResult);

    [PreserveSig]
    int GetAppDomainForObject(ulong obj, ulong* pApp, ulong* pModule, ulong* pDomainAssembly, byte* pResult);

    [PreserveSig]
    int CreateRefWalk(nuint* pHandle, int walkStacks, int walkFQ, uint handleWalkMask);

    [PreserveSig]
    int DeleteRefWalk(nuint handle);

    [PreserveSig]
    int WalkRefs(nuint handle, uint count, nint objects, uint* pFetched);

    [PreserveSig]
    int GetTypeID(ulong obj, nint pID);

    [PreserveSig]
    int GetTypeIDForType(ulong vmTypeHandle, nint pID);

    [PreserveSig]
    int GetObjectFields(nint id, uint celt, nint layout, uint* pceltFetched);

    [PreserveSig]
    int GetTypeLayout(nint id, nint pLayout);

    [PreserveSig]
    int GetArrayLayout(nint id, nint pLayout);

    [PreserveSig]
    int GetGCHeapInformation(nint pHeapInfo);

    [PreserveSig]
    int GetPEFileMDInternalRW(ulong vmPEAssembly, ulong* pAddrMDInternalRW);

    [PreserveSig]
    int GetReJitInfo(ulong vmModule, uint methodTk, ulong* pReJitInfo);

    [PreserveSig]
    int GetReJitInfo(ulong vmMethod, ulong codeStartAddress, ulong* pReJitInfo);

    [PreserveSig]
    int AreOptimizationsDisabled(ulong vmModule, uint methodTk, int* pOptimizationsDisabled);

    [PreserveSig]
    int GetSharedReJitInfo(ulong vmReJitInfo, ulong* pSharedReJitInfo);

    [PreserveSig]
    int GetSharedReJitInfoData(ulong sharedReJitInfo, nint pData);

    [PreserveSig]
    int GetDefinesBitField(uint* pDefines);

    [PreserveSig]
    int GetMDStructuresVersion(uint* pMDStructuresVersion);

    [PreserveSig]
    int EnableGCNotificationEvents(int fEnable);

    [PreserveSig]
    int GetDomainAssemblyFromModule(ulong vmModule, ulong* pVmDomainAssembly);

    [PreserveSig]
    int ParseContinuation(ulong continuationAddress, ulong* pDiagnosticIP, ulong* pNextContinuation, uint* pState);

    [PreserveSig]
    int GetAsyncLocals(ulong vmMethod, ulong codeAddr, uint state, nint pAsyncLocals);

    [PreserveSig]
    int GetGenericArgTokenIndex(ulong vmMethod, uint* pIndex);

    [PreserveSig]
    int GetILCodeAndSig(ulong vmDomainAssembly, uint functionToken, nint pCodeInfo, uint* pLocalSigToken);

    [PreserveSig]
    int GetNativeCodeInfo(ulong vmDomainAssembly, uint functionToken, nint pCodeInfo);

    [PreserveSig]
    int GetNativeCodeInfoForAddr(ulong codeAddress, nint pCodeInfo, ulong* pVmModule, uint* pFunctionToken);

    [PreserveSig]
    int IsValueType(ulong th, int* pResult);

    [PreserveSig]
    int HasTypeParams(ulong th, int* pResult);

    [PreserveSig]
    int GetClassInfo(ulong vmAppDomain, ulong thExact, nint pData);

    [PreserveSig]
    int GetInstantiationFieldInfo(ulong vmDomainAssembly, ulong vmThExact, ulong vmThApprox, nint pFieldList, nuint* pObjectSize);

    [PreserveSig]
    int GetObjectExpandedTypeInfo(int boxed, ulong vmAppDomain, ulong addr, nint pTypeInfo);

    [PreserveSig]
    int GetObjectExpandedTypeInfoFromID(int boxed, ulong vmAppDomain, nint id, nint pTypeInfo);

    [PreserveSig]
    int TypeHandleToExpandedTypeInfo(int boxed, ulong vmAppDomain, ulong vmTypeHandle, nint pTypeInfo);

    [PreserveSig]
    int GetTypeHandle(ulong vmModule, uint metadataToken, ulong* pRetVal);

    [PreserveSig]
    int GetApproxTypeHandle(nint pTypeData, ulong* pRetVal);

    [PreserveSig]
    int GetExactTypeHandle(nint pTypeData, nint pArgInfo, ulong* vmTypeHandle);

    [PreserveSig]
    int GetMethodDescParams(ulong vmAppDomain, ulong vmMethodDesc, ulong genericsToken, uint* pcGenericClassTypeParams, nint pGenericTypeParams);

    [PreserveSig]
    int GetThreadStaticAddress(ulong vmField, ulong vmRuntimeThread, ulong* pRetVal);

    [PreserveSig]
    int GetCollectibleTypeStaticAddress(ulong vmField, ulong vmAppDomain, ulong* pRetVal);

    [PreserveSig]
    int GetEnCHangingFieldInfo(nint pEnCFieldInfo, nint pFieldData, int* pfStatic);

    [PreserveSig]
    int GetTypeHandleParams(ulong vmAppDomain, ulong vmTypeHandle, nint pParams);

    [PreserveSig]
    int GetSimpleType(ulong vmAppDomain, int simpleType, uint* pMetadataToken, ulong* pVmModule, ulong* pVmDomainAssembly);

    [PreserveSig]
    int IsExceptionObject(ulong vmObject, int* pResult);

    [PreserveSig]
    int GetStackFramesFromException(ulong vmObject, nint dacStackFrames);

    [PreserveSig]
    int IsRcw(ulong vmObject, int* pResult);

    [PreserveSig]
    int IsDelegate(ulong vmObject, int* pResult);

    [PreserveSig]
    int GetDelegateType(ulong delegateObject, int* delegateType);

    [PreserveSig]
    int GetDelegateFunctionData(int delegateType, ulong delegateObject, ulong* ppFunctionDomainAssembly, uint* pMethodDef);

    [PreserveSig]
    int GetDelegateTargetObject(int delegateType, ulong delegateObject, ulong* ppTargetObj, ulong* ppTargetAppDomain);

    [PreserveSig]
    int GetLoaderHeapMemoryRanges(nint pRanges);

    [PreserveSig]
    int IsModuleMapped(ulong pModule, int* isModuleMapped);

    [PreserveSig]
    int MetadataUpdatesApplied(byte* pResult);

    [PreserveSig]
    int GetRcwCachedInterfaceTypes(ulong vmObject, ulong vmAppDomain, int bIInspectableOnly, nint pDacInterfaces);

    [PreserveSig]
    int GetRcwCachedInterfacePointers(ulong vmObject, int bIInspectableOnly, nint pDacItfPtrs);

    [PreserveSig]
    int GetCachedWinRTTypesForIIDs(ulong vmAppDomain, nint iids, nint pTypes);

    [PreserveSig]
    int GetCachedWinRTTypes(ulong vmAppDomain, nint piids, nint pTypes);

    [PreserveSig]
    int GetAssemblyFromDomainAssembly(ulong p0, nint p1);

    [PreserveSig]
    int IsAssemblyFullyTrusted(ulong p0, nint p1);

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
    int GetActiveRejitILCodeVersionNode(ulong p0, nint p1, nint p2);

    [PreserveSig]
    int GetNativeCodeVersionNode(ulong p0, ulong p1, nint p2);

    [PreserveSig]
    int GetILCodeVersionNode(ulong p0, nint p1);

    [PreserveSig]
    int GetILCodeVersionNodeData(ulong p0, nint p1);
}
