// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[StructLayout(LayoutKind.Sequential)]
public struct DbiVersion
{
    public uint m_dwFormat;
    public uint m_dwDbiVersion;
    public uint m_dwProtocolBreakingChangeCounter;
    public uint m_dwReservedMustBeZero1;
}

[StructLayout(LayoutKind.Sequential)]
public struct COR_TYPEID
{
    public ulong token1;
    public ulong token2;
}

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

[StructLayout(LayoutKind.Sequential)]
public struct DacDbiTargetBuffer
{
    public ulong pAddress;
    public uint cbSize;
}

[StructLayout(LayoutKind.Sequential)]
public struct DacDbiAssemblyInfo
{
    public ulong vmAppDomain;
    public ulong vmAssembly;
}

[StructLayout(LayoutKind.Sequential)]
public struct DacDbiModuleInfo
{
    public ulong vmAssembly;
    public ulong pPEBaseAddress;
    public ulong vmPEAssembly;
    public uint nPESize;
    public Interop.BOOL fIsDynamic;
    public Interop.BOOL fInMemory;
}

[StructLayout(LayoutKind.Sequential)]
public struct DacDbiMonitorLockInfo
{
    public ulong lockOwner;
    public uint acquisitionCount;
}

[StructLayout(LayoutKind.Sequential)]
public struct DacDbiThreadAllocInfo
{
    public ulong allocBytesSOH;
    public ulong allocBytesUOH;
}

[StructLayout(LayoutKind.Sequential)]
public struct DacDbiTypeRefData
{
    public ulong vmAssembly;
    public uint typeToken;
}

[StructLayout(LayoutKind.Sequential)]
public struct DacDbiSharedReJitInfo
{
    public uint state;
    public ulong pbIL;
    public uint dwCodegenFlags;
    public uint cInstrumentedMapEntries;
    public ulong rgInstrumentedMapEntries;
}

[StructLayout(LayoutKind.Sequential)]
public struct DacDbiExceptionCallStackData
{
    public ulong vmAppDomain;
    public ulong vmAssembly;
    public ulong ip;
    public uint methodDef;
    public Interop.BOOL isLastForeignExceptionFrame;
}

[StructLayout(LayoutKind.Sequential)]
public struct COR_HEAPINFO
{
    public Interop.BOOL areGCStructuresValid;
    public uint pointerSize;
    public uint numHeaps;
    public Interop.BOOL concurrent;
    public int gcType;
}

[StructLayout(LayoutKind.Sequential)]
public struct COR_HEAPOBJECT
{
    public ulong address;
    public ulong size;
    public COR_TYPEID type;
}

[StructLayout(LayoutKind.Sequential)]
public struct COR_SEGMENT
{
    public ulong start;
    public ulong end;
    public int type;
    public uint heap;
}

[StructLayout(LayoutKind.Sequential)]
public struct COR_TYPE_LAYOUT
{
    public COR_TYPEID parentID;
    public uint objectSize;
    public uint numFields;
    public uint boxOffset;
    public int type;
}

[StructLayout(LayoutKind.Sequential)]
public struct COR_FIELD
{
    public uint token;
    public uint offset;
    public COR_TYPEID id;
    public int fieldType;
}

#pragma warning restore CS0649

// Name-surface projection of IDacDbiInterface in native method order for COM binding validation.
// Parameter shapes are intentionally coarse placeholders and will be refined with method implementation work.
[GeneratedComInterface]
[Guid("DB505C1B-A327-4A46-8C32-AF55A56F8E09")]
public unsafe partial interface IDacDbiInterface
{
    [PreserveSig]
    int CheckDbiVersion(DbiVersion* pVersion);

    [PreserveSig]
    int FlushCache();

    [PreserveSig]
    int DacSetTargetConsistencyChecks(Interop.BOOL fEnableAsserts);

    [PreserveSig]
    int IsLeftSideInitialized(Interop.BOOL* pResult);

    [PreserveSig]
    int GetAppDomainId(ulong vmAppDomain, uint* pRetVal);

    [PreserveSig]
    int GetAppDomainObject(ulong vmAppDomain, ulong* pRetVal);

    [PreserveSig]
    int GetAppDomainFullName(ulong vmAppDomain, nint pStrName);

    [PreserveSig]
    int GetModuleSimpleName(ulong vmModule, nint pStrFilename);

    [PreserveSig]
    int GetAssemblyPath(ulong vmAssembly, nint pStrFilename, Interop.BOOL* pResult);

    [PreserveSig]
    int ResolveTypeReference(DacDbiTypeRefData* pTypeRefInfo, DacDbiTypeRefData* pTargetRefInfo);

