// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// agnostic.h - Definition of platform-agnostic data types used by SuperPMI.
//              MethodContext and CompileResult types use these.
//----------------------------------------------------------
#ifndef _Agnostic
#define _Agnostic

#pragma pack(push, 1)

struct Agnostic_CORINFO_SIG_INFO
{
    DWORD     callConv;
    DWORDLONG retTypeClass;
    DWORDLONG retTypeSigClass;
    DWORD     retType;
    DWORD     flags;
    DWORD     numArgs;
    DWORD     sigInst_classInstCount;
    DWORD     sigInst_classInst_Index;
    DWORD     sigInst_methInstCount;
    DWORD     sigInst_methInst_Index;
    DWORDLONG args;
    DWORD     pSig_Index;
    DWORD     cbSig;
    DWORDLONG methodSignature;
    DWORDLONG scope;
    DWORD     token;
};

struct Agnostic_CORINFO_METHOD_INFO
{
    DWORDLONG                 ftn;
    DWORDLONG                 scope;
    DWORD                     ILCode_offset;
    DWORD                     ILCodeSize;
    DWORD                     maxStack;
    DWORD                     EHcount;
    DWORD                     options;
    DWORD                     regionKind;
    Agnostic_CORINFO_SIG_INFO args;
    Agnostic_CORINFO_SIG_INFO locals;
};

struct Agnostic_CompileMethod
{
    Agnostic_CORINFO_METHOD_INFO info;
    DWORD                        flags;
};

struct Agnostic_InitClass
{
    DWORDLONG field;
    DWORDLONG method;
    DWORDLONG context;
};

struct DLDL
{
    DWORDLONG A;
    DWORDLONG B;
};

struct Agnostic_CanInline
{
    DWORD Restrictions;
    DWORD result;
    DWORD exceptionCode;
};

struct Agnostic_GetClassGClayout
{
    DWORD gcPtrs_Index;
    DWORD len;
    DWORD valCount;
};

struct DLD
{
    DWORDLONG A;
    DWORD     B;
};

struct DLDD
{
    DWORDLONG A;
    DWORD     B;
    DWORD     C;
};

struct Agnostic_CORINFO_METHODNAME_TOKENin
{
    DWORDLONG ftn;
    DWORD     className;
    DWORD     namespaceName;
    DWORD     enclosingClassName;
};

struct Agnostic_CORINFO_METHODNAME_TOKENout
{
    DWORD methodName;
    DWORD className;
    DWORD namespaceName;
    DWORD enclosingClassName;
};

struct Agnostic_CORINFO_RESOLVED_TOKENin
{
    DWORDLONG tokenContext;
    DWORDLONG tokenScope;
    DWORD     token;
    DWORD     tokenType;
};

struct Agnostic_CORINFO_RESOLVED_TOKENout
{
    DWORDLONG hClass;
    DWORDLONG hMethod;
    DWORDLONG hField;
    DWORD     pTypeSpec_Index;
    DWORD     cbTypeSpec;
    DWORD     pMethodSpec_Index;
    DWORD     cbMethodSpec;
};

struct Agnostic_GetArgType_Key
{
    // Partial CORINFO_SIG_INFO data
    DWORD     flags;
    DWORD     numArgs;
    DWORD     sigInst_classInstCount;
    DWORD     sigInst_classInst_Index;
    DWORD     sigInst_methInstCount;
    DWORD     sigInst_methInst_Index;
    DWORDLONG methodSignature;
    DWORDLONG scope;

    // Other getArgType() arguments
    DWORDLONG args;
};

struct Agnostic_GetArgClass_Key
{
    DWORD     sigInst_classInstCount;
    DWORD     sigInst_classInst_Index;
    DWORD     sigInst_methInstCount;
    DWORD     sigInst_methInst_Index;
    DWORDLONG methodSignature;
    DWORDLONG scope;
    DWORDLONG args;
};

struct Agnostic_GetBoundaries
{
    DWORD cILOffsets;
    DWORD pILOffset_offset;
    DWORD implicitBoundaries;
};

