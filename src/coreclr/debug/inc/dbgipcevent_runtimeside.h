// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This header contains the runtime-side (EE-only) IPC event types.
// These types are used exclusively by the execution engine (debugger EE)
// and should NOT be included by DBI or DAC-DBI code.
//

#ifndef _DBGIPCEVENT_RUNTIMESIDE_H_
#define _DBGIPCEVENT_RUNTIMESIDE_H_

#include "dbgipcevents.h"

//
// BasicTypeData_RuntimeSide and ExpandedTypeData_RuntimeSide
// hold data for each type sent across the
// boundary, whether it be a constructed type List<String> or a non-constructed
// type such as String, Foo or Object.
//
// Logically speaking BasicTypeData_RuntimeSide might just be "typeHandle", as
// we could then send further events to ask what the elementtype, typeToken and moduleToken
// are for the type handle.  But as
// nearly all types are non-generic we send across even the basic type information in
// the slightly expanded form shown below, sending the element type and the
// tokens with the type handle itself. The fields debuggerModuleToken, metadataToken and typeHandle
// are only used as follows:
//                                   elementType    debuggerModuleToken metadataToken      typeHandle
//     E_T_INT8    :                  E_T_INT8         No                     No              No
//     Boxed E_T_INT8:                E_T_CLASS        No                     No              No
//     E_T_CLASS, non-generic class:  E_T_CLASS       Yes                    Yes              No
//     E_T_VALUETYPE, non-generic:    E_T_VALUETYPE   Yes                    Yes              No
//     E_T_CLASS,     generic class:  E_T_CLASS       Yes                    Yes             Yes
//     E_T_VALUETYPE, generic class:  E_T_VALUETYPE   Yes                    Yes             Yes
//     E_T_BYREF                   :  E_T_BYREF        No                     No             Yes
//     E_T_PTR                     :  E_T_PTR          No                     No             Yes
//     E_T_ARRAY etc.              :  E_T_ARRAY        No                     No             Yes
//     E_T_FNPTR etc.              :  E_T_FNPTR        No                     No             Yes
// This allows us to always set "typeHandle" to NULL except when dealing with highly nested
// types or function-pointer types (the latter are too complexe to transfer over in one hit).
//

struct MSLAYOUT BasicTypeData_RuntimeSide
{
    CorElementType  elementType;
    mdTypeDef       metadataToken;
    VMPTR_Assembly  vmAssembly;
    VMPTR_TypeHandle vmTypeHandle;
};

// ExpandedTypeData_RuntimeSide contains more information showing further
// details for array types, byref types etc.
// Whenever you fetch type information from the left-side
// you get back one of these.  These in turn contain further
// BasicTypeData_RuntimeSide's and typeHandles which you can
// then query to get further information about the type parameters.
// This copes with the nested cases, e.g. jagged arrays,
// String ****, &(String*), Pair<String,Pair<String>>
// and so on.
//
// So this type information is not "fully expanded", it's just a little
// more detail then BasicTypeData_RuntimeSide.  For type
// instantiatons (e.g. List<int>) and
// function pointer types you will need to make further requests for
// information about the type parameters.
// For array types there is always only one type parameter so
// we include that as part of the expanded data.
//
//
struct MSLAYOUT ExpandedTypeData_RuntimeSide
{
    CorElementType  elementType; // Note this is _never_ E_T_VAR, E_T_WITH or E_T_MVAR
    union MSLAYOUT
    {
        // used for E_T_CLASS and E_T_VALUECLASS, E_T_PTR, E_T_BYREF etc.
        // For non-constructed E_T_CLASS or E_T_VALUECLASS the tokens will be set and the typeHandle will be NULL
        // For constructed E_T_CLASS or E_T_VALUECLASS the tokens will be set and the typeHandle will be non-NULL
        // For E_T_PTR etc. the tokens will be NULL and the typeHandle will be non-NULL.
        struct MSLAYOUT
         {
            mdTypeDef       metadataToken;
            VMPTR_Assembly  vmAssembly;
            VMPTR_TypeHandle typeHandle; // if non-null then further fetches will be needed to get type arguments
        } ClassTypeData;

