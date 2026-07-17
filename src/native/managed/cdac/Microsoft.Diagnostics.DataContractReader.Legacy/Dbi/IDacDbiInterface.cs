// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using CorElementType = Microsoft.Diagnostics.DataContractReader.Contracts.CorElementType;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

[StructLayout(LayoutKind.Sequential)]
public struct COR_TYPEID
{
    public ulong token1;
    public ulong token2;
}

[StructLayout(LayoutKind.Sequential)]
public struct FieldData
{
    public uint m_fldMetadataToken;
    public Interop.BOOL m_fFldStorageAvailable;

    public byte m_fFldIsStatic;
    public byte m_fFldIsRVA;
    public byte m_fFldIsTLS;
    public byte m_fFldIsPrimitive;
    public byte m_fFldIsCollectibleStatic;

    public ulong m_fldInstanceOffset;
    public ulong m_pFldStaticAddress;
    public nuint m_fldSignatureCache;
    public uint m_fldSignatureCacheSize;

    public ulong m_vmFieldDesc;
}

public enum VarLocType
{
    VLT_REG,
    VLT_REG_BYREF,
    VLT_REG_FP,
    VLT_STK,
    VLT_STK_BYREF,
    VLT_REG_REG,
    VLT_REG_STK,
    VLT_STK_REG,
    VLT_STK2,
    VLT_FPSTK,
    VLT_FIXED_VA,
    VLT_COUNT,
    VLT_INVALID,
}

// Mirrors the native ICorDebugInfo::VarLoc tagged union: a VarLocType selector plus a union
// payload, modelled as three 4-byte slots with typed accessors per VarLocType below.
[StructLayout(LayoutKind.Sequential)]
public struct VarLoc
{
    public VarLocType vlType;

    private uint _field1;
    private int _field2;
    private int _field3;

    // vlReg / vlReg_BYREF
    public uint vlrReg { get => _field1; set => _field1 = value; }

    // vlStk / vlStk_BYREF / vlStk2
    public uint vlsBaseReg { get => _field1; set => _field1 = value; }
    public int vlsOffset { get => _field2; set => _field2 = value; }

    // vlRegReg
    public uint vlrrReg1 { get => _field1; set => _field1 = value; }
    public uint vlrrReg2 { get => (uint)_field2; set => _field2 = (int)value; }

    // vlRegStk
    public uint vlrsReg { get => _field1; set => _field1 = value; }
    public uint vlrssBaseReg { get => (uint)_field2; set => _field2 = (int)value; }
    public int vlrssOffset { get => _field3; set => _field3 = value; }

    // vlStkReg
    public uint vlsrsBaseReg { get => _field1; set => _field1 = value; }
    public int vlsrsOffset { get => _field2; set => _field2 = value; }
    public uint vlsrReg { get => (uint)_field3; set => _field3 = (int)value; }

    // vlFPstk
    public uint vlfReg { get => _field1; set => _field1 = value; }

    // vlFixedVarArg
    public uint vlfvOffset { get => _field1; set => _field1 = value; }
}

[StructLayout(LayoutKind.Sequential)]
public struct NativeVarInfo
{
    public uint startOffset;
    public uint endOffset;
    public uint callReturnValueILOffset;
    public uint varNumber;
    public VarLoc loc;
}

[Flags]
public enum DbiSourceTypes : uint
{
    SourceTypeInvalid = 0x00,
    SequencePoint = 0x01,
    StackEmpty = 0x02,
    CallSite = 0x04,
    NativeEndOffsetUnknown = 0x08,
    CallInstruction = 0x10,
    Async = 0x20,
}