struct Agnostic_CORINFO_EE_INFO
{
    struct Agnostic_InlinedCallFrameInfo
    {
        DWORD size;
        DWORD offsetOfGSCookie;
        DWORD offsetOfFrameVptr;
        DWORD offsetOfFrameLink;
        DWORD offsetOfCallSiteSP;
        DWORD offsetOfCalleeSavedFP;
        DWORD offsetOfCallTarget;
        DWORD offsetOfReturnAddress;
    } inlinedCallFrameInfo;
    DWORD offsetOfThreadFrame;
    DWORD offsetOfGCState;
    DWORD offsetOfDelegateInstance;
    DWORD offsetOfDelegateFirstTarget;
    DWORD offsetOfWrapperDelegateIndirectCell;
    DWORD sizeOfReversePInvokeFrame;
    DWORD osPageSize;
    DWORD maxUncheckedOffsetForNullObject;
    DWORD targetAbi;
    DWORD osType;
};

struct Agnostic_GetOSRInfo
{
    DWORD index;
    unsigned ilOffset;
};

struct Agnostic_GetFieldAddress
{
    DWORDLONG ppIndirection;
    DWORDLONG fieldAddress;
    DWORD     fieldValue;
};

struct Agnostic_GetStaticFieldCurrentClass
{
    DWORDLONG classHandle;
    bool      isSpeculative;
};

struct Agnostic_CORINFO_RESOLVED_TOKEN
{
    Agnostic_CORINFO_RESOLVED_TOKENin inValue;
    Agnostic_CORINFO_RESOLVED_TOKENout outValue;
};

struct Agnostic_GetFieldInfo
{
    Agnostic_CORINFO_RESOLVED_TOKEN ResolvedToken;
    DWORDLONG                       callerHandle;
    DWORD                           flags;
};

struct Agnostic_CORINFO_HELPER_ARG
{
    DWORDLONG constant; // one view of a large union of ptr size
    DWORD     argType;
};

struct Agnostic_CORINFO_HELPER_DESC
{
    DWORD                       helperNum;
    DWORD                       numArgs;
    Agnostic_CORINFO_HELPER_ARG args[CORINFO_ACCESS_ALLOWED_MAX_ARGS];
};

struct Agnostic_CORINFO_CONST_LOOKUP
{
    DWORD     accessType;
    DWORDLONG handle; // actually a union of two pointer sized things
};

struct Agnostic_CORINFO_LOOKUP_KIND
{
    DWORD needsRuntimeLookup;
    DWORD runtimeLookupKind;
    WORD  runtimeLookupFlags;
};

struct Agnostic_CORINFO_RUNTIME_LOOKUP
{
    DWORDLONG signature;
    DWORD     helper;
    DWORD     indirections;
    DWORD     testForNull;
    DWORD     testForFixup;
    WORD      sizeOffset;
    DWORDLONG offsets[CORINFO_MAXINDIRECTIONS];
    DWORD     indirectFirstOffset;
    DWORD     indirectSecondOffset;
};

struct Agnostic_CORINFO_LOOKUP
{
    Agnostic_CORINFO_LOOKUP_KIND    lookupKind;
    Agnostic_CORINFO_RUNTIME_LOOKUP runtimeLookup; // This and constLookup actually a union, but with different
                                                   // layouts.. :-| copy the right one based on lookupKinds value
    Agnostic_CORINFO_CONST_LOOKUP constLookup;
};

struct Agnostic_CORINFO_FIELD_INFO
{
    DWORD                         fieldAccessor;
    DWORD                         fieldFlags;
    DWORD                         helper;
    DWORD                         offset;
    DWORD                         fieldType;
    DWORDLONG                     structType;
    DWORD                         accessAllowed;
    Agnostic_CORINFO_HELPER_DESC  accessCalloutHelper;
    Agnostic_CORINFO_CONST_LOOKUP fieldLookup;
};

struct DD
{
    DWORD A;
    DWORD B;
};

struct DDD
{
    DWORD A;
    DWORD B;
    DWORD C;
};

struct Agnostic_CanTailCall
{
    DWORDLONG callerHnd;
    DWORDLONG declaredCalleeHnd;
    DWORDLONG exactCalleeHnd;
    WORD      fIsTailPrefix;
};

