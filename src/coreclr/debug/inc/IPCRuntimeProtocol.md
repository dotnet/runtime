### List of Runtime IPC protocol changes

| Version | Changes |
| ------- | ------- |
| 2       |         |

### Current runtime IPC events

#### Type wrappers

| Syntax | Meaning |
| ------ | ------- |
| `Portable<T>` | Stores `T` in the debugger protocol's canonical little-endian representation. Reads and assignments convert between that representation and the host's native byte order, so the same IPC structure can be transferred between hosts with different endianness. On little-endian hosts the conversion is a no-op. |
| `VMPTR_*` | A strongly typed wrapper around a `CORDB_ADDRESS` in the debuggee runtime's address space. The runtime can create or unwrap the target pointer, and the debugger treats it as an opaque handle. A `VMPTR_*` does not transfer the referenced object or give the debugger a directly dereferenceable host pointer. |

All events use the common `DebuggerIPCEvent` header layout:
`Portable<DebuggerIPCEventType> type`, `Portable<DWORD> processId`,
`Portable<DWORD> threadId`, `Portable<VMPTR_AppDomain> vmAppDomain`,
`Portable<VMPTR_Thread> vmThread`, `Portable<HRESULT> hr`,
`Portable<bool> replyRequired`, `Portable<bool> asyncSend`.

The table lists the active events and their event-specific payloads.