        // used for E_T_PTR, E_T_BYREF etc.
        struct MSLAYOUT
         {
            BasicTypeData_RuntimeSide unaryTypeArg;  // used only when sending back to debugger
        } UnaryTypeData;


        // used for E_T_ARRAY etc.
        struct MSLAYOUT
        {
          BasicTypeData_RuntimeSide arrayTypeArg; // used only when sending back to debugger
            DWORD           arrayRank;
        } ArrayTypeData;

        // used for E_T_FNPTR
        struct MSLAYOUT
         {
            VMPTR_TypeHandle typeHandle; // if non-null then further fetches needed to get type arguments
        } NaryTypeData;

    };
};

// DebuggerIPCE_TypeArgData_RuntimeSide is used when sending type arguments
// across to a funceval.  It contains the ExpandedTypeData_RuntimeSide describing the
// essence of the type, but the typeHandle and other
// BasicTypeData fields should be zero and will be ignored.
// The ExpandedTypeData_RuntimeSide is then followed
// by the required number of type arguments, each of which
// will be a further DebuggerIPCE_TypeArgData_RuntimeSide record in the stream of
// flattened type argument data.
struct MSLAYOUT DebuggerIPCE_TypeArgData_RuntimeSide
{
    ExpandedTypeData_RuntimeSide  data;
    unsigned int                   numTypeArgs; // number of immediate children on the type tree
};

//
// DebuggerIPCE_FuncEvalArgData_RuntimeSide holds data for each argument to a
// function evaluation.
//
struct MSLAYOUT DebuggerIPCE_FuncEvalArgData_RuntimeSide
{
    RemoteAddress     argHome;  // enregistered variable home
    void             *argAddr;  // address if not enregistered
    CorElementType    argElementType;
    unsigned int      fullArgTypeNodeCount; // Pointer to LS (DebuggerIPCE_TypeArgData_RuntimeSide *) buffer holding full description of the argument type (if needed - only needed for struct types)
    void             *fullArgType; // Pointer to LS (DebuggerIPCE_TypeArgData_RuntimeSide *) buffer holding full description of the argument type (if needed - only needed for struct types)
    BYTE              argLiteralData[8]; // copy of generic value data
    bool              argIsLiteral; // true if value is in argLiteralData
    bool              argIsHandleValue; // true if argAddr is OBJECTHANDLE
};


//
// DebuggerIPCE_FuncEvalInfo_RuntimeSide holds info necessary to setup a func eval
// operation.
//
struct MSLAYOUT DebuggerIPCE_FuncEvalInfo_RuntimeSide
{
    VMPTR_Thread               vmThreadToken;
    DebuggerIPCE_FuncEvalType  funcEvalType;
    mdMethodDef                funcMetadataToken;
    mdTypeDef                  funcClassMetadataToken;
    VMPTR_Assembly             vmAssembly;
    RSPTR_CORDBEVAL            funcEvalKey;
    bool                       evalDuringException;

    unsigned int               argCount;
    unsigned int               genericArgsCount;
    unsigned int               genericArgsNodeCount;

    SIZE_T                     stringSize;

    SIZE_T                     arrayRank;
};

//
// Event structure that is passed between the Runtime Controller and the
// Debugger Interface. Some types of events are a fixed size and have
// entries in the main union, while others are variable length and have
// more specialized data structures that are attached to the end of this
// structure.
//
struct MSLAYOUT DebuggerIPCEvent_RuntimeSide
{
    DebuggerIPCEventType    type;
    DWORD             processId;
    DWORD             threadId;
    VMPTR_AppDomain   vmAppDomain;
    VMPTR_Thread      vmThread;

    HRESULT           hr;
    bool              replyRequired;
    bool              asyncSend;