[StructLayout(LayoutKind.Sequential)]
public struct DbiOffsetMapping
{
    public uint nativeOffset;
    public uint ilOffset;
    public DbiSourceTypes source;
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
    public ulong pbIL;
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
public struct AsyncLocalData
{
    public uint Offset;
    public uint IlVarNum;
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

[StructLayout(LayoutKind.Explicit)]
public struct DacGcReference
{
    [FieldOffset(0)] public ulong vmDomain;
    [FieldOffset(8)] public ulong pObject;
    [FieldOffset(8)] public ulong objHnd;
    [FieldOffset(16)] public CorGCReferenceType dwType;
    [FieldOffset(24)] public ulong i64ExtraData;
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
public struct COR_ARRAY_LAYOUT
{
    public COR_TYPEID componentID;
    public CorElementType componentType;
    public uint firstElementOffset;
    public uint elementSize;
    public uint countOffset;
    public uint rankSize;
    public uint numRanks;
    public uint rankOffset;
}

[StructLayout(LayoutKind.Sequential)]
public struct COR_FIELD
{
    public uint token;
    public uint offset;
    public COR_TYPEID id;
    public int fieldType;
}

[StructLayout(LayoutKind.Sequential)]
public struct Debugger_FuncData
{
    public uint funcMetadataToken;       // mdMethodDef
    public ulong vmAssembly;
}

[StructLayout(LayoutKind.Sequential)]
public struct Debugger_JITFuncData
{
    public ulong nativeStartAddressPtr;
    public ulong nativeHotSize;
    public ulong nativeStartAddressColdPtr;
    public ulong nativeColdSize;
    public ulong nativeOffset;
    public ulong vmNativeCodeMethodDescToken;
    public Interop.BOOL fIsFilterFrame;
    public ulong parentNativeOffset;
    public ulong fpParentOrSelf;
    public Interop.BOOL isInstantiatedGeneric;
    public Interop.BOOL justAfterILThrow;
}

// Data for a method frame (the v variant of the Debugger_STRData union).
[StructLayout(LayoutKind.Sequential)]
public struct DebuggerIPCE_STRData_MethodFrame
{
    public Debugger_FuncData funcData;
    public Debugger_JITFuncData jitFuncData;
    public int mapping;                            // CorDebugMappingResult
    public byte fVarArgs;                          // bool
    public byte fNoMetadata;                       // bool
    public ulong taAmbientESP;                     // TADDR
    public ulong exactGenericArgsToken;            // GENERICS_TYPE_TOKEN
    public uint dwExactGenericArgsTokenIndex;
}

// Data for a stub frame (the stubFrame variant of the Debugger_STRData union).
[StructLayout(LayoutKind.Sequential)]
public struct DebuggerIPCE_STRData_StubFrame
{
    public uint funcMetadataToken;                 // mdMethodDef
    public ulong vmAssembly;                       // VMPTR_Assembly
    public ulong vmMethodDesc;                     // VMPTR_MethodDesc
    public int frameType;                          // CorDebugInternalFrameType
}

// Holds data for each stack frame or chain, passed from the RC to the DI during a
// stack walk. Mirrors the native Debugger_STRData in src/coreclr/debug/inc/dacdbistructures.h.
// ctx is a host-sized pointer to a dbi-allocated DT_CONTEXT buffer the DAC writes through;
// paths that produce no context (e.g. EnumerateInternalFrames cStubFrame entries) leave it 0.
[StructLayout(LayoutKind.Explicit)]
public struct Debugger_STRData
{
    public enum EType
    {
        cMethodFrame = 0,
        cStubFrame = 1,
        cRuntimeNativeFrame = 2,
    }

