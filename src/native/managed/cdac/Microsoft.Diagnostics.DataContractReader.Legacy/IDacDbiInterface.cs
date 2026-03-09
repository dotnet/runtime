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
public unsafe interface IDacDbiInterface
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
    int GetAssemblyFromDomainAssembly(ulong vmDomainAssembly, ulong* vmAssembly);

    [PreserveSig]
    int IsAssemblyFullyTrusted(ulong vmDomainAssembly, int* pResult);

    [PreserveSig]
    int GetAppDomainFullName(ulong vmAppDomain, IStringHolder pStrName);

    [PreserveSig]
    int GetModuleSimpleName(ulong vmModule, IStringHolder pStrFilename);

    [PreserveSig]
    int GetAssemblyPath(ulong vmAssembly, IStringHolder pStrFilename, int* pResult);

    [PreserveSig]
    int ResolveTypeReference(nint pTypeRefInfo, nint pTargetRefInfo);

    [PreserveSig]
    int GetModulePath(ulong vmModule, IStringHolder pStrFilename, int* pResult);

    [PreserveSig]
    int GetMetadata(ulong vmModule, nint pTargetBuffer);

    [PreserveSig]
    int GetSymbolsBuffer(ulong vmModule, nint pTargetBuffer, int* pSymbolFormat);

    [PreserveSig]
    int GetModuleData(ulong vmModule, nint pData);

    [PreserveSig]
    int GetDomainAssemblyData(ulong vmDomainAssembly, nint pData);

    [PreserveSig]
    int GetModuleForDomainAssembly(ulong vmDomainAssembly, ulong* pModule);

    [PreserveSig]
    int GetAddressType(ulong address, int* pRetVal);

    [PreserveSig]
    int IsTransitionStub(ulong address, int* pResult);

    [PreserveSig]
    int GetCompilerFlags(ulong vmDomainAssembly, int* pfAllowJITOpts, int* pfEnableEnC);

    [PreserveSig]
    int SetCompilerFlags(ulong vmDomainAssembly, int fAllowJitOpts, int fEnableEnC);

    [PreserveSig]
    int EnumerateAppDomains(nint fpCallback, nint pUserData);

    [PreserveSig]
    int EnumerateAssembliesInAppDomain(ulong vmAppDomain, nint fpCallback, nint pUserData);

    [PreserveSig]
    int EnumerateModulesInAssembly(ulong vmAssembly, nint fpCallback, nint pUserData);

    [PreserveSig]
    int RequestSyncAtEvent();

    [PreserveSig]
    int SetSendExceptionsOutsideOfJMC(int sendExceptionsOutsideOfJMC);

    [PreserveSig]
    int MarkDebuggerAttachPending();

    [PreserveSig]
    int MarkDebuggerAttached(int fAttached);

    [PreserveSig]
    int Hijack(ulong vmThread, uint dwThreadId, nint pRecord, nint pOriginalContext, uint cbSizeContext, int reason, nint pUserData, ulong* pRemoteContextAddr);

    [PreserveSig]
    int EnumerateThreads(nint fpCallback, nint pUserData);

    [PreserveSig]
    int IsThreadMarkedDead(ulong vmThread, byte* pResult);

    [PreserveSig]
    int GetThreadHandle(ulong vmThread, nint pRetVal);

    [PreserveSig]
    int GetThreadObject(ulong vmThread, ulong* pRetVal);

    [PreserveSig]
    int GetThreadAllocInfo(ulong vmThread, nint pThreadAllocInfo);

    [PreserveSig]
    int SetDebugState(ulong vmThread, int debugState);

    [PreserveSig]
    int HasUnhandledException(ulong vmThread, int* pResult);

    [PreserveSig]
    int GetUserState(ulong vmThread, int* pRetVal);

    [PreserveSig]
    int GetPartialUserState(ulong vmThread, int* pRetVal);

    [PreserveSig]
    int GetConnectionID(ulong vmThread, uint* pRetVal);

    [PreserveSig]
    int GetTaskID(ulong vmThread, ulong* pRetVal);

    [PreserveSig]
    int TryGetVolatileOSThreadID(ulong vmThread, uint* pRetVal);

    [PreserveSig]
    int GetUniqueThreadID(ulong vmThread, uint* pRetVal);

    [PreserveSig]
    int GetCurrentException(ulong vmThread, ulong* pRetVal);

    [PreserveSig]
    int GetObjectForCCW(ulong ccwPtr, ulong* pRetVal);

    [PreserveSig]
    int GetCurrentCustomDebuggerNotification(ulong vmThread, ulong* pRetVal);

    [PreserveSig]
    int GetCurrentAppDomain(ulong* pRetVal);

    [PreserveSig]
    int ResolveAssembly(ulong vmScope, uint tkAssemblyRef, ulong* pRetVal);

    [PreserveSig]
    int GetNativeCodeSequencePointsAndVarInfo(ulong vmMethodDesc, ulong startAddress, int fCodeAvailable, nint pNativeVarData, nint pSequencePoints);

    [PreserveSig]
    int GetManagedStoppedContext(ulong vmThread, ulong* pRetVal);

    [PreserveSig]
    int CreateStackWalk(ulong vmThread, nint pInternalContextBuffer, nint ppSFIHandle);

    [PreserveSig]
    int DeleteStackWalk(ulong ppSFIHandle);

    [PreserveSig]
    int GetStackWalkCurrentContext(ulong pSFIHandle, nint pContext);

    [PreserveSig]
    int SetStackWalkCurrentContext(ulong vmThread, ulong pSFIHandle, int flag, nint pContext);

    [PreserveSig]
    int UnwindStackWalkFrame(ulong pSFIHandle, int* pResult);

    [PreserveSig]
    int CheckContext(ulong vmThread, nint pContext);

    [PreserveSig]
    int GetStackWalkCurrentFrameInfo(ulong pSFIHandle, nint pFrameData, int* pRetVal);

    [PreserveSig]
    int GetCountOfInternalFrames(ulong vmThread, uint* pRetVal);

    [PreserveSig]
    int EnumerateInternalFrames(ulong vmThread, nint fpCallback, nint pUserData);

    [PreserveSig]
    int IsMatchingParentFrame(ulong fpToCheck, ulong fpParent, int* pResult);

    [PreserveSig]
    int GetStackParameterSize(ulong controlPC, uint* pRetVal);

    [PreserveSig]
    int GetFramePointer(ulong pSFIHandle, ulong* pRetVal);

    [PreserveSig]
    int IsLeafFrame(ulong vmThread, nint pContext, int* pResult);

    [PreserveSig]
    int GetContext(ulong vmThread, nint pContextBuffer);

    [PreserveSig]
    int ConvertContextToDebuggerRegDisplay(nint pInContext, nint pOutDRD, int fActive);

    [PreserveSig]
    int IsDiagnosticsHiddenOrLCGMethod(ulong vmMethodDesc, int* pRetVal);

    [PreserveSig]
    int GetVarArgSig(ulong VASigCookieAddr, ulong* pArgBase, nint pRetVal);

    [PreserveSig]
    int RequiresAlign8(ulong thExact, int* pResult);

    [PreserveSig]
    int ResolveExactGenericArgsToken(uint dwExactGenericArgsTokenIndex, ulong rawToken, ulong* pRetVal);

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
    int TypeHandleToExpandedTypeInfo(int boxed, ulong vmAppDomain, ulong vmTypeHandle, nint pTypeInfo);

    [PreserveSig]
    int GetObjectExpandedTypeInfo(int boxed, ulong vmAppDomain, ulong addr, nint pTypeInfo);

    [PreserveSig]
    int GetObjectExpandedTypeInfoFromID(int boxed, ulong vmAppDomain, nint id, nint pTypeInfo);

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
    int GetRcwCachedInterfaceTypes(ulong vmObject, ulong vmAppDomain, int bIInspectableOnly, nint pDacInterfaces);

    [PreserveSig]
    int GetRcwCachedInterfacePointers(ulong vmObject, int bIInspectableOnly, nint pDacItfPtrs);

    [PreserveSig]
    int GetCachedWinRTTypesForIIDs(ulong vmAppDomain, nint iids, nint pTypes);

    [PreserveSig]
    int GetCachedWinRTTypes(ulong vmAppDomain, nint piids, nint pTypes);

    [PreserveSig]
    int GetTypedByRefInfo(ulong pTypedByRef, ulong vmAppDomain, nint pObjectData);

    [PreserveSig]
    int GetStringData(ulong objectAddress, nint pObjectData);

    [PreserveSig]
    int GetArrayData(ulong objectAddress, nint pObjectData);

    [PreserveSig]
    int GetBasicObjectInfo(ulong objectAddress, int type, ulong vmAppDomain, nint pObjectData);

    [PreserveSig]
    int TestCrst(ulong vmCrst);

    [PreserveSig]
    int TestRWLock(ulong vmRWLock);

    [PreserveSig]
    int GetDebuggerControlBlockAddress(ulong* pRetVal);

    [PreserveSig]
    int GetObjectFromRefPtr(ulong ptr, ulong* pRetVal);

    [PreserveSig]
    int GetObject(ulong ptr, ulong* pRetVal);

    [PreserveSig]
    int EnableNGENPolicy(int ePolicy);

    [PreserveSig]
    int SetNGENCompilerFlags(uint dwFlags);

    [PreserveSig]
    int GetNGENCompilerFlags(uint* pdwFlags);

    [PreserveSig]
    int GetVmObjectHandle(ulong handleAddress, ulong* pRetVal);

    [PreserveSig]
    int IsVmObjectHandleValid(ulong vmHandle, int* pResult);

    [PreserveSig]
    int IsWinRTModule(ulong vmModule, int* isWinRT);

    [PreserveSig]
    int GetAppDomainIdFromVmObjectHandle(ulong vmHandle, uint* pRetVal);

    [PreserveSig]
    int GetHandleAddressFromVmHandle(ulong vmHandle, ulong* pRetVal);

    [PreserveSig]
    int GetObjectContents(ulong obj, nint pRetVal);

    [PreserveSig]
    int GetThreadOwningMonitorLock(ulong vmObject, nint pRetVal);

    [PreserveSig]
    int EnumerateMonitorEventWaitList(ulong vmObject, nint fpCallback, nint pUserData);

    [PreserveSig]
    int GetAttachStateFlags(int* pRetVal);

    [PreserveSig]
    int GetMetaDataFileInfoFromPEFile(ulong vmPEAssembly, uint* dwTimeStamp, uint* dwImageSize, IStringHolder pStrFilename, byte* pResult);

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
    int WalkRefs(nuint handle, uint count, nint refs, uint* pFetched);

    [PreserveSig]
    int GetTypeID(ulong obj, nint pType);

    [PreserveSig]
    int GetTypeIDForType(ulong vmTypeHandle, nint pId);

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
    int GetSharedReJitInfo(ulong vmReJitInfo, ulong* pSharedReJitInfo);

    [PreserveSig]
    int GetSharedReJitInfoData(ulong sharedReJitInfo, nint pData);

    [PreserveSig]
    int AreOptimizationsDisabled(ulong vmModule, uint methodTk, int* pOptimizationsDisabled);

    [PreserveSig]
    int GetDefinesBitField(uint* pDefines);

    [PreserveSig]
    int GetMDStructuresVersion(uint* pMDStructuresVersion);

    [PreserveSig]
    int GetActiveRejitILCodeVersionNode(ulong vmModule, uint methodTk, ulong* pVmILCodeVersionNode);

    [PreserveSig]
    int GetNativeCodeVersionNode(ulong vmMethod, ulong codeStartAddress, ulong* pVmNativeCodeVersionNode);

    [PreserveSig]
    int GetILCodeVersionNode(ulong vmNativeCodeVersionNode, ulong* pVmILCodeVersionNode);

    [PreserveSig]
    int GetILCodeVersionNodeData(ulong ilCodeVersionNode, nint pData);

    [PreserveSig]
    int EnableGCNotificationEvents(int fEnable);

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
    int GetDomainAssemblyFromModule(ulong vmModule, ulong* pVmDomainAssembly);

    [PreserveSig]
    int ParseContinuation(ulong continuationAddress, ulong* pDiagnosticIP, ulong* pNextContinuation, uint* pState);

    [PreserveSig]
    int GetAsyncLocals(ulong vmMethod, ulong codeAddr, uint state, nint pAsyncLocals);

    [PreserveSig]
    int GetGenericArgTokenIndex(ulong vmMethod, uint* pIndex);

}