struct Agnostic_Environment
{
    DWORD name_index;
    ;
    DWORD val_index;
};

struct Agnostic_GetCallInfo
{
    Agnostic_CORINFO_RESOLVED_TOKEN ResolvedToken;
    Agnostic_CORINFO_RESOLVED_TOKEN ConstrainedResolvedToken;
    DWORDLONG                       callerHandle;
    DWORD                           flags;
};

struct Agnostic_CORINFO_CALL_INFO
{
    DWORDLONG                     hMethod;
    DWORD                         methodFlags;
    DWORD                         classFlags;
    Agnostic_CORINFO_SIG_INFO     sig;
    DWORD                         verMethodFlags;
    Agnostic_CORINFO_SIG_INFO     verSig;
    DWORD                         accessAllowed;
    Agnostic_CORINFO_HELPER_DESC  callsiteCalloutHelper;
    DWORD                         thisTransform;
    DWORD                         kind;
    DWORD                         nullInstanceCheck;
    DWORDLONG                     contextHandle;
    DWORD                         exactContextNeedsRuntimeLookup;
    Agnostic_CORINFO_LOOKUP       stubLookup; // first view of union.  others are matching or subordinate
    Agnostic_CORINFO_CONST_LOOKUP instParamLookup;
    DWORD                         wrapperDelegateInvoke;
    DWORD                         exceptionCode;
};

struct Agnostic_GetMethodInfo
{
    Agnostic_CORINFO_METHOD_INFO info;
    bool                         result;
    DWORD                        exceptionCode;
};

struct Agnostic_FindSig
{
    DWORDLONG module;
    DWORD     sigTOK;
    DWORDLONG context;
};

struct MethodOrSigInfoValue
{
    DWORDLONG method;
    DWORD     pSig_Index;
    DWORD     cbSig;
    DWORDLONG scope;
};

struct Agnostic_CORINFO_EH_CLAUSE
{
    DWORD Flags;
    DWORD TryOffset;
    DWORD TryLength;
    DWORD HandlerOffset;
    DWORD HandlerLength;
    DWORD ClassToken; // first view of a two dword union
};

struct Agnostic_GetVars
{
    DWORD cVars;
    DWORD vars_offset;
    DWORD extendOthers;
};

struct Agnostic_CanAccessClassIn
{
    Agnostic_CORINFO_RESOLVED_TOKEN ResolvedToken;
    DWORDLONG                       callerHandle;
};

struct Agnostic_CanAccessClassOut
{
    Agnostic_CORINFO_HELPER_DESC AccessHelper;
    DWORD                        result;
};

struct Agnostic_AppendClassName
{
    DWORDLONG classHandle;
    DWORD     fNamespace;
    DWORD     fFullInst;
    DWORD     fAssembly;
};

struct Agnostic_CheckMethodModifier
{
    DWORDLONG hMethod;
    DWORD     modifier;
    DWORD     fOptional;
};

struct Agnostic_EmbedGenericHandle
{
    Agnostic_CORINFO_RESOLVED_TOKEN ResolvedToken;
    DWORD                           fEmbedParent;
};

struct Agnostic_CORINFO_GENERICHANDLE_RESULT
{
    Agnostic_CORINFO_LOOKUP lookup;
    DWORDLONG               compileTimeHandle;
    DWORD                   handleType;
};

struct Agnostic_GetDelegateCtorIn
{
    DWORDLONG methHnd;
    DWORDLONG clsHnd;
    DWORDLONG targetMethodHnd;
};

struct Agnostic_DelegateCtorArgs
{
    DWORDLONG pMethod;
    DWORDLONG pArg3;
    DWORDLONG pArg4;
    DWORDLONG pArg5;
};

struct Agnostic_GetDelegateCtorOut
{
    Agnostic_DelegateCtorArgs CtorData;
    DWORDLONG                 result;
};

struct Agnostic_FindCallSiteSig
{
    DWORDLONG module;
    DWORD     methTok;
    DWORDLONG context;
};

struct Agnostic_GetNewHelper
{
    DWORDLONG hClass;
    DWORDLONG callerHandle;
};

struct Agnostic_GetCastingHelper
{
    DWORDLONG hClass;
    DWORD     fThrowing;
};