| Event | Direction | Payload | Payload layout |
| ----- | --------- | ------- | -------------- |
| `DB_IPCE_BREAKPOINT` | Runtime -> debugger notification | `BreakpointData` | `LSPTR_BREAKPOINT breakpointToken, Portable<mdMethodDef> funcMetadataToken, Portable<VMPTR_Assembly> vmAssembly, Portable<bool> isIL, Portable<UINT> offset, Portable<UINT> encVersion, LSPTR_METHODDESC nativeCodeMethodDescToken, Portable<CORDB_ADDRESS> codeStartAddress` |
| `DB_IPCE_SYNC_COMPLETE` | Runtime -> debugger notification | None | Common header only |
| `DB_IPCE_THREAD_ATTACH` | Runtime -> debugger notification | None | Common header only |
| `DB_IPCE_THREAD_DETACH` | Runtime -> debugger notification | None | Common header only |
| `DB_IPCE_LOAD_MODULE` | Runtime -> debugger notification | `LoadModuleData` | `Portable<VMPTR_Assembly> vmAssembly` |
| `DB_IPCE_UNLOAD_MODULE` | Runtime -> debugger notification | `UnloadModuleData` | `Portable<VMPTR_Assembly> vmAssembly, LSPTR_ASSEMBLY debuggerAssemblyToken` |
| `DB_IPCE_LOAD_CLASS` | Runtime -> debugger notification | `LoadClass` | `Portable<mdTypeDef> classMetadataToken, Portable<VMPTR_Assembly> vmAssembly, LSPTR_ASSEMBLY classDebuggerAssemblyToken` |
| `DB_IPCE_UNLOAD_CLASS` | Runtime -> debugger notification | `UnloadClass` | `Portable<mdTypeDef> classMetadataToken, Portable<VMPTR_Assembly> vmAssembly, LSPTR_ASSEMBLY classDebuggerAssemblyToken` |
| `DB_IPCE_EXCEPTION` | Runtime -> debugger notification | `Exception` | `Portable<VMPTR_OBJECTHANDLE> vmExceptionHandle, Portable<bool> firstChance, Portable<bool> continuable` |
| `DB_IPCE_UNHANDLED_EXCEPTION` | Runtime -> debugger notification | None | Common header only |
| `DB_IPCE_BREAKPOINT_ADD_RESULT` | Runtime -> debugger response | `BreakpointData` | `LSPTR_BREAKPOINT breakpointToken, Portable<mdMethodDef> funcMetadataToken, Portable<VMPTR_Assembly> vmAssembly, Portable<bool> isIL, Portable<UINT> offset, Portable<UINT> encVersion, LSPTR_METHODDESC nativeCodeMethodDescToken, Portable<CORDB_ADDRESS> codeStartAddress` |
| `DB_IPCE_STEP_RESULT` | Runtime -> debugger response | `StepData` | `LSPTR_STEPPER stepperToken, Portable<VMPTR_Thread> vmThreadToken, Portable<CORDB_ADDRESS> frameToken, Portable<bool> stepIn, Portable<bool> rangeIL, Portable<bool> IsJMCStop, Portable<UINT> totalRangeCount, Portable<CorDebugStepReason> reason, Portable<CorDebugUnmappedStop> rgfMappingStop, Portable<CorDebugIntercept> rgfInterceptStop, Portable<UINT> rangeCount, COR_DEBUG_STEP_RANGE range` |
| `DB_IPCE_STEP_COMPLETE` | Runtime -> debugger notification | `StepData` | `LSPTR_STEPPER stepperToken, Portable<VMPTR_Thread> vmThreadToken, Portable<CORDB_ADDRESS> frameToken, Portable<bool> stepIn, Portable<bool> rangeIL, Portable<bool> IsJMCStop, Portable<UINT> totalRangeCount, Portable<CorDebugStepReason> reason, Portable<CorDebugUnmappedStop> rgfMappingStop, Portable<CorDebugIntercept> rgfInterceptStop, Portable<UINT> rangeCount, COR_DEBUG_STEP_RANGE range` |
| `DB_IPCE_BREAKPOINT_REMOVE_RESULT` | Runtime -> debugger response | `BreakpointData` | `LSPTR_BREAKPOINT breakpointToken, Portable<mdMethodDef> funcMetadataToken, Portable<VMPTR_Assembly> vmAssembly, Portable<bool> isIL, Portable<UINT> offset, Portable<UINT> encVersion, LSPTR_METHODDESC nativeCodeMethodDescToken, Portable<CORDB_ADDRESS> codeStartAddress` |
| `DB_IPCE_GET_BUFFER_RESULT` | Runtime -> debugger response | `GetBufferResult` | `Portable<CORDB_ADDRESS> pBuffer, Portable<HRESULT> hr` |
| `DB_IPCE_RELEASE_BUFFER_RESULT` | Runtime -> debugger response | `ReleaseBufferResult` | `Portable<HRESULT> hr` |
| `DB_IPCE_ENC_ADD_FIELD` | Runtime -> debugger notification | `EnCUpdate` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdToken> memberMetadataToken, Portable<mdTypeDef> classMetadataToken, Portable<ULONG64> newVersionNumber` |
| `DB_IPCE_APPLY_CHANGES_RESULT` | Runtime -> debugger response | `ApplyChangesResult` | `Portable<HRESULT> hr` |
| `DB_IPCE_CUSTOM_NOTIFICATION` | Runtime -> debugger notification | `CustomNotification` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdTypeDef> classToken` |
| `DB_IPCE_USER_BREAKPOINT` | Runtime -> debugger notification | None | Common header only |
| `DB_IPCE_FIRST_LOG_MESSAGE` | Runtime -> debugger notification | `FirstLogMessage` | `Portable<int> iLevel, Portable<CORDB_ADDRESS> szCategory, Portable<ULONG> cchCategory, Portable<CORDB_ADDRESS> szContent, Portable<ULONG> cchContent` |
| `DB_IPCE_CREATE_APP_DOMAIN` | Runtime -> debugger notification | `AppDomainData` | `Portable<VMPTR_AppDomain> vmAppDomain` |
| `DB_IPCE_LOAD_ASSEMBLY` | Runtime -> debugger notification | `AssemblyData` | `Portable<VMPTR_Assembly> vmAssembly` |
| `DB_IPCE_UNLOAD_ASSEMBLY` | Runtime -> debugger notification | `AssemblyData` | `Portable<VMPTR_Assembly> vmAssembly` |
| `DB_IPCE_SET_DEBUG_STATE_RESULT` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` |
| `DB_IPCE_FUNC_EVAL_SETUP_RESULT` | Runtime -> debugger response | `FuncEvalSetupComplete` | `Portable<CORDB_ADDRESS> argDataArea, LSPTR_DEBUGGEREVAL debuggerEvalKey` |
| `DB_IPCE_FUNC_EVAL_COMPLETE` | Runtime -> debugger notification | `FuncEvalComplete` | `RSPTR_CORDBEVAL funcEvalKey, Portable<bool> successful, Portable<bool> aborted, Portable<CORDB_ADDRESS> resultAddr, Portable<VMPTR_AppDomain> vmAppDomain, Portable<VMPTR_OBJECTHANDLE> vmObjectHandle, DebuggerIPCE_ExpandedTypeData resultType` |
| `DB_IPCE_SET_REFERENCE_RESULT` | Runtime -> debugger response | `SetReference` | `Portable<CORDB_ADDRESS> objectRefAddress, Portable<VMPTR_OBJECTHANDLE> vmObjectHandle, Portable<CORDB_ADDRESS> newReference` |
| `DB_IPCE_FUNC_EVAL_ABORT_RESULT` | Runtime -> debugger response | `FuncEvalAbort` | `LSPTR_DEBUGGEREVAL debuggerEvalKey` |
| `DB_IPCE_NAME_CHANGE` | Runtime -> debugger notification | `NameChange` | `Portable<NameChangeType> eventType, Portable<VMPTR_AppDomain> vmAppDomain, Portable<VMPTR_Thread> vmThread` |
| `DB_IPCE_UPDATE_MODULE_SYMS` | Runtime -> debugger notification | `UpdateModuleSymsData` | `Portable<VMPTR_Assembly> vmAssembly` |
| `DB_IPCE_CONTROL_C_EVENT` | Runtime -> debugger notification | None | Common header only |
| `DB_IPCE_FUNC_EVAL_CLEANUP_RESULT` | Runtime -> debugger response | `FuncEvalCleanup` | `LSPTR_DEBUGGEREVAL debuggerEvalKey` |
| `DB_IPCE_ENC_REMAP` | Runtime -> debugger notification | `EnCRemap` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdMethodDef> funcMetadataToken, Portable<ULONG64> currentVersionNumber, Portable<ULONG64> resumeVersionNumber, Portable<ULONG64> currentILOffset, Portable<CORDB_ADDRESS> resumeILOffset` |
| `DB_IPCE_SET_VALUE_CLASS_RESULT` | Runtime -> debugger response | `SetValueClass` | `Portable<CORDB_ADDRESS> oldData, Portable<CORDB_ADDRESS> newData, DebuggerIPCE_BasicTypeData type` |
| `DB_IPCE_BREAKPOINT_SET_ERROR` | Runtime -> debugger notification | `BreakpointSetErrorData` | `LSPTR_BREAKPOINT breakpointToken` |
| `DB_IPCE_ENC_UPDATE_FUNCTION` | Runtime -> debugger notification | `EnCUpdate` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdToken> memberMetadataToken, Portable<mdTypeDef> classMetadataToken, Portable<ULONG64> newVersionNumber` |
| `DB_IPCE_SET_METHOD_JMC_STATUS_RESULT` | Runtime -> debugger response | `SetJMCFunctionStatus` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdMethodDef> funcMetadataToken, Portable<DWORD> dwStatus` |
| `DB_IPCE_GET_METHOD_JMC_STATUS_RESULT` | Runtime -> debugger response | `SetJMCFunctionStatus` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdMethodDef> funcMetadataToken, Portable<DWORD> dwStatus` |
| `DB_IPCE_SET_MODULE_JMC_STATUS_RESULT` | Runtime -> debugger response | `SetJMCFunctionStatus` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdMethodDef> funcMetadataToken, Portable<DWORD> dwStatus` |
| `DB_IPCE_FUNC_EVAL_RUDE_ABORT_RESULT` | Runtime -> debugger response | `FuncEvalRudeAbort` | `LSPTR_DEBUGGEREVAL debuggerEvalKey` |
| `DB_IPCE_EXCEPTION_CALLBACK2` | Runtime -> debugger notification | `ExceptionCallback2` | `Portable<CORDB_ADDRESS> framePointer, Portable<UINT> nOffset, Portable<CorDebugExceptionCallbackType> eventType, Portable<DWORD> dwFlags, Portable<VMPTR_OBJECTHANDLE> vmExceptionHandle` |
| `DB_IPCE_EXCEPTION_UNWIND` | Runtime -> debugger notification | `ExceptionUnwind` | `Portable<CorDebugExceptionUnwindCallbackType> eventType, Portable<DWORD> dwFlags` |
| `DB_IPCE_INTERCEPT_EXCEPTION_RESULT` | Runtime -> debugger response | `InterceptException` | `Portable<VMPTR_Thread> vmThreadToken, Portable<CORDB_ADDRESS> frameToken` |
| `DB_IPCE_CREATE_HANDLE_RESULT` | Runtime -> debugger response | `CreateHandleResult` | `Portable<VMPTR_OBJECTHANDLE> vmObjectHandle` |
| `DB_IPCE_INTERCEPT_EXCEPTION_COMPLETE` | Runtime -> debugger notification | None | Common header only |
| `DB_IPCE_ENC_REMAP_COMPLETE` | Runtime -> debugger notification | `EnCRemapComplete` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdMethodDef> funcMetadataToken` |
| `DB_IPCE_CREATE_PROCESS` | Runtime -> debugger notification | None | Common header only |
| `DB_IPCE_ENC_ADD_FUNCTION` | Runtime -> debugger notification | `EnCUpdate` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdToken> memberMetadataToken, Portable<mdTypeDef> classMetadataToken, Portable<ULONG64> newVersionNumber` |
| `DB_IPCE_LEFTSIDE_STARTUP` | Runtime -> debugger notification | None | Common header only |
| `DB_IPCE_METADATA_UPDATE` | Runtime -> debugger notification | `MetadataUpdateData` | `Portable<VMPTR_Assembly> vmAssembly` |
| `DB_IPCE_RESOLVE_UPDATE_METADATA_1_RESULT` | Runtime -> debugger response | `MetadataUpdateRequest` | `Portable<VMPTR_Module> vmModule, Portable<CORDB_ADDRESS> pMetadataStart, Portable<ULONG> nMetadataSize` |
| `DB_IPCE_RESOLVE_UPDATE_METADATA_2_RESULT` | Runtime -> debugger response | `MetadataUpdateRequest` | `Portable<VMPTR_Module> vmModule, Portable<CORDB_ADDRESS> pMetadataStart, Portable<ULONG> nMetadataSize` |
| `DB_IPCE_DATA_BREAKPOINT` | Runtime -> debugger notification | `DataBreakpointData` | `Portable<UINT> contextSize, CONTEXT context` |
| `DB_IPCE_BEFORE_GARBAGE_COLLECTION` | Runtime -> debugger notification | None | Common header only |
| `DB_IPCE_AFTER_GARBAGE_COLLECTION` | Runtime -> debugger notification | None | Common header only |
| `DB_IPCE_DISABLE_OPTS_RESULT` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` |
| `DB_IPCE_CATCH_HANDLER_FOUND_RESULT` | Runtime -> debugger response | `ForceCatchHandlerFoundData` | `Portable<bool> enableEvents, Portable<VMPTR_Object> vmObj` |
| `DB_IPCE_SET_ENABLE_CUSTOM_NOTIFICATION_RESULT` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` |
| `DB_IPCE_ASYNC_BREAK` | Debugger -> runtime | None | Common header only |
| `DB_IPCE_CONTINUE` | Debugger -> runtime | None | Common header only |
| `DB_IPCE_LIST_THREADS` | Debugger -> runtime | None | Common header only |
| `DB_IPCE_SET_IP` | Debugger -> runtime; runtime -> debugger response | `SetIP` | `Portable<CORDB_ADDRESS> startAddress, Portable<bool> fCanSetIPOnly, Portable<VMPTR_Thread> vmThreadToken, Portable<VMPTR_Assembly> vmAssembly, Portable<mdMethodDef> mdMethod, Portable<VMPTR_MethodDesc> vmMethodDesc, Portable<ULONG64> offset, Portable<bool> fIsIL` |
| `DB_IPCE_SUSPEND_THREAD` | Debugger -> runtime | None | Common header only |
| `DB_IPCE_RESUME_THREAD` | Debugger -> runtime | None | Common header only |
| `DB_IPCE_BREAKPOINT_ADD` | Debugger -> runtime | `BreakpointData` | `LSPTR_BREAKPOINT breakpointToken, Portable<mdMethodDef> funcMetadataToken, Portable<VMPTR_Assembly> vmAssembly, Portable<bool> isIL, Portable<UINT> offset, Portable<UINT> encVersion, LSPTR_METHODDESC nativeCodeMethodDescToken, Portable<CORDB_ADDRESS> codeStartAddress` |
| `DB_IPCE_BREAKPOINT_REMOVE` | Debugger -> runtime | `BreakpointData` | `LSPTR_BREAKPOINT breakpointToken, Portable<mdMethodDef> funcMetadataToken, Portable<VMPTR_Assembly> vmAssembly, Portable<bool> isIL, Portable<UINT> offset, Portable<UINT> encVersion, LSPTR_METHODDESC nativeCodeMethodDescToken, Portable<CORDB_ADDRESS> codeStartAddress` |
| `DB_IPCE_STEP_CANCEL` | Debugger -> runtime | `StepData` | `LSPTR_STEPPER stepperToken, Portable<VMPTR_Thread> vmThreadToken, Portable<CORDB_ADDRESS> frameToken, Portable<bool> stepIn, Portable<bool> rangeIL, Portable<bool> IsJMCStop, Portable<UINT> totalRangeCount, Portable<CorDebugStepReason> reason, Portable<CorDebugUnmappedStop> rgfMappingStop, Portable<CorDebugIntercept> rgfInterceptStop, Portable<UINT> rangeCount, COR_DEBUG_STEP_RANGE range` |
| `DB_IPCE_STEP` | Debugger -> runtime | `StepData` | `LSPTR_STEPPER stepperToken, Portable<VMPTR_Thread> vmThreadToken, Portable<CORDB_ADDRESS> frameToken, Portable<bool> stepIn, Portable<bool> rangeIL, Portable<bool> IsJMCStop, Portable<UINT> totalRangeCount, Portable<CorDebugStepReason> reason, Portable<CorDebugUnmappedStop> rgfMappingStop, Portable<CorDebugIntercept> rgfInterceptStop, Portable<UINT> rangeCount, COR_DEBUG_STEP_RANGE range` |
| `DB_IPCE_STEP_OUT` | Debugger -> runtime | `StepData` | `LSPTR_STEPPER stepperToken, Portable<VMPTR_Thread> vmThreadToken, Portable<CORDB_ADDRESS> frameToken, Portable<bool> stepIn, Portable<bool> rangeIL, Portable<bool> IsJMCStop, Portable<UINT> totalRangeCount, Portable<CorDebugStepReason> reason, Portable<CorDebugUnmappedStop> rgfMappingStop, Portable<CorDebugIntercept> rgfInterceptStop, Portable<UINT> rangeCount, COR_DEBUG_STEP_RANGE range` |
| `DB_IPCE_GET_BUFFER` | Debugger -> runtime | `GetBuffer` | `Portable<ULONG> bufSize` |
| `DB_IPCE_RELEASE_BUFFER` | Debugger -> runtime | `ReleaseBuffer` | `Portable<CORDB_ADDRESS> pBuffer` |
| `DB_IPCE_SET_CLASS_LOAD_FLAG` | Debugger -> runtime | `SetClassLoad` | `Portable<VMPTR_Assembly> vmAssembly, Portable<bool> flag` |
| `DB_IPCE_CONTINUE_EXCEPTION` | Debugger -> runtime | None | Common header only |
| `DB_IPCE_ATTACHING` | Debugger -> runtime | None | Common header only |
| `DB_IPCE_APPLY_CHANGES` | Debugger -> runtime | `ApplyChanges` | `Portable<VMPTR_Assembly> vmAssembly, Portable<DWORD> cbDeltaMetadata, Portable<CORDB_ADDRESS> pDeltaMetadata, Portable<CORDB_ADDRESS> pDeltaIL, Portable<DWORD> cbDeltaIL` |
| `DB_IPCE_IS_TRANSITION_STUB` | Debugger -> runtime | `IsTransitionStub` | `Portable<CORDB_ADDRESS> address` |
| `DB_IPCE_IS_TRANSITION_STUB_RESULT` | Runtime -> debugger response | `IsTransitionStubResult` | `Portable<bool> isStub` |
| `DB_IPCE_ENABLE_LOG_MESSAGES` | Debugger -> runtime | `LogSwitchSettingMessage` | `Portable<int> iLevel` |
| `DB_IPCE_FUNC_EVAL` | Debugger -> runtime | `FuncEval` (`DebuggerIPCE_FuncEvalInfo`) | `Portable<VMPTR_Thread> vmThreadToken, Portable<DebuggerIPCE_FuncEvalType> funcEvalType, Portable<mdMethodDef> funcMetadataToken, Portable<mdTypeDef> funcClassMetadataToken, Portable<VMPTR_Assembly> vmAssembly, RSPTR_CORDBEVAL funcEvalKey, Portable<bool> evalDuringException, Portable<UINT> argCount, Portable<UINT> genericArgsCount, Portable<UINT> genericArgsNodeCount, Portable<UINT> stringSize, Portable<UINT> arrayRank` |
| `DB_IPCE_SET_REFERENCE` | Debugger -> runtime | `SetReference` | `Portable<CORDB_ADDRESS> objectRefAddress, Portable<VMPTR_OBJECTHANDLE> vmObjectHandle, Portable<CORDB_ADDRESS> newReference` |
| `DB_IPCE_FUNC_EVAL_ABORT` | Debugger -> runtime | `FuncEvalAbort` | `LSPTR_DEBUGGEREVAL debuggerEvalKey` |
| `DB_IPCE_DETACH_FROM_PROCESS` | Debugger -> runtime; runtime -> debugger response | None | Common header only |
| `DB_IPCE_CONTROL_C_EVENT_RESULT` | Debugger -> runtime | None | Common header only |
| `DB_IPCE_FUNC_EVAL_CLEANUP` | Debugger -> runtime | `FuncEvalCleanup` | `LSPTR_DEBUGGEREVAL debuggerEvalKey` |
| `DB_IPCE_SET_ALL_DEBUG_STATE` | Debugger -> runtime | `SetAllDebugState` | `Portable<VMPTR_Thread> vmThreadToken, Portable<CorDebugThreadState> debugState` |
| `DB_IPCE_SET_VALUE_CLASS` | Debugger -> runtime | `SetValueClass` | `Portable<CORDB_ADDRESS> oldData, Portable<CORDB_ADDRESS> newData, DebuggerIPCE_BasicTypeData type` |
| `DB_IPCE_SET_METHOD_JMC_STATUS` | Debugger -> runtime | `SetJMCFunctionStatus` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdMethodDef> funcMetadataToken, Portable<DWORD> dwStatus` |
| `DB_IPCE_GET_METHOD_JMC_STATUS` | Debugger -> runtime | `SetJMCFunctionStatus` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdMethodDef> funcMetadataToken, Portable<DWORD> dwStatus` |
| `DB_IPCE_SET_MODULE_JMC_STATUS` | Debugger -> runtime | `SetJMCFunctionStatus` | `Portable<VMPTR_Assembly> vmAssembly, Portable<mdMethodDef> funcMetadataToken, Portable<DWORD> dwStatus` |
| `DB_IPCE_FUNC_EVAL_RUDE_ABORT` | Debugger -> runtime | `FuncEvalRudeAbort` | `LSPTR_DEBUGGEREVAL debuggerEvalKey` |
| `DB_IPCE_CREATE_HANDLE` | Debugger -> runtime | `CreateHandle` | `Portable<CORDB_ADDRESS> objectToken, Portable<CorDebugHandleType> handleType` |
| `DB_IPCE_DISPOSE_HANDLE` | Debugger -> runtime | `DisposeHandle` | `Portable<VMPTR_OBJECTHANDLE> vmObjectHandle, Portable<CorDebugHandleType> handleType` |
| `DB_IPCE_INTERCEPT_EXCEPTION` | Debugger -> runtime | `InterceptException` | `Portable<VMPTR_Thread> vmThreadToken, Portable<CORDB_ADDRESS> frameToken` |
| `DB_IPCE_RESOLVE_UPDATE_METADATA_1` | Debugger -> runtime | `MetadataUpdateRequest` | `Portable<VMPTR_Module> vmModule, Portable<CORDB_ADDRESS> pMetadataStart, Portable<ULONG> nMetadataSize` |
| `DB_IPCE_RESOLVE_UPDATE_METADATA_2` | Debugger -> runtime | `MetadataUpdateRequest` | `Portable<VMPTR_Module> vmModule, Portable<CORDB_ADDRESS> pMetadataStart, Portable<ULONG> nMetadataSize` |
| `DB_IPCE_DISABLE_OPTS` | Debugger -> runtime | `DisableOptData` | `Portable<mdMethodDef> funcMetadataToken, Portable<VMPTR_Module> pModule` |
| `DB_IPCE_FORCE_CATCH_HANDLER_FOUND` | Debugger -> runtime | `ForceCatchHandlerFoundData` | `Portable<bool> enableEvents, Portable<VMPTR_Object> vmObj` |
| `DB_IPCE_SET_ENABLE_CUSTOM_NOTIFICATION` | Debugger -> runtime | `CustomNotificationData` | `Portable<VMPTR_Module> vmModule, Portable<mdTypeDef> classMetadataToken, Portable<bool> Enabled` |