    [FieldOffset(0)] public ulong fp;                           // FramePointer (CORDB_ADDRESS)
    [FieldOffset(8)] public nuint ctx;                          // DT_CONTEXT* (host-sized)
    [FieldOffset(16)] public ulong vmCurrentAppDomainToken;     // VMPTR_AppDomain
    [FieldOffset(24)] public EType eType;
    // v (method frame) and stubFrame overlap, mirroring the native anonymous union.
    [FieldOffset(32)] public DebuggerIPCE_STRData_MethodFrame v;
    [FieldOffset(32)] public DebuggerIPCE_STRData_StubFrame stubFrame;
}

#pragma warning restore CS0649

public enum CorDebugInternalFrameType
{
    STUBFRAME_NONE = 0x00000000,
    STUBFRAME_M2U = 0x00000001,
    STUBFRAME_U2M = 0x00000002,
    STUBFRAME_FUNC_EVAL = 0x00000005,
    STUBFRAME_INTERNALCALL = 0x00000006,
    STUBFRAME_CLASS_INIT = 0x00000007,
    STUBFRAME_EXCEPTION = 0x00000008,
    STUBFRAME_JIT_COMPILATION = 0x0000000a,
}

public enum AreValueTypesBoxed : int
{
    NoValueTypeBoxing = 0,
    OnlyPrimitivesUnboxed = 1,
    AllBoxed = 2
}
// Matches native DebuggerIPCE_BasicTypeData layout (24 bytes).
// All fields are stored in little-endian format (Portable<T> in native).
[StructLayout(LayoutKind.Explicit, Size = 24)]
public struct DebuggerIPCE_BasicTypeData
{
    [FieldOffset(0)] public int elementType;       // Portable<CorElementType>
    [FieldOffset(4)] public uint metadataToken;    // Portable<mdTypeDef>
    [FieldOffset(8)] public ulong vmAssembly;      // VMPTR_Assembly (Portable<CORDB_ADDRESS>)
    [FieldOffset(16)] public ulong vmTypeHandle;   // VMPTR_TypeHandle (Portable<CORDB_ADDRESS>)
}

[StructLayout(LayoutKind.Sequential)]
public struct EnCHangingFieldInfo
{
    public DebuggerIPCE_BasicTypeData objectTypeData;
    public ulong vmObject;
    public uint offsetToVars;
    public uint fldToken;
}

// Matches native DebuggerIPCE_ExpandedTypeData layout (40 bytes).
// Contains a union at offset 8 (4 bytes of padding after elementType to align the
// 8-byte VMPTR fields inside the union). All fields are stored in little-endian format.
[StructLayout(LayoutKind.Explicit, Size = 40)]
public struct DebuggerIPCE_ExpandedTypeData
{
    [FieldOffset(0)] public int elementType;       // Portable<CorElementType>

    // ClassTypeData (used for E_T_CLASS, E_T_VALUETYPE)
    [FieldOffset(8)] public uint ClassTypeData_metadataToken;    // Portable<mdTypeDef>
    [FieldOffset(16)] public ulong ClassTypeData_vmAssembly;     // VMPTR_Assembly
    [FieldOffset(24)] public ulong ClassTypeData_typeHandle;     // VMPTR_TypeHandle

    // UnaryTypeData (used for E_T_PTR, E_T_BYREF) — overlaps union at offset 8
    [FieldOffset(8)] public DebuggerIPCE_BasicTypeData UnaryTypeData_unaryTypeArg;

    // ArrayTypeData (used for E_T_ARRAY, E_T_SZARRAY) — overlaps union at offset 8
    [FieldOffset(8)] public DebuggerIPCE_BasicTypeData ArrayTypeData_arrayTypeArg;
    [FieldOffset(32)] public uint ArrayTypeData_arrayRank;       // Portable<DWORD>