struct Agnostic_GetClassModuleIdForStatics
{
    DWORDLONG Module;
    DWORDLONG pIndirection;
    DWORDLONG result;
};

struct Agnostic_IsCompatibleDelegate
{
    DWORDLONG objCls;
    DWORDLONG methodParentCls;
    DWORDLONG method;
    DWORDLONG delegateCls;
};

struct Agnostic_PgoInstrumentationSchema
{
    DWORDLONG Offset;          // size_t
    DWORD InstrumentationKind; // ICorJitInfo::PgoInstrumentationKind
    DWORD ILOffset;            // int32_t
    DWORD Count;               // int32_t
    DWORD Other;               // int32_t
};

struct Agnostic_AllocPgoInstrumentationBySchema
{
    DWORDLONG instrumentationDataAddress;
    DWORD schema_index;
    DWORD countSchemaItems;
    DWORD result;
};

struct Agnostic_GetPgoInstrumentationResults
{
    DWORD countSchemaItems;
    DWORD schema_index;
    DWORD data_index;
    DWORD dataByteCount;
    DWORD result;
};

struct Agnostic_GetLikelyClass
{
    DWORDLONG ftnHnd;
    DWORDLONG baseHnd;
    DWORD     ilOffset;
};

struct Agnostic_GetLikelyClassResult
{
    DWORDLONG classHnd;
    DWORD     likelihood;
    DWORD     numberOfClasses;
};

struct Agnostic_GetProfilingHandle
{
    DWORD     bHookFunction;
    DWORDLONG ProfilerHandle;
    DWORD     bIndirectedHandles;
};

struct Agnostic_GetTailCallHelpers
{
    Agnostic_CORINFO_RESOLVED_TOKEN callToken;
    Agnostic_CORINFO_SIG_INFO sig;
    DWORD flags;
};

struct Agnostic_CORINFO_TAILCALL_HELPERS
{
    bool result;
    DWORD flags;
    DWORDLONG hStoreArgs;
    DWORDLONG hCallTarget;
    DWORDLONG hDispatcher;
};

struct Agnostic_GetArgClass_Value
{
    DWORDLONG result;
    DWORD     exceptionCode;
};

struct Agnostic_GetArgType_Value
{
    DWORDLONG vcTypeRet;
    DWORD     result;
    DWORD     exceptionCode;
};

// Agnostic_ConfigIntInfo combines as a single key the name
// and defaultValue of a integer config query.
// Note: nameIndex is treated as a DWORD index to the name string.
struct Agnostic_ConfigIntInfo
{
    DWORD nameIndex;
    DWORD defaultValue;
};

// SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR
struct Agnostic_GetSystemVAmd64PassStructInRegisterDescriptor
{
    DWORD passedInRegisters; // Whether the struct is passable/passed (this includes struct returning) in registers.
    DWORD eightByteCount;    // Number of eightbytes for this struct.
    DWORD eightByteClassifications[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS]; // The eightbytes type
                                                                                           // classification.
    DWORD eightByteSizes[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS]; // The size of the eightbytes (an
                                                                                 // eightbyte could include padding.
                                                                                 // This represents the no padding
                                                                                 // size of the eightbyte).
    DWORD eightByteOffsets[CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS]; // The start offset of the
                                                                                   // eightbytes (in bytes).
    DWORD result;
};

struct Agnostic_ResolveVirtualMethodKey
{
    DWORDLONG virtualMethod;
    DWORDLONG objClass;
    DWORDLONG context;
};

struct Agnostic_ResolveVirtualMethodResult
{
    bool      returnValue;
    DWORDLONG devirtualizedMethod;
    bool      requiresInstMethodTableArg;
    DWORDLONG exactContext;
};

struct ResolveTokenValue
{
    Agnostic_CORINFO_RESOLVED_TOKENout tokenOut;
    DWORD                              exceptionCode;
};

struct TryResolveTokenValue
{
    Agnostic_CORINFO_RESOLVED_TOKENout tokenOut;
    DWORD                              success;
};

struct GetTokenTypeAsHandleValue
{
    DWORDLONG hMethod;
    DWORDLONG hField;
};