    [PreserveSig]
    int GetModulePath(ulong vmModule, nint pStrFilename, Interop.BOOL* pResult);

    [PreserveSig]
    int GetMetadata(ulong vmModule, DacDbiTargetBuffer* pTargetBuffer);

    [PreserveSig]
    int GetSymbolsBuffer(ulong vmModule, DacDbiTargetBuffer* pTargetBuffer, int* pSymbolFormat);

    [PreserveSig]
    int GetModuleData(ulong vmModule, DacDbiModuleInfo* pData);

    [PreserveSig]
    int GetAssemblyInfo(ulong vmAssembly, DacDbiAssemblyInfo* pData);

    [PreserveSig]
    int GetModuleForAssembly(ulong vmAssembly, ulong* pModule);

    [PreserveSig]
    int GetAddressType(ulong address, int* pRetVal);

    [PreserveSig]
    int GetCompilerFlags(ulong vmAssembly, Interop.BOOL* pfAllowJITOpts, Interop.BOOL* pfEnableEnC);

    [PreserveSig]
    int SetCompilerFlags(ulong vmAssembly, Interop.BOOL fAllowJitOpts, Interop.BOOL fEnableEnC);

    [PreserveSig]
    int EnumerateAssembliesInAppDomain(ulong vmAppDomain, nint fpCallback, nint pUserData);

    [PreserveSig]
    int EnumerateModulesInAssembly(ulong vmAssembly, nint fpCallback, nint pUserData);

    [PreserveSig]
    int RequestSyncAtEvent();

    [PreserveSig]
    int SetSendExceptionsOutsideOfJMC(Interop.BOOL sendExceptionsOutsideOfJMC);

    [PreserveSig]
    int MarkDebuggerAttachPending();

    [PreserveSig]
    int MarkDebuggerAttached(Interop.BOOL fAttached);

    [PreserveSig]
    int Hijack(ulong vmThread, uint dwThreadId, nint pRecord, nint pOriginalContext, uint cbSizeContext, int reason, nint pUserData, ulong* pRemoteContextAddr);

    [PreserveSig]
    int EnumerateThreads(nint fpCallback, nint pUserData);

    [PreserveSig]
    int IsThreadMarkedDead(ulong vmThread, Interop.BOOL* pResult);

    [PreserveSig]
    int GetThreadHandle(ulong vmThread, nint pRetVal);

    [PreserveSig]
    int GetThreadObject(ulong vmThread, ulong* pRetVal);

    [PreserveSig]
    int GetThreadAllocInfo(ulong vmThread, DacDbiThreadAllocInfo* pThreadAllocInfo);

    [PreserveSig]
    int SetDebugState(ulong vmThread, int debugState);

    [PreserveSig]
    int HasUnhandledException(ulong vmThread, Interop.BOOL* pResult);

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
    int GetNativeCodeSequencePointsAndVarInfo(ulong vmMethodDesc, ulong startAddress, Interop.BOOL fCodeAvailable, nint pNativeVarData, nint pSequencePoints);

    [PreserveSig]
    int GetManagedStoppedContext(ulong vmThread, ulong* pRetVal);

    [PreserveSig]
    int CreateStackWalk(ulong vmThread, nint pInternalContextBuffer, nuint* ppSFIHandle);

    [PreserveSig]
    int DeleteStackWalk(nuint ppSFIHandle);

    [PreserveSig]
    int GetStackWalkCurrentContext(nuint pSFIHandle, nint pContext);

    [PreserveSig]
    int SetStackWalkCurrentContext(ulong vmThread, nuint pSFIHandle, int flag, nint pContext);

    [PreserveSig]
    int UnwindStackWalkFrame(nuint pSFIHandle, Interop.BOOL* pResult);

    [PreserveSig]
    int CheckContext(ulong vmThread, nint pContext);

    [PreserveSig]
    int GetStackWalkCurrentFrameInfo(nuint pSFIHandle, nint pFrameData, int* pRetVal);

    [PreserveSig]
    int GetCountOfInternalFrames(ulong vmThread, uint* pRetVal);

    [PreserveSig]
    int EnumerateInternalFrames(ulong vmThread, nint fpCallback, nint pUserData);

    [PreserveSig]
    int GetStackParameterSize(ulong controlPC, uint* pRetVal);

    [PreserveSig]
    int GetFramePointer(nuint pSFIHandle, ulong* pRetVal);

    [PreserveSig]
    int IsLeafFrame(ulong vmThread, nint pContext, Interop.BOOL* pResult);

    [PreserveSig]
    int GetContext(ulong vmThread, nint pContextBuffer);