    // NaryTypeData (used for E_T_FNPTR) — overlaps union at offset 8
    [FieldOffset(8)] public ulong NaryTypeData_typeHandle;       // VMPTR_TypeHandle
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct ArgInfoList
{
    public DebuggerIPCE_BasicTypeData* m_pList;
    public int m_nEntries;
}

[StructLayout(LayoutKind.Sequential, Size = 48)]
public struct DebuggerIPCE_TypeArgData
{
    public DebuggerIPCE_ExpandedTypeData data;
    public uint numTypeArgs; // Portable<UINT>
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct TypeInfoList
{
    public DebuggerIPCE_TypeArgData* m_pList;
    public int m_nEntries;
}

[StructLayout(LayoutKind.Sequential)]
public struct DacDbiArrayInfo
{
    public uint rank;
    public uint componentCount;
    public uint offsetToArrayBase;
    public uint offsetToUpperBounds;   // 0 for SZArray
    public uint offsetToLowerBounds;   // 0 for SZArray
    public uint elementSize;
}

public enum DynamicMethodType
{
    kNone = 0,
    kDiagnosticHidden = 1,
    kLCGMethod = 2,
}

public enum CorDebugThreadState
{
    ThreadRun = 0,
    ThreadSuspend = 1,
}

[Flags]
public enum CorDebugUserState
{
    USER_BACKGROUND = 0x04,
    USER_UNSTARTED = 0x08,
    USER_STOPPED = 0x10,
    USER_WAIT_SLEEP_JOIN = 0x20,
    USER_UNSAFE_POINT = 0x80,
    USER_THREADPOOL = 0x100,
}

public enum SymbolFormat
{
    None = 0,
    Pdb = 1,
}

public enum CorDebugGenerationTypes
{
    CorDebug_Gen0 = 0,
    CorDebug_Gen1 = 1,
    CorDebug_Gen2 = 2,
    CorDebug_LOH = 3,
    CorDebug_POH = 4,
    CorDebug_NonGC = 0x7FFFFFFF,
}

public enum IlNum : int
{
    TYPECTXT_ILNUM = -3,
}

[Flags]
public enum CorGCReferenceType : uint
{
    CorHandleStrong = 1 << 0,
    CorHandleStrongPinning = 1 << 1,
    CorHandleWeakShort = 1 << 2,
    CorHandleWeakLong = 1 << 3,
    CorHandleWeakRefCount = 1 << 4,
    CorHandleStrongRefCount = 1 << 5,
    CorHandleStrongDependent = 1 << 6,
    CorReferenceStack = 0x80000001,
}

public enum CorDebugSetContextFlags
{
    SET_CONTEXT_FLAG_ACTIVE_FRAME = 0x1,
    SET_CONTEXT_FLAG_UNWIND_FRAME = 0x2,
}

// Name-surface projection of IDacDbiInterface in native method order for COM binding validation.
// Parameter shapes are intentionally coarse placeholders and will be refined with method implementation work.
[GeneratedComInterface]
[Guid("DB505C1B-A327-4A46-8C32-AF55A56F8E09")]
public unsafe partial interface IDacDbiInterface
{
    [PreserveSig]
    int FlushCache();

    [PreserveSig]
    int DacSetTargetConsistencyChecks(Interop.BOOL fEnableAsserts);

    [PreserveSig]
    int IsLeftSideInitialized(Interop.BOOL* pResult);

    [PreserveSig]
    int GetAppDomainId(ulong vmAppDomain, uint* pRetVal);

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
    int GetSymbolsBuffer(ulong vmModule, DacDbiTargetBuffer* pTargetBuffer, SymbolFormat* pSymbolFormat);

    [PreserveSig]
    int GetModuleData(ulong vmModule, DacDbiModuleInfo* pData);

    [PreserveSig]
    int GetModuleForAssembly(ulong vmAssembly, ulong* pModule, Interop.BOOL* pIsModuleLoaded);

    [PreserveSig]
    int IsManagedCode(ulong address, Interop.BOOL* pIsManaged);

    [PreserveSig]
    int GetCompilerFlags(ulong vmAssembly, Interop.BOOL* pfAllowJITOpts, Interop.BOOL* pfEnableEnC);

    [PreserveSig]
    int SetCompilerFlags(ulong vmAssembly, Interop.BOOL fAllowJitOpts, Interop.BOOL fEnableEnC);

    [PreserveSig]
    int EnumerateAssembliesInAppDomain(ulong vmAppDomain, delegate* unmanaged<ulong, nint, void> fpCallback, nint pUserData);

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
    int EnumerateThreads(delegate* unmanaged<ulong, nint, void> fpCallback, nint pUserData);

    [PreserveSig]
    int IsThreadMarkedDead(ulong vmThread, Interop.BOOL* pResult);

    [PreserveSig]
    int GetThreadHandle(ulong vmThread, void** pRetVal);

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
    int GetPartialUserState(ulong vmThread, CorDebugUserState* pRetVal);

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
    int GetNativeCodeSequencePointsAndVarInfo(ulong vmMethodDesc, ulong startAddress, Interop.BOOL fCodeAvailable, uint* pFixedArgCount, delegate* unmanaged<NativeVarInfo*, void*, void> fpVarInfoCallback, delegate* unmanaged<DbiOffsetMapping*, void*, void> fpSeqPointCallback, nint pUserData);

    [PreserveSig]
    int GetManagedStoppedContext(ulong vmThread, ulong* pRetVal);

    [PreserveSig]
    int CreateStackWalk(ulong vmThread, byte* pInternalContextBuffer, nuint* ppSFIHandle);

    [PreserveSig]
    int DeleteStackWalk(nuint ppSFIHandle);

