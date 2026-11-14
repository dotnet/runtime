// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The records that the method contexts store for the JIT-EE interface calls: platform agnostic, in the layout of
// the .mc format.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SuperPmi;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_SIG_INFO
{
    public uint callConv;
    public ulong retTypeClass;
    public ulong retTypeSigClass;
    public uint retType;
    public uint flags;
    public uint numArgs;
    public uint sigInst_classInstCount;
    public uint sigInst_classInst_Index;
    public uint sigInst_methInstCount;
    public uint sigInst_methInst_Index;
    public ulong args;
    public uint pSig_Index;
    public uint cbSig;
    public ulong methodSignature;
    public ulong scope;
    public uint token;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_CONST_LOOKUP
{
    public uint accessType;
    public ulong handle;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_LOOKUP_KIND
{
    public uint needsRuntimeLookup;
    public uint runtimeLookupKind;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_RUNTIME_LOOKUP
{
    public ulong signature;
    public uint helper;
    public uint indirections;
    public uint testForNull;
    public ushort sizeOffset;
    public UlongArray4 offsets;
    public uint indirectFirstOffset;
    public uint indirectSecondOffset;
    public Agnostic_CORINFO_CONST_LOOKUP helperEntryPoint;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_LOOKUP
{
    public Agnostic_CORINFO_LOOKUP_KIND lookupKind;
    public Agnostic_CORINFO_RUNTIME_LOOKUP runtimeLookup;
    public Agnostic_CORINFO_CONST_LOOKUP constLookup;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_METHOD_INFO
{
    public ulong ftn;
    public ulong scope;
    public uint ILCode_offset;
    public uint ILCodeSize;
    public uint maxStack;
    public uint EHcount;
    public uint options;
    public uint regionKind;
    public Agnostic_CORINFO_SIG_INFO args;
    public Agnostic_CORINFO_SIG_INFO locals;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CompileMethod
{
    public Agnostic_CORINFO_METHOD_INFO info;
    public uint flags;
    public uint os;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_InitClass
{
    public ulong field;
    public ulong method;
    public ulong context;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DLDL
{
    public ulong A;
    public ulong B;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CanInline
{
    public uint result;
    public uint exceptionCode;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetClassGClayout
{
    public uint gcPtrs_Index;
    public uint len;
    public uint valCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DLD
{
    public ulong A;
    public uint B;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DLDD
{
    public ulong A;
    public uint B;
    public uint C;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DLDDD
{
    public ulong A;
    public uint B;
    public uint C;
    public uint D;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_METHODNAME_TOKENin
{
    public ulong ftn;
    public uint className;
    public uint namespaceName;
    public uint maxEnclosingClassNames;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_METHODNAME_TOKENout
{
    public uint methodName;
    public uint className;
    public uint namespaceName;
    public uint enclosingClassNames;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_RESOLVED_TOKENin
{
    public ulong tokenContext;
    public ulong tokenScope;
    public uint token;
    public uint tokenType;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_RESOLVED_TOKENout
{
    public ulong hClass;
    public ulong hMethod;
    public ulong hField;
    public uint pTypeSpec_Index;
    public uint cbTypeSpec;
    public uint pMethodSpec_Index;
    public uint cbMethodSpec;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetArgType_Key
{
    public uint flags;
    public uint numArgs;
    public uint sigInst_classInstCount;
    public uint sigInst_classInst_Index;
    public uint sigInst_methInstCount;
    public uint sigInst_methInst_Index;
    public ulong methodSignature;
    public ulong scope;
    public ulong args;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetArgClass_Key
{
    public uint sigInst_classInstCount;
    public uint sigInst_classInst_Index;
    public uint sigInst_methInstCount;
    public uint sigInst_methInst_Index;
    public ulong methodSignature;
    public ulong scope;
    public ulong args;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetBoundaries
{
    public uint cILOffsets;
    public uint pILOffset_offset;
    public uint implicitBoundaries;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_InlinedCallFrameInfo
{
    public uint size;
    public uint sizeWithSecretStubArg;
    public uint offsetOfFrameLink;
    public uint offsetOfCallSiteSP;
    public uint offsetOfCalleeSavedFP;
    public uint offsetOfCallTarget;
    public uint offsetOfReturnAddress;
    public uint offsetOfSecretStubArg;
    public uint offsetOfSPAfterProlog;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_EE_INFO
{
    public Agnostic_InlinedCallFrameInfo inlinedCallFrameInfo;
    public uint offsetOfThreadFrame;
    public uint offsetOfGCState;
    public uint offsetOfDelegateInstance;
    public uint offsetOfDelegateFirstTarget;
    public uint sizeOfReversePInvokeFrame;
    public uint osPageSize;
    public uint maxUncheckedOffsetForNullObject;
    public uint targetAbi;
    public uint osType;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_ASYNC_INFO
{
    public ulong continuationClsHnd;
    public ulong continuationNextFldHnd;
    public ulong continuationResumeInfoFldHnd;
    public ulong continuationStateFldHnd;
    public ulong continuationFlagsFldHnd;
    public ulong captureExecutionContextMethHnd;
    public ulong captureContinuationContextMethHnd;
    public ulong captureContextsMethHnd;
    public ulong restoreContextsMethHnd;
    public ulong restoreContextsOnSuspensionMethHnd;
    public ulong restoreInlinedFrameContextsMethHnd;
    public ulong captureInlinedFrameTransitionWithContinuationContextMethHnd;
    public ulong captureInlinedFrameTransitionNoContinuationContextMethHnd;
    public ulong captureInlinedFrameTransitionContinueOnThreadPoolMethHnd;
    public ulong finishSuspensionNoContinuationContextMethHnd;
    public ulong finishSuspensionWithContinuationContextMethHnd;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_WASM_WELLKNOWN_GLOBALS
{
    public ulong stackPointer;
    public ulong imageBase;
    public ulong tableBase;
    public ulong asyncContinuation;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetAwaitReturnCallResult
{
    public ulong methodHnd;
    public ulong contextHandle;
    public Agnostic_CORINFO_LOOKUP instArg;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetOSRInfo
{
    public uint index;
    public uint ilOffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetStaticFieldCurrentClass
{
    public ulong classHandle;
    public byte isSpeculative;  // bool
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_TYPE_LAYOUT_NODE
{
    public ulong simdTypeHnd;
    public ulong diagFieldHnd;
    public uint parent;
    public uint offset;
    public uint size;
    public uint numFields;
    public byte type;
    public byte hasSignificantPadding;  // bool
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetTypeLayoutResult
{
    public uint result;
    public uint nodesBuffer;
    public uint numNodes;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_RESOLVED_TOKEN
{
    public Agnostic_CORINFO_RESOLVED_TOKENin inValue;
    public Agnostic_CORINFO_RESOLVED_TOKENout outValue;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetAwaitAwaiterInContinuationCall
{
    public ulong callerHnd;
    public Agnostic_CORINFO_RESOLVED_TOKEN ResolvedToken;
    public uint isUnsafe;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetFieldInfo
{
    public Agnostic_CORINFO_RESOLVED_TOKEN ResolvedToken;
    public ulong callerHandle;
    public uint flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_HELPER_ARG
{
    public ulong constant;
    public uint argType;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_HELPER_DESC
{
    public uint helperNum;
    public uint numArgs;
    public Agnostic_CORINFO_HELPER_ARGArray4 args;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetHelperFtn
{
    public Agnostic_CORINFO_CONST_LOOKUP helperLookup;
    public ulong helperMethod;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_FIELD_INFO
{
    public uint fieldAccessor;
    public uint fieldFlags;
    public uint helper;
    public uint offset;
    public uint fieldType;
    public ulong structType;
    public uint accessAllowed;
    public Agnostic_CORINFO_HELPER_DESC accessCalloutHelper;
    public Agnostic_CORINFO_CONST_LOOKUP fieldLookup;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DD
{
    public uint A;
    public uint B;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DDD
{
    public uint A;
    public uint B;
    public uint C;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CanTailCall
{
    public ulong callerHnd;
    public ulong declaredCalleeHnd;
    public ulong exactCalleeHnd;
    public ushort fIsTailPrefix;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_Environment
{
    public uint name_index;
    public uint val_index;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetCallInfo
{
    public Agnostic_CORINFO_RESOLVED_TOKEN ResolvedToken;
    public Agnostic_CORINFO_RESOLVED_TOKEN ConstrainedResolvedToken;
    public ulong callerHandle;
    public uint flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_CALL_INFO
{
    public ulong hMethod;
    public uint methodFlags;
    public uint classFlags;
    public Agnostic_CORINFO_SIG_INFO sig;
    public uint accessAllowed;
    public Agnostic_CORINFO_HELPER_DESC callsiteCalloutHelper;
    public uint thisTransform;
    public uint kind;
    public uint nullInstanceCheck;
    public ulong contextHandle;
    public uint exactContextNeedsRuntimeLookup;
    public Agnostic_CORINFO_LOOKUP stubLookup;
    public Agnostic_CORINFO_CONST_LOOKUP instParamLookup;
    public uint exceptionCode;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetMethodInfo
{
    public Agnostic_CORINFO_METHOD_INFO info;
    public byte result;  // bool
    public uint exceptionCode;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_FindSig
{
    public ulong module;
    public uint sigTOK;
    public ulong context;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MethodOrSigInfoValue
{
    public ulong method;
    public uint pSig_Index;
    public uint cbSig;
    public ulong scope;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_EH_CLAUSE
{
    public uint Flags;
    public uint TryOffset;
    public uint TryLength;
    public uint HandlerOffset;
    public uint HandlerLength;
    public uint ClassToken;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetVars
{
    public uint cVars;
    public uint vars_offset;
    public uint extendOthers;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CanAccessClassIn
{
    public Agnostic_CORINFO_RESOLVED_TOKEN ResolvedToken;
    public ulong callerHandle;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CanAccessClassOut
{
    public Agnostic_CORINFO_HELPER_DESC AccessHelper;
    public uint result;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_AppendClassNameIn
{
    public uint nBufLenIsZero;
    public ulong classHandle;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_AppendClassNameOut
{
    public uint nLen;
    public uint name_index;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CheckMethodModifier
{
    public ulong hMethod;
    public uint modifier;
    public uint fOptional;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_EmbedGenericHandle
{
    public Agnostic_CORINFO_RESOLVED_TOKEN ResolvedToken;
    public ulong hCallerHandle;
    public uint fEmbedParent;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_ExpandRawHandleIntrinsic
{
    public Agnostic_CORINFO_RESOLVED_TOKENin ResolvedToken;
    public ulong hCallerHandle;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_GENERICHANDLE_RESULT
{
    public Agnostic_CORINFO_LOOKUP lookup;
    public ulong compileTimeHandle;
    public uint handleType;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetDelegateCtorIn
{
    public ulong methHnd;
    public ulong clsHnd;
    public ulong targetMethodHnd;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_DelegateCtorArgs
{
    public ulong pMethod;
    public ulong pArg3;
    public ulong pArg4;
    public ulong pArg5;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetDelegateCtorOut
{
    public Agnostic_DelegateCtorArgs CtorData;
    public ulong result;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_FindCallSiteSig
{
    public ulong module;
    public uint methTok;
    public ulong context;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetCastingHelper
{
    public ulong hClass;
    public uint fThrowing;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetClassModuleIdForStatics
{
    public ulong Module;
    public ulong pIndirection;
    public ulong result;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetIsClassInitedFlagAddress
{
    public Agnostic_CORINFO_CONST_LOOKUP addr;
    public uint offset;
    public uint result;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetStaticBaseAddress
{
    public Agnostic_CORINFO_CONST_LOOKUP addr;
    public uint result;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_PgoInstrumentationSchema
{
    public ulong Offset;
    public uint InstrumentationKind;
    public uint ILOffset;
    public uint Count;
    public uint Other;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_AllocPgoInstrumentationBySchema
{
    public ulong instrumentationDataAddress;
    public uint schema_index;
    public uint countSchemaItems;
    public uint result;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetPgoInstrumentationResults
{
    public uint countSchemaItems;
    public uint schema_index;
    public uint data_index;
    public uint dataByteCount;
    public uint result;
    public uint pgoSource;
    public uint dynamicPgo;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetProfilingHandle
{
    public uint bHookFunction;
    public ulong ProfilerHandle;
    public uint bIndirectedHandles;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetThreadLocalStaticBlocksInfo
{
    public Agnostic_CORINFO_CONST_LOOKUP tlsIndex;
    public ulong tlsGetAddrFtnPtr;
    public ulong tlsIndexObject;
    public ulong threadVarsSection;
    public uint offsetOfThreadLocalStoragePointer;
    public uint offsetOfMaxThreadStaticBlocks;
    public uint offsetOfThreadStaticBlocks;
    public uint offsetOfBaseOfThreadLocalData;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetThreadStaticInfo_NativeAOT
{
    public uint offsetOfThreadLocalStoragePointer;
    public Agnostic_CORINFO_CONST_LOOKUP tlsRootObject;
    public Agnostic_CORINFO_CONST_LOOKUP tlsIndexObject;
    public Agnostic_CORINFO_CONST_LOOKUP threadStaticBaseSlow;
    public Agnostic_CORINFO_CONST_LOOKUP tlsGetAddrFtnPtr;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetClassCtorInitializationInfo
{
    public Agnostic_CORINFO_CONST_LOOKUP addr;
    public Agnostic_CORINFO_CONST_LOOKUP targetAddr;
    public int size;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetThreadLocalFieldInfo
{
    public uint staticBlockIndex;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetTailCallHelpers
{
    public Agnostic_CORINFO_RESOLVED_TOKEN callToken;
    public Agnostic_CORINFO_SIG_INFO sig;
    public uint flags;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CORINFO_TAILCALL_HELPERS
{
    public byte result;  // bool
    public uint flags;
    public ulong hStoreArgs;
    public ulong hCallTarget;
    public ulong hDispatcher;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetArgClass_Value
{
    public ulong result;
    public uint exceptionCode;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetArgType_Value
{
    public ulong vcTypeRet;
    public uint result;
    public uint exceptionCode;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetExactClassesResult
{
    public int numClasses;
    public uint classes;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_ConfigIntInfo
{
    public uint nameIndex;
    public uint defaultValue;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetSystemVAmd64PassStructInRegisterDescriptor
{
    public uint passedInRegisters;
    public uint eightByteCount;
    public UintArray2 eightByteClassifications;
    public UintArray2 eightByteSizes;
    public UintArray2 eightByteOffsets;
    public uint result;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetSwiftLowering
{
    public uint byReference;
    public UintArray4 loweredElements;
    public UintArray4 offsets;
    public uint numLoweredElements;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetFpStructLowering
{
    public uint byIntegerCallConv;
    public UintArray2 loweredElements;
    public UintArray2 offsets;
    public uint numLoweredElements;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetContinuationTypeIn
{
    public ulong dataSize;
    public uint objRefs;
    public uint objRefsSize;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_GetWasmTypeSymbol
{
    public uint types;
    public uint typesSize;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_ResolveVirtualMethodKey
{
    public ulong virtualMethod;
    public ulong objClass;
    public ulong context;
    public uint pResolvedTokenVirtualMethodNonNull;
    public Agnostic_CORINFO_RESOLVED_TOKEN pResolvedTokenVirtualMethod;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_ResolveVirtualMethodResult
{
    public byte returnValue;  // bool
    public ulong devirtualizedMethod;
    public ulong tokenLookupContext;
    public uint detail;
    public Agnostic_CORINFO_RESOLVED_TOKEN resolvedTokenDevirtualizedMethod;
    public Agnostic_CORINFO_RESOLVED_TOKEN resolvedTokenDevirtualizedUnboxedMethod;
    public Agnostic_CORINFO_LOOKUP instParamLookup;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ResolveTokenValue
{
    public Agnostic_CORINFO_RESOLVED_TOKENout tokenOut;
    public uint exceptionCode;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GetTokenTypeAsHandleValue
{
    public ulong hMethod;
    public ulong hField;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GetVarArgsHandleValue
{
    public uint cbSig;
    public uint pSig_Index;
    public ulong scope;
    public uint token;
    public ulong methHnd;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GetCookieForInterpreterCalliSigValue
{
    public uint cbSig;
    public uint pSig_Index;
    public ulong scope;
    public uint token;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GetReadyToRunHelper_TOKENin
{
    public Agnostic_CORINFO_RESOLVED_TOKEN ResolvedToken;
    public uint id;
    public ulong callerHandle;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GetReadyToRunHelper_TOKENout
{
    public Agnostic_CORINFO_CONST_LOOKUP Lookup;
    public byte result;  // bool
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GetReadyToRunDelegateCtorHelper_TOKENIn
{
    public Agnostic_CORINFO_RESOLVED_TOKEN TargetMethod;
    public uint targetConstraint;
    public ulong delegateType;
    public ulong callerHandle;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_RecordRelocation
{
    public ulong location;
    public ulong target;
    public uint fRelocType;
    public uint addlDelta;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_AllocGCInfo
{
    public ulong size;
    public uint retval_offset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_AllocMemDetails
{
    public uint hotCodeSize;
    public uint coldCodeSize;
    public uint roDataSize;
    public uint xcptnsCount;
    public uint hotCodeBlock_offset;
    public uint coldCodeBlock_offset;
    public uint roDataBlock_offset;
    public ulong hotCodeBlock;
    public ulong coldCodeBlock;
    public ulong roDataBlock;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_AllocUnwindInfo
{
    public ulong pHotCode;
    public ulong pColdCode;
    public uint startOffset;
    public uint endOffset;
    public uint unwindSize;
    public uint pUnwindBlock_index;
    public uint funcKind;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_CompileMethodResults
{
    public ulong nativeEntry;
    public uint nativeSizeOfCode;
    public uint CorJitResult;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_ReportInliningDecision
{
    public ulong inlinerHnd;
    public ulong inlineeHnd;
    public uint inlineResult;
    public uint reason_offset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_ReportTailCallDecision
{
    public ulong callerHnd;
    public ulong calleeHnd;
    public uint fIsTailPrefix;
    public uint tailCallResult;
    public uint reason_index;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_ReserveUnwindInfo
{
    public uint isFunclet;
    public uint isColdCode;
    public uint unwindSize;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_SetBoundaries
{
    public ulong ftn;
    public uint cMap;
    public uint pMap_offset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_SetVars
{
    public ulong ftn;
    public uint cVars;
    public uint vars_offset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_SetPatchpointInfo
{
    public uint index;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_RecordCallSite
{
    public Agnostic_CORINFO_SIG_INFO callSig;
    public ulong methodHandle;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Agnostic_PrintResult
{
    public uint requiredBufferSize;
    public uint stringBuffer;
    public uint stringBufferSize;
}
[InlineArray(4)]
public struct Agnostic_CORINFO_HELPER_ARGArray4
{
    private Agnostic_CORINFO_HELPER_ARG _element0;
}

[InlineArray(2)]
public struct UintArray2
{
    private uint _element0;
}

[InlineArray(4)]
public struct UintArray4
{
    private uint _element0;
}

[InlineArray(4)]
public struct UlongArray4
{
    private ulong _element0;
}