    [PreserveSig]
    int ConvertContextToDebuggerRegDisplay(nint pInContext, nint pOutDRD, Interop.BOOL fActive);

    [PreserveSig]
    int IsDiagnosticsHiddenOrLCGMethod(ulong vmMethodDesc, int* pRetVal);

    [PreserveSig]
    int GetVarArgSig(ulong VASigCookieAddr, ulong* pArgBase, DacDbiTargetBuffer* pRetVal);

    [PreserveSig]
    int RequiresAlign8(ulong thExact, Interop.BOOL* pResult);

    [PreserveSig]
    int ResolveExactGenericArgsToken(uint dwExactGenericArgsTokenIndex, ulong rawToken, ulong* pRetVal);

    [PreserveSig]
    int GetILCodeAndSig(ulong vmAssembly, uint functionToken, DacDbiTargetBuffer* pTargetBuffer, uint* pLocalSigToken);

    [PreserveSig]
    int GetNativeCodeInfo(ulong vmAssembly, uint functionToken, nint pJitManagerList);

    [PreserveSig]
    int GetNativeCodeInfoForAddr(ulong codeAddress, nint pCodeInfo, ulong* pVmModule, uint* pFunctionToken);

    [PreserveSig]
    int IsValueType(ulong vmTypeHandle, Interop.BOOL* pResult);

    [PreserveSig]
    int HasTypeParams(ulong vmTypeHandle, Interop.BOOL* pResult);

    [PreserveSig]
    int GetClassInfo(ulong thExact, nint pData);

    [PreserveSig]
    int GetInstantiationFieldInfo(ulong vmAssembly, ulong vmTypeHandle, ulong vmExactMethodTable, nint pFieldList, nuint* pObjectSize);

    [PreserveSig]
    int TypeHandleToExpandedTypeInfo(int boxed, ulong vmTypeHandle, nint pData);

    [PreserveSig]
    int GetObjectExpandedTypeInfo(int boxed, ulong addr, nint pTypeInfo);

    [PreserveSig]
    int GetObjectExpandedTypeInfoFromID(int boxed, COR_TYPEID id, nint pTypeInfo);

    [PreserveSig]
    int GetTypeHandle(ulong vmModule, uint metadataToken, ulong* pRetVal);

    [PreserveSig]
    int GetApproxTypeHandle(nint pTypeData, ulong* pRetVal);

    [PreserveSig]
    int GetExactTypeHandle(nint pTypeData, nint pArgInfo, ulong* pVmTypeHandle);

    [PreserveSig]
    int GetMethodDescParams(ulong vmMethodDesc, ulong genericsToken, uint* pcGenericClassTypeParams, nint pGenericTypeParams);

    [PreserveSig]
    int GetThreadStaticAddress(ulong vmField, ulong vmRuntimeThread, ulong* pRetVal);

    [PreserveSig]
    int GetCollectibleTypeStaticAddress(ulong vmField, ulong* pRetVal);

    [PreserveSig]
    int GetEnCHangingFieldInfo(nint pEnCFieldInfo, nint pFieldData, Interop.BOOL* pfStatic);

    [PreserveSig]
    int GetTypeHandleParams(ulong vmTypeHandle, nint pParams);

    [PreserveSig]
    int GetSimpleType(int simpleType, uint* pMetadataToken, ulong* pVmModule);

    [PreserveSig]
    int IsExceptionObject(ulong vmObject, Interop.BOOL* pResult);

    [PreserveSig]
    int GetStackFramesFromException(ulong vmObject, nint pDacStackFrames);

    [PreserveSig]
    int IsRcw(ulong vmObject, Interop.BOOL* pResult);

    [PreserveSig]
    int GetRcwCachedInterfacePointers(ulong vmObject, Interop.BOOL bIInspectableOnly, nint pDacItfPtrs);

    [PreserveSig]
    int GetTypedByRefInfo(ulong pTypedByRef, nint pObjectData);

    [PreserveSig]
    int GetStringData(ulong objectAddress, nint pObjectData);

    [PreserveSig]
    int GetArrayData(ulong objectAddress, nint pObjectData);

    [PreserveSig]
    int GetBasicObjectInfo(ulong objectAddress, int type, nint pObjectData);

    [PreserveSig]
    int GetDebuggerControlBlockAddress(ulong* pRetVal);

    [PreserveSig]
    int GetObjectFromRefPtr(ulong ptr, ulong* pRetVal);

    [PreserveSig]
    int GetObject(ulong ptr, ulong* pRetVal);

    [PreserveSig]
    int GetVmObjectHandle(ulong handleAddress, ulong* pRetVal);