    [PreserveSig]
    int GetStackWalkCurrentContext(nuint pSFIHandle, byte* pContext);

    [PreserveSig]
    int SetStackWalkCurrentContext(ulong vmThread, nuint pSFIHandle, int flag, byte* pContext);

    [PreserveSig]
    int UnwindStackWalkFrame(nuint pSFIHandle, Interop.BOOL* pResult);

    [PreserveSig]
    int CheckContext(ulong vmThread, byte* pContext);

    [PreserveSig]
    int GetStackWalkCurrentFrameInfo(nuint pSFIHandle, nint pFrameData, int* pRetVal);

    [PreserveSig]
    int GetCountOfInternalFrames(ulong vmThread, uint* pRetVal);

    [PreserveSig]
    int EnumerateInternalFrames(ulong vmThread, delegate* unmanaged<Debugger_STRData*, void*, void> fpCallback, nint pUserData);

    [PreserveSig]
    int GetStackParameterSize(ulong controlPC, uint* pRetVal);

    [PreserveSig]
    int IsLeafFrame(ulong vmThread, byte* pContext, Interop.BOOL* pResult);

    [PreserveSig]
    int GetContext(ulong vmThread, byte* pContextBuffer);

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
    int EnumerateClassFields(ulong thExact, nuint* pObjectSize, delegate* unmanaged<FieldData*, void*, void> fpCallback, nint pUserData);

    [PreserveSig]
    int EnumerateInstantiationFields(ulong vmAssembly, ulong vmThExact, ulong vmThApprox, nuint* pObjectSize, delegate* unmanaged<FieldData*, void*, void> fpCallback, nint pUserData);

    [PreserveSig]
    int TypeHandleToExpandedTypeInfo(AreValueTypesBoxed boxed, ulong vmTypeHandle, DebuggerIPCE_ExpandedTypeData* pData);

    [PreserveSig]
    int GetObjectExpandedTypeInfo(AreValueTypesBoxed boxed, ulong addr, DebuggerIPCE_ExpandedTypeData* pTypeInfo);

    [PreserveSig]
    int GetTypeHandle(ulong vmModule, uint metadataToken, ulong* pRetVal);

    [PreserveSig]
    int GetApproxTypeHandle(TypeInfoList* pTypeData, ulong* pRetVal);

    [PreserveSig]
    int GetExactTypeHandle(DebuggerIPCE_ExpandedTypeData* pTypeData, ArgInfoList* pArgInfo, ulong* pVmTypeHandle);

    [PreserveSig]
    int EnumerateMethodDescParams(ulong vmMethodDesc, ulong genericsToken, uint* pcGenericClassTypeParams,
        delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> fpCallback, nint pUserData);

    [PreserveSig]
    int GetThreadStaticAddress(ulong vmField, ulong vmRuntimeThread, ulong* pRetVal);

    [PreserveSig]
    int GetCollectibleTypeStaticAddress(ulong vmField, ulong* pRetVal);

    [PreserveSig]
    int GetEnCHangingFieldInfo(EnCHangingFieldInfo* pEnCFieldInfo, FieldData* pFieldData);

    [PreserveSig]
    int EnumerateTypeHandleParams(ulong vmTypeHandle,
        delegate* unmanaged<DebuggerIPCE_ExpandedTypeData*, nint, void> fpCallback, nint pUserData);

    [PreserveSig]
    int GetSimpleType(int simpleType, uint* pMetadataToken, ulong* pVmModule);

    [PreserveSig]
    int IsExceptionObject(ulong vmObject, Interop.BOOL* pResult);

    [PreserveSig]
    int EnumerateStackFramesFromException(ulong vmObject, /*FP_EXCEPTION_STACK_FRAME_CALLBACK*/ delegate* unmanaged<ulong, ulong, ulong, uint, Interop.BOOL, nint, void> fpCallback, nint pUserData);

    [PreserveSig]
    int IsRcw(ulong vmObject, Interop.BOOL* pResult);

    [PreserveSig]
    int EnumerateRcwCachedInterfacePointers(ulong vmObject, /*FP_RCW_INTERFACE_CALLBACK*/ delegate* unmanaged<ulong, nint, void> fpCallback, nint pUserData);