    union MSLAYOUT
    {
        struct MSLAYOUT
        {
            // Module whose metadata is being updated
            // This tells the RS that the metadata for that module has become invalid.
            VMPTR_Assembly vmAssembly;

        } MetadataUpdateData;

        struct MSLAYOUT
        {
            // Handle to CLR's internal appdomain object.
            VMPTR_AppDomain vmAppDomain;
        } AppDomainData;

        struct MSLAYOUT
        {
            VMPTR_Assembly vmAssembly;
        } AssemblyData;

        // Debug event that a module has been loaded
        struct MSLAYOUT
        {
            // Module that was just loaded.
            VMPTR_Assembly vmAssembly;
        }LoadModuleData;


        struct MSLAYOUT
        {
            VMPTR_Assembly vmAssembly;
        } UnloadModuleData;


        // The given module's pdb has been updated.
        // Queury PDB from OOP
        struct MSLAYOUT
        {
            VMPTR_Assembly vmAssembly;
        } UpdateModuleSymsData;

        struct MSLAYOUT
        {
            LSPTR_BREAKPOINT breakpointToken;
            mdMethodDef  funcMetadataToken;
            VMPTR_Assembly vmAssembly;
            bool         isIL;
            SIZE_T       offset;
            SIZE_T       encVersion;
            LSPTR_METHODDESC  nativeCodeMethodDescToken; // points to the MethodDesc if !isIL
            CORDB_ADDRESS codeStartAddress;
        } BreakpointData;

        struct MSLAYOUT
        {
            mdMethodDef funcMetadataToken;
            VMPTR_Module pModule;
        } DisableOptData;

        struct MSLAYOUT
        {
            BOOL enableEvents;
            VMPTR_Object vmObj;
        } ForceCatchHandlerFoundData;

        struct MSLAYOUT
        {
            VMPTR_Module vmModule;
            mdTypeDef    classMetadataToken;
            BOOL Enabled;
        } CustomNotificationData;

        struct MSLAYOUT
        {
            LSPTR_BREAKPOINT breakpointToken;
        } BreakpointSetErrorData;

        struct MSLAYOUT
        {
#ifdef FEATURE_DATABREAKPOINT
            CONTEXT context;
#else
            int dummy;
#endif
        } DataBreakpointData;

        struct MSLAYOUT
        {
            LSPTR_STEPPER        stepperToken;
            VMPTR_Thread         vmThreadToken;
            FramePointer         frameToken;
            bool                 stepIn;
            bool                 rangeIL;
            bool                 IsJMCStop;
            unsigned int         totalRangeCount;
            CorDebugStepReason   reason;
            CorDebugUnmappedStop rgfMappingStop;
            CorDebugIntercept    rgfInterceptStop;
            unsigned int         rangeCount;
            COR_DEBUG_STEP_RANGE range[5]; //note that this is an array
        } StepData;

        // Allocate memory on the left-side
        struct MSLAYOUT
        {
            ULONG      bufSize;             // number of bytes to allocate
        } GetBuffer;

        // Memory allocated on the left-side
        struct MSLAYOUT
        {
            void        *pBuffer;           // LS pointer to the buffer allocated
            HRESULT     hr;                 // success / failure
        } GetBufferResult;

        // Free a buffer allocated on the left-side with GetBuffer
        struct MSLAYOUT
        {
            void        *pBuffer;           // Pointer previously returned in GetBufferResult
        } ReleaseBuffer;

        struct MSLAYOUT
        {
            HRESULT     hr;
        } ReleaseBufferResult;

        // Apply an EnC edit
        struct MSLAYOUT
        {
            VMPTR_Assembly vmAssembly;      // Module to edit
            DWORD cbDeltaMetadata;              // size of blob pointed to by pDeltaMetadata
            CORDB_ADDRESS pDeltaMetadata;       // pointer to delta metadata in debuggee
                                                // it's the RS's responsibility to allocate and free
                                                // this (and pDeltaIL) using GetBuffer / ReleaseBuffer
            CORDB_ADDRESS pDeltaIL;             // pointer to delta IL in debugee
            DWORD cbDeltaIL;                    // size of blob pointed to by pDeltaIL
        } ApplyChanges;