`DebuggerIPCE_BasicTypeData` is laid out as `Portable<CorElementType> elementType`,
`Portable<mdTypeDef> metadataToken`, `Portable<VMPTR_Assembly> vmAssembly`,
`Portable<VMPTR_TypeHandle> vmTypeHandle`. `DebuggerIPCE_ExpandedTypeData` starts
with `Portable<CorElementType> elementType`, followed by its `ClassTypeData`,
`UnaryTypeData`, `ArrayTypeData`, or `NaryTypeData` union member. `ClassTypeData`
contains `Portable<mdTypeDef> metadataToken`, `Portable<VMPTR_Assembly> vmAssembly`,
and `Portable<VMPTR_TypeHandle> typeHandle`; `UnaryTypeData` contains
`DebuggerIPCE_BasicTypeData unaryTypeArg`; `ArrayTypeData` contains
`DebuggerIPCE_BasicTypeData arrayTypeArg` and `Portable<DWORD> arrayRank`;
`NaryTypeData` contains `Portable<VMPTR_TypeHandle> typeHandle`.


### Debugger IPC control block baseline

`CorDBIPC_BUFFER_SIZE` is 4016 when the host is 64-bit, or 2092 on a x86/ARM host.
Those buffers start at the same offset and add 24 bytes to every field after them (`sizeof` 4312).