    [PreserveSig]
    int GetTypedByRefInfo(ulong pTypedByRef, ulong* pObjRef, DebuggerIPCE_BasicTypeData* pTypedByRefType);

    [PreserveSig]
    int GetStringData(ulong objectAddress, uint* pLength, uint* pOffsetToStringBase);

    [PreserveSig]
    int GetArrayData(ulong objectAddress, Interop.BOOL* pIsValidArray, DacDbiArrayInfo* pArrayInfo);

    [PreserveSig]
    int GetBasicObjectInfo(ulong objectAddress, Interop.BOOL* pIsValidRef, uint* pObjSize, uint* pObjOffsetToVars, DebuggerIPCE_ExpandedTypeData* pObjTypeData);

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
    int GetHandleAddressFromVmHandle(ulong vmHandle, ulong* pRetVal);

    [PreserveSig]
    int GetThreadOwningMonitorLock(ulong vmObject, DacDbiMonitorLockInfo* pRetVal);

    [PreserveSig]
    int EnumerateMonitorEventWaitList(ulong vmObject, nint fpCallback, nint pUserData);

    [PreserveSig]
    int GetAttachStateFlags(int* pRetVal);

    [PreserveSig]
    int GetModuleMetaDataFileInfo(ulong vmModule, uint* dwTimeStamp, uint* dwImageSize, nint pStrFilename, Interop.BOOL* pResult);

    [PreserveSig]
    int IsThreadSuspendedOrHijacked(ulong vmThread, Interop.BOOL* pResult);

    [PreserveSig]
    int CreateHeapWalk(nuint* pHandle);

    [PreserveSig]
    int DeleteHeapWalk(nuint handle);

    [PreserveSig]
    int WalkHeap(nuint handle, uint count, COR_HEAPOBJECT* objects, uint* fetched);

    [PreserveSig]
    int EnumerateHeapSegments(/*FP_HEAPSEGMENT_CALLBACK*/ delegate* unmanaged<ulong, ulong, int, uint, nint, void> fpCallback, nint pUserData);

    [PreserveSig]
    int IsValidObject(ulong obj, Interop.BOOL* pResult);

    [PreserveSig]
    int CreateRefWalk(nuint* pHandle, Interop.BOOL walkStacks, CorGCReferenceType handleWalkMask);

    [PreserveSig]
    int DeleteRefWalk(nuint handle);

    [PreserveSig]
    int WalkRefs(nuint handle, uint count, [In, Out, MarshalUsing(CountElementName = nameof(count))] DacGcReference[] refs, uint* pFetched);

    [PreserveSig]
    int GetTypeID(ulong obj, COR_TYPEID* pType);

    [PreserveSig]
    int GetTypeIDForType(ulong vmTypeHandle, COR_TYPEID* pId);

    [PreserveSig]
    int GetObjectFields(ulong id, uint celt, COR_FIELD* layout, uint* pceltFetched);

    [PreserveSig]
    int GetTypeLayout(ulong id, COR_TYPE_LAYOUT* pLayout);

    [PreserveSig]
    int GetArrayLayout(ulong id, COR_ARRAY_LAYOUT* pLayout);

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
    int GetDelegateFunctionData(ulong delegateObject, ulong* ppFunctionAssembly, uint* pMethodDef);

    [PreserveSig]
    int GetDelegateTargetObject(ulong delegateObject, ulong* ppTargetObj);

    [PreserveSig]
    int IsModuleMapped(ulong pModule, Interop.BOOL* isModuleMapped);

    [PreserveSig]
    int MetadataUpdatesApplied(Interop.BOOL* pResult);

    [PreserveSig]
    int GetAssemblyFromModule(ulong vmModule, ulong* pVmAssembly);

    [PreserveSig]
    int ParseContinuation(ulong continuationAddress, ulong* pDiagnosticIP, ulong* pNextContinuation, uint* pState);

    [PreserveSig]
    int EnumerateAsyncLocals(ulong vmMethod, ulong codeAddr, uint state,
        delegate* unmanaged<AsyncLocalData*, nint, void> fpCallback, nint pUserData);

    [PreserveSig]
    int GetGenericArgTokenIndex(ulong vmMethod, uint* pIndex);
}