        struct MSLAYOUT
        {
            HRESULT hr;
        } ApplyChangesResult;

        struct MSLAYOUT
        {
            mdTypeDef   classMetadataToken;
            VMPTR_Assembly vmAssembly;
        } LoadClass;

        struct MSLAYOUT
        {
            mdTypeDef   classMetadataToken;
            VMPTR_Assembly vmAssembly;
        } UnloadClass;

        struct MSLAYOUT
        {
            VMPTR_Assembly vmAssembly;
            bool  flag;
        } SetClassLoad;

        struct MSLAYOUT
        {
            VMPTR_OBJECTHANDLE vmExceptionHandle;
            bool        firstChance;
            bool        continuable;
        } Exception;

        struct MSLAYOUT
        {
            void        *address;
        } IsTransitionStub;

        struct MSLAYOUT
        {
            bool        isStub;
        } IsTransitionStubResult;

        struct MSLAYOUT
        {
            CORDB_ADDRESS    startAddress;
            bool             fCanSetIPOnly;
            VMPTR_Thread     vmThreadToken;
            VMPTR_Assembly vmAssembly;
            mdMethodDef      mdMethod;
            VMPTR_MethodDesc vmMethodDesc;
            SIZE_T           offset;
            bool             fIsIL;
        } SetIP; // this is also used for CanSetIP

        struct MSLAYOUT
        {
            int iLevel;

            CORDB_ADDRESS szCategory;
            CORDB_ADDRESS szContent;
        } FirstLogMessage;

        struct MSLAYOUT
        {
            int iLevel;
        } LogSwitchSettingMessage;

        // information needed to send to the RS as part of a custom notification from the target
        struct MSLAYOUT
        {
            // assembly for the domain in which the notification occurred
            VMPTR_Assembly vmAssembly;

            // metadata token for the type of the CustomNotification object's type
            mdTypeDef    classToken;
        } CustomNotification;

        struct MSLAYOUT
        {
            VMPTR_Thread vmThreadToken;
            CorDebugThreadState debugState;
        } SetAllDebugState;

        DebuggerIPCE_FuncEvalInfo_RuntimeSide FuncEval;

        struct MSLAYOUT
        {
            CORDB_ADDRESS argDataArea;
            LSPTR_DEBUGGEREVAL debuggerEvalKey;
        } FuncEvalSetupComplete;

        struct MSLAYOUT
        {
            RSPTR_CORDBEVAL funcEvalKey;
            bool            successful;
            bool            aborted;
            void           *resultAddr;

            // AppDomain that the result is in.
            VMPTR_AppDomain vmAppDomain;

            VMPTR_OBJECTHANDLE vmObjectHandle;
            ExpandedTypeData_RuntimeSide resultType;
        } FuncEvalComplete;

        struct MSLAYOUT
        {
            LSPTR_DEBUGGEREVAL debuggerEvalKey;
        } FuncEvalAbort;

        struct MSLAYOUT
        {
            LSPTR_DEBUGGEREVAL debuggerEvalKey;
        } FuncEvalRudeAbort;

        struct MSLAYOUT
        {
            LSPTR_DEBUGGEREVAL debuggerEvalKey;
        } FuncEvalCleanup;

        struct MSLAYOUT
        {
            void           *objectRefAddress;
            VMPTR_OBJECTHANDLE vmObjectHandle;
            void           *newReference;
        } SetReference;

        struct MSLAYOUT
        {
            NameChangeType  eventType;
            VMPTR_AppDomain vmAppDomain;
            VMPTR_Thread    vmThread;
        } NameChange;