struct GetVarArgsHandleValue
{
    DWORD     cbSig;
    DWORD     pSig_Index;
    DWORDLONG scope;
    DWORD     token;
};

struct CanGetVarArgsHandleValue
{
    DWORDLONG scope;
    DWORD     token;
};

struct GetCookieForPInvokeCalliSigValue
{
    DWORD     cbSig;
    DWORD     pSig_Index;
    DWORDLONG scope;
    DWORD     token;
};

struct CanGetCookieForPInvokeCalliSigValue
{
    DWORDLONG scope;
    DWORD     token;
};

struct GetReadyToRunHelper_TOKENin
{
    Agnostic_CORINFO_RESOLVED_TOKEN ResolvedToken;
    Agnostic_CORINFO_LOOKUP_KIND    GenericLookupKind;
    DWORD                           id;
};

struct GetReadyToRunHelper_TOKENout
{
    Agnostic_CORINFO_CONST_LOOKUP Lookup;
    bool                          result;
};

struct GetReadyToRunDelegateCtorHelper_TOKENIn
{
    Agnostic_CORINFO_RESOLVED_TOKEN TargetMethod;
    DWORDLONG                       delegateType;
};

struct Agnostic_RecordRelocation
{
    DWORDLONG location;
    DWORDLONG target;
    DWORD     fRelocType;
    DWORD     slotNum;
    DWORD     addlDelta;
};

struct Capture_AllocMemDetails
{
    ULONG              hotCodeSize;
    ULONG              coldCodeSize;
    ULONG              roDataSize;
    ULONG              xcptnsCount;
    CorJitAllocMemFlag flag;
    void*              hotCodeBlock;
    void*              coldCodeBlock;
    void*              roDataBlock;
};

struct allocGCInfoDetails
{
    size_t size;
    void*  retval;
};

struct Agnostic_AddressMap
{
    DWORDLONG Address;
    DWORD     size;
};

struct Agnostic_AllocGCInfo
{
    DWORDLONG size;
    DWORD     retval_offset;
};

struct Agnostic_AllocMemDetails
{
    DWORD     hotCodeSize;
    DWORD     coldCodeSize;
    DWORD     roDataSize;
    DWORD     xcptnsCount;
    DWORD     flag;
    DWORD     hotCodeBlock_offset;
    DWORD     coldCodeBlock_offset;
    DWORD     roDataBlock_offset;
    DWORDLONG hotCodeBlock;
    DWORDLONG coldCodeBlock;
    DWORDLONG roDataBlock;
};

struct Agnostic_AllocUnwindInfo
{
    DWORDLONG pHotCode;
    DWORDLONG pColdCode;
    DWORD     startOffset;
    DWORD     endOffset;
    DWORD     unwindSize;
    DWORD     pUnwindBlock_index;
    DWORD     funcKind;
};

struct Agnostic_CompileMethodResults
{
    DWORDLONG nativeEntry;
    DWORD     nativeSizeOfCode;
    DWORD     CorJitResult;
};

struct Agnostic_ReportInliningDecision
{
    DWORDLONG inlinerHnd;
    DWORDLONG inlineeHnd;
    DWORD     inlineResult;
    DWORD     reason_offset;
};

struct Agnostic_ReportTailCallDecision
{
    DWORDLONG callerHnd;
    DWORDLONG calleeHnd;
    DWORD     fIsTailPrefix;
    DWORD     tailCallResult;
    DWORD     reason_index;
};

struct Agnostic_ReserveUnwindInfo
{
    DWORD isFunclet;
    DWORD isColdCode;
    DWORD unwindSize;
};

struct Agnostic_SetBoundaries
{
    DWORDLONG ftn;
    DWORD     cMap;
    DWORD     pMap_offset;
};

struct Agnostic_SetVars
{
    DWORDLONG ftn;
    DWORD     cVars;
    DWORD     vars_offset;
};

struct Agnostic_SetPatchpointInfo
{
    DWORD     index;
};

struct Agnostic_RecordCallSite
{
    Agnostic_CORINFO_SIG_INFO callSig;
    DWORDLONG                 methodHandle;
};

#pragma pack(pop)

#endif // _Agnostic