#### Shared-memory control block

| Field | Type | 64-bit | x86/ARM 32-bit host | Other 32-bit | Notes |
| ----- | ---- | ------ | ------------------- | ------------ | ----- |
| `m_DCBSize` | `SIZE_T` | 0 | 0 | 0 | Initialization semaphore. Published last. |
| `m_verMajor` | `ULONG` | 8 | 4 | 4 | Left-side build number. |
| `m_verMinor` | `ULONG` | 12 | 8 | 8 | Left-side build number. |
| `m_checkedBuild` | `bool` | 16 | 12 | 12 | Left-side build flavor. |
| `padding1` | `BYTE` | 17 | 13 | 13 | |
| `padding2` | `BYTE` | 18 | 14 | 14 | |
| `padding3` | `BYTE` | 19 | 15 | 15 | |
| `m_runtimeProtocol` | `ULONG` | 20 | 16 | 16 | Current runtime protocol. Version 2 value is 2. |
| `m_runtimeProtocolMinSupported` | `ULONG` | 24 | 20 | 20 | Equal to `m_runtimeProtocol`. Version 2 value is 2. |
| `m_debuggerProtocolCurrent` | `ULONG` | 28 | 24 | 24 | Written by the debugger. Version 2 value is 2. |
| `m_debuggerProtocolMinSupported` | `ULONG` | 32 | 28 | 28 | Written by the debugger. Version 2 value is 2. |
| `m_errorHR` | `HRESULT` | 36 | 32 | 32 | |
| `m_errorCode` | `unsigned int` | 40 | 36 | 36 | |
| `padding4` | `ULONG` | 44 | absent | absent | 64-bit only. Keeps the following handles aligned. |
| `m_rightSideEventAvailable` | `RemoteHANDLE` | 48 | 40 | 40 | |
| `m_rightSideEventRead` | `RemoteHANDLE` | 56 | 44 | 44 | |
| `m_paddingObsoleteLSEA` | `RemoteHANDLE` | 64 | 48 | 48 | Retained for the v1.1 prefix. |
| `m_paddingObsoleteLSER` | `RemoteHANDLE` | 72 | 52 | 52 | Retained for the v1.1 prefix. |
| `m_rightSideProcessHandle` | `RemoteHANDLE` | 80 | 56 | 56 | Last field of the v1.1 prefix. |
| `m_leftSideUnmanagedWaitEvent` | `RemoteHANDLE` | 88 | 60 | 60 | |
| `m_realHelperThreadId` | `DWORD` | 96 | 64 | 64 | Set when the helper thread is created. |
| `m_helperThreadId` | `DWORD` | 100 | 68 | 68 | Published once the helper thread is pumping. |
| `m_temporaryHelperThreadId` | `DWORD` | 104 | 72 | 72 | Non-zero while a temporary helper thread exists. |
| `m_CanaryThreadId` | `DWORD` | 108 | 76 | 76 | |
| `m_pRuntimeOffsets` | `DebuggerIPCRuntimeOffsets*` | 112 | 80 | 80 | Target pointer to a separate allocation. |
| `m_helperThreadStartAddr` | `void*` | 120 | 84 | 84 | |
| `m_helperRemoteStartAddr` | `void*` | 128 | 88 | 88 | |
| `m_specialThreadList` | `DWORD*` | 136 | 92 | 92 | |
| `m_receiveBuffer` | `BYTE[CorDBIPC_BUFFER_SIZE]` | 144 | 96 | 96 | Embedded only in the shared-memory block. |
| `m_sendBuffer` | `BYTE[CorDBIPC_BUFFER_SIZE]` | 4160 | 2188 | 4112 | Embedded only in the shared-memory block. |
| `m_specialThreadListLength` | `DWORD` | 8176 | 4280 | 8128 | |
| `m_shutdownBegun` | `bool` | 8180 | 4284 | 8132 | |
| `m_rightSideIsWin32Debugger` | `bool` | 8181 | 4285 | 8133 | |
| `m_specialThreadListDirty` | `bool` | 8182 | 4286 | 8134 | |
| `m_rightSideShouldCreateHelperThread` | `bool` | 8183 | 4287 | 8135 | |
| `sizeof` | | 8184 | 4288 | 8136 | |