        // EnC Remap opportunity
        struct MSLAYOUT
        {
            VMPTR_Assembly vmAssembly;
            mdMethodDef funcMetadataToken ;        // methodDef of function with remap opportunity
            SIZE_T          currentVersionNumber;  // version currently executing
            SIZE_T          resumeVersionNumber;   // latest version
            SIZE_T          currentILOffset;       // the IL offset of the current IP
            SIZE_T          *resumeILOffset;       // pointer into left-side where an offset to resume
                                                   // to should be written if remap is desired.
        } EnCRemap;

        // EnC Remap has taken place
        struct MSLAYOUT
        {
            VMPTR_Assembly vmAssembly;
            mdMethodDef funcMetadataToken;         // methodDef of function that was remapped
        } EnCRemapComplete;

        // Notification that the LS is about to update a CLR data structure to account for a
        // specific edit made by EnC (function add/update or field add).
        struct MSLAYOUT
        {
            VMPTR_Assembly vmAssembly;
            mdToken         memberMetadataToken;   // Either a methodDef token indicating the function that
                                                   // was updated/added, or a fieldDef token indicating the
                                                   // field which was added.
            mdTypeDef       classMetadataToken;    // TypeDef token of the class in which the update was made
            SIZE_T          newVersionNumber;      // The new function/module version
        } EnCUpdate;

        struct MSLAYOUT
        {
            void      *oldData;
            void      *newData;
            BasicTypeData_RuntimeSide type;
        } SetValueClass;


        // Event used to tell LS if a single function is user or non-user code.
        // Same structure used to get function status.
        // @todo - Perhaps we can bundle these up so we can set multiple funcs w/ 1 event?
        struct MSLAYOUT
        {
            VMPTR_Assembly vmAssembly;
            mdMethodDef     funcMetadataToken;
            DWORD           dwStatus;
        } SetJMCFunctionStatus;

        struct MSLAYOUT
        {
            void               *objectToken;
            CorDebugHandleType handleType;
        } CreateHandle;

        struct MSLAYOUT
        {
            VMPTR_OBJECTHANDLE vmObjectHandle;
        } CreateHandleResult;

        // used in DB_IPCE_DISPOSE_HANDLE event
        struct MSLAYOUT
        {
            VMPTR_OBJECTHANDLE vmObjectHandle;
            CorDebugHandleType handleType;
        } DisposeHandle;

        struct MSLAYOUT
        {
            FramePointer                  framePointer;
            SIZE_T                        nOffset;
            CorDebugExceptionCallbackType eventType;
            DWORD                         dwFlags;
            VMPTR_OBJECTHANDLE            vmExceptionHandle;
        } ExceptionCallback2;

        struct MSLAYOUT
        {
            CorDebugExceptionUnwindCallbackType eventType;
            DWORD                               dwFlags;
        } ExceptionUnwind;

        struct MSLAYOUT
        {
            VMPTR_Thread vmThreadToken;
            FramePointer frameToken;
        } InterceptException;

        struct MSLAYOUT
        {
            VMPTR_Module vmModule;
            void * pMetadataStart;
            ULONG nMetadataSize;
        } MetadataUpdateRequest;
    };
};

// When using a network transport rather than shared memory buffers CorDBIPC_BUFFER_SIZE is the upper bound
// for a single DebuggerIPCEvent_DebuggerSide structure. This now relates to the maximal size of a network message and is
// orthogonal to the host's page size. Round the buffer size up to a multiple of 8 since MSVC seems more
// aggressive in this regard than gcc.
#define CorDBIPC_TRANSPORT_BUFFER_SIZE (((sizeof(DebuggerIPCEvent_RuntimeSide) + 7) / 8) * 8)

// A DebuggerIPCEvent_RuntimeSide must fit in the send & receive buffers, which are CorDBIPC_BUFFER_SIZE bytes.
static_assert(sizeof(DebuggerIPCEvent_RuntimeSide) <= CorDBIPC_BUFFER_SIZE);
static_assert(CorDBIPC_TRANSPORT_BUFFER_SIZE <= CorDBIPC_BUFFER_SIZE);

#endif /* _DBGIPCEVENT_RUNTIMESIDE_H_ */
