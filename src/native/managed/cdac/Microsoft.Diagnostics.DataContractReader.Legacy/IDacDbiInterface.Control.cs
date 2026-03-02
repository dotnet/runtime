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
}