Other 32-bit is any 32-bit target other than x86 or ARM, including WASM.
Its buffer size is 4016.

#### Transport control block

`DebuggerIPCControlBlockTransport` is the `MT_GetDCB` image. It omits every
`RemoteHANDLE` and both embedded buffers. Those fields are not given
substitute offsets; marshaling copies the remaining fields by name.

| Field | Type | 64-bit | 32-bit |
| ----- | ---- | ------ | ------ |
| `m_DCBSize` | `SIZE_T` | 0 | 0 |
| `m_verMajor` | `ULONG` | 8 | 4 |
| `m_verMinor` | `ULONG` | 12 | 8 |
| `m_checkedBuild` | `bool` | 16 | 12 |
| `padding1` | `BYTE` | 17 | 13 |
| `padding2` | `BYTE` | 18 | 14 |
| `padding3` | `BYTE` | 19 | 15 |
| `m_runtimeProtocol` | `ULONG` | 20 | 16 |
| `m_runtimeProtocolMinSupported` | `ULONG` | 24 | 20 |
| `m_debuggerProtocolCurrent` | `ULONG` | 28 | 24 |
| `m_debuggerProtocolMinSupported` | `ULONG` | 32 | 28 |
| `m_errorHR` | `HRESULT` | 36 | 32 |
| `m_errorCode` | `unsigned int` | 40 | 36 |
| `padding4` | `ULONG` | 44 | absent |
| `m_realHelperThreadId` | `DWORD` | 48 | 40 |
| `m_helperThreadId` | `DWORD` | 52 | 44 |
| `m_temporaryHelperThreadId` | `DWORD` | 56 | 48 |
| `m_CanaryThreadId` | `DWORD` | 60 | 52 |
| `m_pRuntimeOffsets` | `DebuggerIPCRuntimeOffsets*` | 64 | 56 |
| `m_helperThreadStartAddr` | `void*` | 72 | 60 |
| `m_helperRemoteStartAddr` | `void*` | 80 | 64 |
| `m_specialThreadList` | `DWORD*` | 88 | 68 |
| `m_specialThreadListLength` | `DWORD` | 96 | 72 |
| `m_shutdownBegun` | `bool` | 100 | 76 |
| `m_rightSideIsWin32Debugger` | `bool` | 101 | 77 |
| `m_specialThreadListDirty` | `bool` | 102 | 78 |
| `m_rightSideShouldCreateHelperThread` | `bool` | 103 | 79 |
| `sizeof` | | 104 | 80 |