    [PreserveSig]
    int IsVmObjectHandleValid(ulong vmHandle, Interop.BOOL* pResult);

    [PreserveSig]
    int IsWinRTModule(ulong vmModule, Interop.BOOL* isWinRT);

    [PreserveSig]
    int GetHandleAddressFromVmHandle(ulong vmHandle, ulong* pRetVal);

    [PreserveSig]
    int GetObjectContents(ulong obj, DacDbiTargetBuffer* pRetVal);

    [PreserveSig]
    int GetThreadOwningMonitorLock(ulong vmObject, DacDbiMonitorLockInfo* pRetVal);

    [PreserveSig]
    int EnumerateMonitorEventWaitList(ulong vmObject, nint fpCallback, nint pUserData);

    [PreserveSig]
    int GetAttachStateFlags(int* pRetVal);

    [PreserveSig]
    int GetMetaDataFileInfoFromPEFile(ulong vmPEAssembly, uint* dwTimeStamp, uint* dwImageSize, nint pStrFilename, Interop.BOOL* pResult);

    [PreserveSig]
    int IsThreadSuspendedOrHijacked(ulong vmThread, Interop.BOOL* pResult);

    [PreserveSig]
    int AreGCStructuresValid(Interop.BOOL* pResult);

    [PreserveSig]
    int CreateHeapWalk(nuint* pHandle);

    [PreserveSig]
    int DeleteHeapWalk(nuint handle);

    [PreserveSig]
    int WalkHeap(nuint handle, uint count, COR_HEAPOBJECT* objects, uint* fetched);

    [PreserveSig]
    int GetHeapSegments(nint pSegments);

    [PreserveSig]
    int IsValidObject(ulong obj, Interop.BOOL* pResult);

    [PreserveSig]
    int CreateRefWalk(nuint* pHandle, Interop.BOOL walkStacks, Interop.BOOL walkFQ, uint handleWalkMask);

    [PreserveSig]
    int DeleteRefWalk(nuint handle);

    [PreserveSig]
    int WalkRefs(nuint handle, uint count, nint refs, uint* pFetched);

    [PreserveSig]
    int GetTypeID(ulong obj, COR_TYPEID* pType);

    [PreserveSig]
    int GetTypeIDForType(ulong vmTypeHandle, COR_TYPEID* pId);

    [PreserveSig]
    int GetObjectFields(nint id, uint celt, COR_FIELD* layout, uint* pceltFetched);

    [PreserveSig]
    int GetTypeLayout(nint id, COR_TYPE_LAYOUT* pLayout);

    [PreserveSig]
    int GetArrayLayout(nint id, nint pLayout);

    [PreserveSig]
    int GetGCHeapInformation(COR_HEAPINFO* pHeapInfo);

    [PreserveSig]
    int GetPEFileMDInternalRW(ulong vmPEAssembly, ulong* pAddrMDInternalRW);

    [PreserveSig]
    int AreOptimizationsDisabled(ulong vmModule, uint methodTk, Interop.BOOL* pOptimizationsDisabled);

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
    int GetILCodeVersionNodeData(ulong ilCodeVersionNode, DacDbiSharedReJitInfo* pData);

    [PreserveSig]
    int EnableGCNotificationEvents(Interop.BOOL fEnable);

    [PreserveSig]
    int IsDelegate(ulong vmObject, Interop.BOOL* pResult);

    [PreserveSig]
    int GetDelegateType(ulong delegateObject, int* delegateType);

    [PreserveSig]
    int GetDelegateFunctionData(int delegateType, ulong delegateObject, ulong* ppFunctionAssembly, uint* pMethodDef);

    [PreserveSig]
    int GetDelegateTargetObject(int delegateType, ulong delegateObject, ulong* ppTargetObj, ulong* ppTargetAppDomain);

    [PreserveSig]
    int GetLoaderHeapMemoryRanges(nint pRanges);

    [PreserveSig]
    int IsModuleMapped(ulong pModule, int* isModuleMapped);

    [PreserveSig]
    int MetadataUpdatesApplied(Interop.BOOL* pResult);

    [PreserveSig]
    int GetAssemblyFromModule(ulong vmModule, ulong* pVmAssembly);

    [PreserveSig]
    int ParseContinuation(ulong continuationAddress, ulong* pDiagnosticIP, ulong* pNextContinuation, uint* pState);

    [PreserveSig]
    int GetAsyncLocals(ulong vmMethod, ulong codeAddr, uint state, nint pAsyncLocals);

    [PreserveSig]
    int GetGenericArgTokenIndex(ulong vmMethod, uint* pIndex);
}
