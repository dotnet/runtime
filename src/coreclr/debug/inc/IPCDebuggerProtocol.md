### List of Debugger IPC protocol changes

| Version | Minimum supported version | Changes |
| ------- | ------------------------- | ------- |
| 2       | 2                         |         |

### List of Runtime IPC protocol changes

| Version | Changes |
| ------- | ------- |
| 2       |         |

### Current debugger IPC events

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

The table lists declared event IDs and their event-specific storage. A declared
ID is not necessarily implemented by the current sender, receiver, or transport.
A response does not return all the fields of the corresponding request.

Event IDs are hexadecimal values, not table ordinals. `DB_IPCE_TYPE_MASK=0x0fff`
selects the event type; `DB_IPCE_INVALID_EVENT=0x0000` and
`DB_IPCE_DEBUGGER_INVALID=0x0249` are invalid-event sentinels. The range markers
are `DB_IPCE_RUNTIME_FIRST=0x0100`, `DB_IPCE_RUNTIME_LAST=0x0167` (exclusive),
`DB_IPCE_DEBUGGER_FIRST=0x0200`, and `DB_IPCE_DEBUGGER_LAST=0x025c` (exclusive).
Range membership alone is not a direction guarantee:
`DB_IPCE_IS_TRANSITION_STUB_RESULT=0x0222` is a runtime reply despite its
debugger-range ID.

In the table, `field @n` gives the field's byte offset relative to the
payload union; its offset from the event start is `H+n`. `H` is 40 for
Windows x86 and 32-bit ARM, or 48 for the 64-bit layouts. `C` is `alignof(CONTEXT)`
(4, 8 or 16).

Transport lengths are event bytes from `DbgTransportSession::GetEventSize`,
including the common header but excluding transport framing, with no extra
rounding. `N = max(1, StepData.rangeCount)`.

| Event | ID | Direction | Payload | Payload layout | Transport length |
| ----- | -- | --------- | ------- | -------------- | ---------------- |
| `DB_IPCE_BREAKPOINT` | `0x0100` | Runtime -> debugger notification | `BreakpointData` | `LSPTR_BREAKPOINT breakpointToken @0, Portable<mdMethodDef> funcMetadataToken @8, Portable<VMPTR_Assembly> vmAssembly @16, Portable<bool> isIL @24, Portable<UINT> offset @28, Portable<UINT> encVersion @32, LSPTR_METHODDESC nativeCodeMethodDescToken @40, Portable<CORDB_ADDRESS> codeStartAddress @48` | `H + 56` |
| `DB_IPCE_SYNC_COMPLETE` | `0x0102` | Runtime -> debugger notification | None | Common header only | `H` |
| `DB_IPCE_THREAD_ATTACH` | `0x0103` | Runtime -> debugger notification | None | Common header only | `H` |
| `DB_IPCE_THREAD_DETACH` | `0x0104` | Runtime -> debugger notification | None | Common header only | `H` |
| `DB_IPCE_LOAD_MODULE` | `0x0105` | Runtime -> debugger notification | `LoadModuleData` | `Portable<VMPTR_Assembly> vmAssembly @0` | `H + 8` |
| `DB_IPCE_UNLOAD_MODULE` | `0x0106` | Runtime -> debugger notification | `UnloadModuleData` | `Portable<VMPTR_Assembly> vmAssembly @0, LSPTR_ASSEMBLY debuggerAssemblyToken @8` | `H + 16` |
| `DB_IPCE_LOAD_CLASS` | `0x0107` | Runtime -> debugger notification | `LoadClass` | `Portable<mdTypeDef> classMetadataToken @0, Portable<VMPTR_Assembly> vmAssembly @8, LSPTR_ASSEMBLY classDebuggerAssemblyToken @16` | `H + 24` |
| `DB_IPCE_UNLOAD_CLASS` | `0x0108` | Runtime -> debugger notification | `UnloadClass` | `Portable<mdTypeDef> classMetadataToken @0, Portable<VMPTR_Assembly> vmAssembly @8, LSPTR_ASSEMBLY classDebuggerAssemblyToken @16` | `H + 24` |
| `DB_IPCE_EXCEPTION` | `0x0109` | Runtime -> debugger notification | `Exception` | `Portable<VMPTR_OBJECTHANDLE> vmExceptionHandle @0, Portable<bool> firstChance @8, Portable<bool> continuable @9` | `H + 16` |
| `DB_IPCE_UNHANDLED_EXCEPTION` | `0x010a` | Runtime -> debugger notification | None | Common header only | `H` |
| `DB_IPCE_BREAKPOINT_ADD_RESULT` | `0x010d` | Runtime -> debugger response | `BreakpointData` | `LSPTR_BREAKPOINT breakpointToken @0, Portable<mdMethodDef> funcMetadataToken @8, Portable<VMPTR_Assembly> vmAssembly @16, Portable<bool> isIL @24, Portable<UINT> offset @28, Portable<UINT> encVersion @32, LSPTR_METHODDESC nativeCodeMethodDescToken @40, Portable<CORDB_ADDRESS> codeStartAddress @48` | `H + 56` |
| `DB_IPCE_STEP_RESULT` | `0x010e` | Runtime -> debugger response | `StepData` | `LSPTR_STEPPER stepperToken @0, Portable<VMPTR_Thread> vmThreadToken @8, Portable<CORDB_ADDRESS> frameToken @16, Portable<bool> stepIn @24, Portable<bool> rangeIL @25, Portable<bool> IsJMCStop @26, Portable<UINT> totalRangeCount @28, Portable<CorDebugStepReason> reason @32, Portable<CorDebugUnmappedStop> rgfMappingStop @36, Portable<CorDebugIntercept> rgfInterceptStop @40, Portable<UINT> rangeCount @44, COR_DEBUG_STEP_RANGE range @48` | `H + 48 + 8*N` |
| `DB_IPCE_STEP_COMPLETE` | `0x010f` | Runtime -> debugger notification | `StepData` | `LSPTR_STEPPER stepperToken @0, Portable<VMPTR_Thread> vmThreadToken @8, Portable<CORDB_ADDRESS> frameToken @16, Portable<bool> stepIn @24, Portable<bool> rangeIL @25, Portable<bool> IsJMCStop @26, Portable<UINT> totalRangeCount @28, Portable<CorDebugStepReason> reason @32, Portable<CorDebugUnmappedStop> rgfMappingStop @36, Portable<CorDebugIntercept> rgfInterceptStop @40, Portable<UINT> rangeCount @44, COR_DEBUG_STEP_RANGE range @48` | `H + 56` |
| `DB_IPCE_BREAKPOINT_REMOVE_RESULT` | `0x0111` | Runtime -> debugger response | `BreakpointData` | `LSPTR_BREAKPOINT breakpointToken @0, Portable<mdMethodDef> funcMetadataToken @8, Portable<VMPTR_Assembly> vmAssembly @16, Portable<bool> isIL @24, Portable<UINT> offset @28, Portable<UINT> encVersion @32, LSPTR_METHODDESC nativeCodeMethodDescToken @40, Portable<CORDB_ADDRESS> codeStartAddress @48` | `H` |
| `DB_IPCE_GET_BUFFER_RESULT` | `0x0115` | Runtime -> debugger response | `GetBufferResult` | `Portable<CORDB_ADDRESS> pBuffer @0, Portable<HRESULT> hr @8` | `H + 16` |
| `DB_IPCE_RELEASE_BUFFER_RESULT` | `0x0116` | Runtime -> debugger response | `ReleaseBufferResult` | `Portable<HRESULT> hr @0` | `H + 4` |
| `DB_IPCE_ENC_ADD_FIELD` | `0x0117` | Runtime -> debugger notification | `EnCUpdate` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<mdToken> memberMetadataToken @8, Portable<mdTypeDef> classMetadataToken @12, Portable<ULONG64> newVersionNumber @16` | `H + 24` |
| `DB_IPCE_APPLY_CHANGES_RESULT` | `0x0118` | Runtime -> debugger response | `ApplyChangesResult` | `Portable<HRESULT> hr @0` | `H + 4` |
| `DB_IPCE_CUSTOM_NOTIFICATION` | `0x011b` | Runtime -> debugger notification | `CustomNotification` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<mdTypeDef> classToken @8` | `H + 16` |
| `DB_IPCE_USER_BREAKPOINT` | `0x011c` | Runtime -> debugger notification | None | Common header only | `H` |
| `DB_IPCE_FIRST_LOG_MESSAGE` | `0x011d` | Runtime -> debugger notification | `FirstLogMessage` | `Portable<int> iLevel @0, Portable<CORDB_ADDRESS> szCategory @8, Portable<ULONG> cchCategory @16, Portable<CORDB_ADDRESS> szContent @24, Portable<ULONG> cchContent @32` | `H + 40` |
| `DB_IPCE_CREATE_APP_DOMAIN` | `0x0120` | Runtime -> debugger notification | `AppDomainData` | `Portable<VMPTR_AppDomain> vmAppDomain @0` | `H + 8` |
| `DB_IPCE_LOAD_ASSEMBLY` | `0x0122` | Runtime -> debugger notification | `AssemblyData` | `Portable<VMPTR_Assembly> vmAssembly @0` | `H + 8` |
| `DB_IPCE_UNLOAD_ASSEMBLY` | `0x0123` | Runtime -> debugger notification | `AssemblyData` | `Portable<VMPTR_Assembly> vmAssembly @0` | `H + 8` |
| `DB_IPCE_SET_DEBUG_STATE_RESULT` | `0x0124` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` | `H` |
| `DB_IPCE_FUNC_EVAL_SETUP_RESULT` | `0x0125` | Runtime -> debugger response | `FuncEvalSetupComplete` | `Portable<CORDB_ADDRESS> argDataArea @0, LSPTR_DEBUGGEREVAL debuggerEvalKey @8` | `H + 16` |
| `DB_IPCE_FUNC_EVAL_COMPLETE` | `0x0126` | Runtime -> debugger notification | `FuncEvalComplete` | `RSPTR_CORDBEVAL funcEvalKey @0, Portable<bool> successful @4, Portable<bool> aborted @5, Portable<CORDB_ADDRESS> resultAddr @8, Portable<VMPTR_AppDomain> vmAppDomain @16, Portable<VMPTR_OBJECTHANDLE> vmObjectHandle @24, DebuggerIPCE_ExpandedTypeData resultType @32` | `H + 72` |
| `DB_IPCE_SET_REFERENCE_RESULT` | `0x0127` | Runtime -> debugger response | `SetReference` | `Portable<CORDB_ADDRESS> objectRefAddress @0, Portable<VMPTR_OBJECTHANDLE> vmObjectHandle @8, Portable<CORDB_ADDRESS> newReference @16` | `H + 24` |
| `DB_IPCE_FUNC_EVAL_ABORT_RESULT` | `0x0129` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` | `H` |
| `DB_IPCE_NAME_CHANGE` | `0x012a` | Runtime -> debugger notification | `NameChange` | `Portable<NameChangeType> eventType @0, Portable<VMPTR_AppDomain> vmAppDomain @8, Portable<VMPTR_Thread> vmThread @16` | `H + 24` |
| `DB_IPCE_UPDATE_MODULE_SYMS` | `0x012c` | Runtime -> debugger notification | `UpdateModuleSymsData` | `Portable<VMPTR_Assembly> vmAssembly @0` | `H + 8` |
| `DB_IPCE_CONTROL_C_EVENT` | `0x012f` | Runtime -> debugger notification | None | Common header only | `H` |
| `DB_IPCE_FUNC_EVAL_CLEANUP_RESULT` | `0x0130` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` | `H` |
| `DB_IPCE_ENC_REMAP` | `0x0131` | Runtime -> debugger notification | `EnCRemap` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<mdMethodDef> funcMetadataToken @8, Portable<ULONG64> currentVersionNumber @16, Portable<ULONG64> resumeVersionNumber @24, Portable<ULONG64> currentILOffset @32, Portable<CORDB_ADDRESS> resumeILOffset @40` | `H + 48` |
| `DB_IPCE_SET_VALUE_CLASS_RESULT` | `0x0133` | Runtime -> debugger response | `SetValueClass` | `Portable<CORDB_ADDRESS> oldData @0, Portable<CORDB_ADDRESS> newData @8, DebuggerIPCE_BasicTypeData type @16` | `H + 40` |
| `DB_IPCE_BREAKPOINT_SET_ERROR` | `0x0134` | Runtime -> debugger notification | `BreakpointSetErrorData` | `LSPTR_BREAKPOINT breakpointToken @0` | `H + 8` |
| `DB_IPCE_ENC_UPDATE_FUNCTION` | `0x0137` | Runtime -> debugger notification | `EnCUpdate` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<mdToken> memberMetadataToken @8, Portable<mdTypeDef> classMetadataToken @12, Portable<ULONG64> newVersionNumber @16` | `H + 24` |
| `DB_IPCE_SET_METHOD_JMC_STATUS_RESULT` | `0x013a` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` | `H` |
| `DB_IPCE_GET_METHOD_JMC_STATUS_RESULT` | `0x013b` | Runtime -> debugger response | `SetJMCFunctionStatus` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<mdMethodDef> funcMetadataToken @8, Portable<DWORD> dwStatus @12` | `H + 16` |
| `DB_IPCE_SET_MODULE_JMC_STATUS_RESULT` | `0x013c` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` | `H` |
| `DB_IPCE_FUNC_EVAL_RUDE_ABORT_RESULT` | `0x0144` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` | `H` |
| `DB_IPCE_EXCEPTION_CALLBACK2` | `0x0147` | Runtime -> debugger notification | `ExceptionCallback2` | `Portable<CORDB_ADDRESS> framePointer @0, Portable<UINT> nOffset @8, Portable<CorDebugExceptionCallbackType> eventType @12, Portable<DWORD> dwFlags @16, Portable<VMPTR_OBJECTHANDLE> vmExceptionHandle @24` | `H + 32` |
| `DB_IPCE_EXCEPTION_UNWIND` | `0x0148` | Runtime -> debugger notification | `ExceptionUnwind` | `Portable<CorDebugExceptionUnwindCallbackType> eventType @0, Portable<DWORD> dwFlags @4` | `H + 8` |
| `DB_IPCE_INTERCEPT_EXCEPTION_RESULT` | `0x0149` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` | `H` |
| `DB_IPCE_CREATE_HANDLE_RESULT` | `0x014a` | Runtime -> debugger response | `CreateHandleResult` | `Portable<VMPTR_OBJECTHANDLE> vmObjectHandle @0` | `H + 8` |
| `DB_IPCE_INTERCEPT_EXCEPTION_COMPLETE` | `0x014b` | Runtime -> debugger notification | None | Common header only | `H` |
| `DB_IPCE_ENC_REMAP_COMPLETE` | `0x014c` | Runtime -> debugger notification | `EnCRemapComplete` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<mdMethodDef> funcMetadataToken @8` | `H + 16` |
| `DB_IPCE_CREATE_PROCESS` | `0x014d` | Runtime -> debugger notification | None | Common header only | `H` |
| `DB_IPCE_ENC_ADD_FUNCTION` | `0x014e` | Runtime -> debugger notification | `EnCUpdate` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<mdToken> memberMetadataToken @8, Portable<mdTypeDef> classMetadataToken @12, Portable<ULONG64> newVersionNumber @16` | `H + 24` |
| `DB_IPCE_LEFTSIDE_STARTUP` | `0x015c` | Runtime -> debugger notification | None | Common header only | `H` |
| `DB_IPCE_METADATA_UPDATE` | `0x015d` | Runtime -> debugger notification | `MetadataUpdateData` | `Portable<VMPTR_Assembly> vmAssembly @0` | `H` |
| `DB_IPCE_RESOLVE_UPDATE_METADATA_1_RESULT` | `0x015e` | Runtime -> debugger response | `MetadataUpdateRequest` | `Portable<VMPTR_Module> vmModule @0, Portable<CORDB_ADDRESS> pMetadataStart @8, Portable<ULONG> nMetadataSize @16` | `H` |
| `DB_IPCE_RESOLVE_UPDATE_METADATA_2_RESULT` | `0x015f` | Runtime -> debugger response | `MetadataUpdateRequest` | `Portable<VMPTR_Module> vmModule @0, Portable<CORDB_ADDRESS> pMetadataStart @8, Portable<ULONG> nMetadataSize @16` | `H` |
| `DB_IPCE_DATA_BREAKPOINT` | `0x0160` | Runtime -> debugger notification | `DataBreakpointData` | `Portable<UINT> contextSize @0, CONTEXT context @C` | `H + C + sizeof(CONTEXT)` |
| `DB_IPCE_BEFORE_GARBAGE_COLLECTION` | `0x0161` | Runtime -> debugger notification | None | Common header only | `H` |
| `DB_IPCE_AFTER_GARBAGE_COLLECTION` | `0x0162` | Runtime -> debugger notification | None | Common header only | `H` |
| `DB_IPCE_DISABLE_OPTS_RESULT` | `0x0163` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` | `H` |
| `DB_IPCE_CATCH_HANDLER_FOUND_RESULT` | `0x0165` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` | `H` |
| `DB_IPCE_SET_ENABLE_CUSTOM_NOTIFICATION_RESULT` | `0x0166` | Runtime -> debugger response | None | Common header; result in `Portable<HRESULT> hr` | `H` |
| `DB_IPCE_ASYNC_BREAK` | `0x0200` | Debugger -> runtime | None | Common header only | `H` |
| `DB_IPCE_CONTINUE` | `0x0201` | Debugger -> runtime | None | Common header only | `H` |
| `DB_IPCE_LIST_THREADS` | `0x0202` | Debugger -> runtime | None | Common header only | `H` |
| `DB_IPCE_SET_IP` | `0x0205` | Debugger -> runtime; runtime -> debugger response | `SetIP` | `Portable<CORDB_ADDRESS> startAddress @0, Portable<bool> fCanSetIPOnly @8, Portable<VMPTR_Thread> vmThreadToken @16, Portable<VMPTR_Assembly> vmAssembly @24, Portable<mdMethodDef> mdMethod @32, Portable<VMPTR_MethodDesc> vmMethodDesc @40, Portable<ULONG64> offset @48, Portable<bool> fIsIL @56` | `H + 64` |
| `DB_IPCE_SUSPEND_THREAD` | `0x0206` | Debugger -> runtime | None | Common header only | `H` |
| `DB_IPCE_RESUME_THREAD` | `0x0207` | Debugger -> runtime | None | Common header only | `H` |
| `DB_IPCE_BREAKPOINT_ADD` | `0x0209` | Debugger -> runtime | `BreakpointData` | `LSPTR_BREAKPOINT breakpointToken @0, Portable<mdMethodDef> funcMetadataToken @8, Portable<VMPTR_Assembly> vmAssembly @16, Portable<bool> isIL @24, Portable<UINT> offset @28, Portable<UINT> encVersion @32, LSPTR_METHODDESC nativeCodeMethodDescToken @40, Portable<CORDB_ADDRESS> codeStartAddress @48` | `H + 56` |
| `DB_IPCE_BREAKPOINT_REMOVE` | `0x020a` | Debugger -> runtime | `BreakpointData` | `LSPTR_BREAKPOINT breakpointToken @0, Portable<mdMethodDef> funcMetadataToken @8, Portable<VMPTR_Assembly> vmAssembly @16, Portable<bool> isIL @24, Portable<UINT> offset @28, Portable<UINT> encVersion @32, LSPTR_METHODDESC nativeCodeMethodDescToken @40, Portable<CORDB_ADDRESS> codeStartAddress @48` | `H + 56` |
| `DB_IPCE_STEP_CANCEL` | `0x020b` | Debugger -> runtime | `StepData` | `LSPTR_STEPPER stepperToken @0, Portable<VMPTR_Thread> vmThreadToken @8, Portable<CORDB_ADDRESS> frameToken @16, Portable<bool> stepIn @24, Portable<bool> rangeIL @25, Portable<bool> IsJMCStop @26, Portable<UINT> totalRangeCount @28, Portable<CorDebugStepReason> reason @32, Portable<CorDebugUnmappedStop> rgfMappingStop @36, Portable<CorDebugIntercept> rgfInterceptStop @40, Portable<UINT> rangeCount @44, COR_DEBUG_STEP_RANGE range @48` | `H + 56` |
| `DB_IPCE_STEP` | `0x020c` | Debugger -> runtime | `StepData` | `LSPTR_STEPPER stepperToken @0, Portable<VMPTR_Thread> vmThreadToken @8, Portable<CORDB_ADDRESS> frameToken @16, Portable<bool> stepIn @24, Portable<bool> rangeIL @25, Portable<bool> IsJMCStop @26, Portable<UINT> totalRangeCount @28, Portable<CorDebugStepReason> reason @32, Portable<CorDebugUnmappedStop> rgfMappingStop @36, Portable<CorDebugIntercept> rgfInterceptStop @40, Portable<UINT> rangeCount @44, COR_DEBUG_STEP_RANGE range @48` | `H + 48 + 8*N` |
| `DB_IPCE_STEP_OUT` | `0x020d` | Debugger -> runtime | `StepData` | `LSPTR_STEPPER stepperToken @0, Portable<VMPTR_Thread> vmThreadToken @8, Portable<CORDB_ADDRESS> frameToken @16, Portable<bool> stepIn @24, Portable<bool> rangeIL @25, Portable<bool> IsJMCStop @26, Portable<UINT> totalRangeCount @28, Portable<CorDebugStepReason> reason @32, Portable<CorDebugUnmappedStop> rgfMappingStop @36, Portable<CorDebugIntercept> rgfInterceptStop @40, Portable<UINT> rangeCount @44, COR_DEBUG_STEP_RANGE range @48` | `H + 56` |
| `DB_IPCE_GET_BUFFER` | `0x0211` | Debugger -> runtime | `GetBuffer` | `Portable<ULONG> bufSize @0` | `H + 4` |
| `DB_IPCE_RELEASE_BUFFER` | `0x0212` | Debugger -> runtime | `ReleaseBuffer` | `Portable<CORDB_ADDRESS> pBuffer @0` | `H + 8` |
| `DB_IPCE_SET_CLASS_LOAD_FLAG` | `0x0217` | Debugger -> runtime | `SetClassLoad` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<bool> flag @8` | `H + 16` |
| `DB_IPCE_CONTINUE_EXCEPTION` | `0x0219` | Debugger -> runtime | None | Common header only | `H` |
| `DB_IPCE_ATTACHING` | `0x021a` | Debugger -> runtime | None | Common header only | `H` |
| `DB_IPCE_APPLY_CHANGES` | `0x021b` | Debugger -> runtime | `ApplyChanges` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<DWORD> cbDeltaMetadata @8, Portable<CORDB_ADDRESS> pDeltaMetadata @16, Portable<CORDB_ADDRESS> pDeltaIL @24, Portable<DWORD> cbDeltaIL @32` | `H + 40` |
| `DB_IPCE_IS_TRANSITION_STUB` | `0x0221` | Debugger -> runtime | `IsTransitionStub` | `Portable<CORDB_ADDRESS> address @0` | `H + 8` |
| `DB_IPCE_IS_TRANSITION_STUB_RESULT` | `0x0222` | Runtime -> debugger response | `IsTransitionStubResult` | `Portable<bool> isStub @0` | `H + 1` |
| `DB_IPCE_ENABLE_LOG_MESSAGES` | `0x0224` | Debugger -> runtime | `LogSwitchSettingMessage` | `Portable<int> iLevel @0` | `H + 4` |
| `DB_IPCE_FUNC_EVAL` | `0x0225` | Debugger -> runtime | `FuncEval` (`DebuggerIPCE_FuncEvalInfo`) | `Portable<VMPTR_Thread> vmThreadToken @0, Portable<DebuggerIPCE_FuncEvalType> funcEvalType @8, Portable<mdMethodDef> funcMetadataToken @12, Portable<mdTypeDef> funcClassMetadataToken @16, Portable<VMPTR_Assembly> vmAssembly @24, RSPTR_CORDBEVAL funcEvalKey @32, Portable<bool> evalDuringException @36, Portable<UINT> argCount @40, Portable<UINT> genericArgsCount @44, Portable<UINT> genericArgsNodeCount @48, Portable<UINT> stringSize @52, Portable<UINT> arrayRank @56` | `H + 64` |
| `DB_IPCE_SET_REFERENCE` | `0x0228` | Debugger -> runtime | `SetReference` | `Portable<CORDB_ADDRESS> objectRefAddress @0, Portable<VMPTR_OBJECTHANDLE> vmObjectHandle @8, Portable<CORDB_ADDRESS> newReference @16` | `H + 24` |
| `DB_IPCE_FUNC_EVAL_ABORT` | `0x022c` | Debugger -> runtime | `FuncEvalAbort` | `LSPTR_DEBUGGEREVAL debuggerEvalKey @0` | `H + 8` |
| `DB_IPCE_DETACH_FROM_PROCESS` | `0x022f` | Debugger -> runtime; runtime -> debugger response | None | Common header only | `H` |
| `DB_IPCE_CONTROL_C_EVENT_RESULT` | `0x0230` | Debugger -> runtime | None | Common header only | `H` |
| `DB_IPCE_FUNC_EVAL_CLEANUP` | `0x0231` | Debugger -> runtime | `FuncEvalCleanup` | `LSPTR_DEBUGGEREVAL debuggerEvalKey @0` | `H + 8` |
| `DB_IPCE_SET_ALL_DEBUG_STATE` | `0x0232` | Debugger -> runtime | `SetAllDebugState` | `Portable<VMPTR_Thread> vmThreadToken @0, Portable<CorDebugThreadState> debugState @8` | `H + 16` |
| `DB_IPCE_SET_VALUE_CLASS` | `0x0234` | Debugger -> runtime | `SetValueClass` | `Portable<CORDB_ADDRESS> oldData @0, Portable<CORDB_ADDRESS> newData @8, DebuggerIPCE_BasicTypeData type @16` | `H + 40` |
| `DB_IPCE_SET_METHOD_JMC_STATUS` | `0x023a` | Debugger -> runtime | `SetJMCFunctionStatus` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<mdMethodDef> funcMetadataToken @8, Portable<DWORD> dwStatus @12` | `H + 16` |
| `DB_IPCE_GET_METHOD_JMC_STATUS` | `0x023b` | Debugger -> runtime | `SetJMCFunctionStatus` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<mdMethodDef> funcMetadataToken @8, Portable<DWORD> dwStatus @12` | `H + 16` |
| `DB_IPCE_SET_MODULE_JMC_STATUS` | `0x023c` | Debugger -> runtime | `SetJMCFunctionStatus` | `Portable<VMPTR_Assembly> vmAssembly @0, Portable<mdMethodDef> funcMetadataToken @8, Portable<DWORD> dwStatus @12` | `H + 16` |
| `DB_IPCE_FUNC_EVAL_RUDE_ABORT` | `0x0241` | Debugger -> runtime | `FuncEvalRudeAbort` | `LSPTR_DEBUGGEREVAL debuggerEvalKey @0` | `H + 8` |
| `DB_IPCE_CREATE_HANDLE` | `0x0244` | Debugger -> runtime | `CreateHandle` | `Portable<CORDB_ADDRESS> objectToken @0, Portable<CorDebugHandleType> handleType @8` | `H + 16` |
| `DB_IPCE_DISPOSE_HANDLE` | `0x0245` | Debugger -> runtime | `DisposeHandle` | `Portable<VMPTR_OBJECTHANDLE> vmObjectHandle @0, Portable<CorDebugHandleType> handleType @8` | `H + 16` |
| `DB_IPCE_INTERCEPT_EXCEPTION` | `0x0246` | Debugger -> runtime | `InterceptException` | `Portable<VMPTR_Thread> vmThreadToken @0, Portable<CORDB_ADDRESS> frameToken @8` | `H + 16` |
| `DB_IPCE_RESOLVE_UPDATE_METADATA_1` | `0x0256` | Debugger -> runtime | `MetadataUpdateRequest` | `Portable<VMPTR_Module> vmModule @0, Portable<CORDB_ADDRESS> pMetadataStart @8, Portable<ULONG> nMetadataSize @16` | `H` |
| `DB_IPCE_RESOLVE_UPDATE_METADATA_2` | `0x0257` | Debugger -> runtime | `MetadataUpdateRequest` | `Portable<VMPTR_Module> vmModule @0, Portable<CORDB_ADDRESS> pMetadataStart @8, Portable<ULONG> nMetadataSize @16` | `H` |
| `DB_IPCE_DISABLE_OPTS` | `0x0258` | Debugger -> runtime | `DisableOptData` | `Portable<mdMethodDef> funcMetadataToken @0, Portable<VMPTR_Module> pModule @8` | `H + 16` |
| `DB_IPCE_FORCE_CATCH_HANDLER_FOUND` | `0x025a` | Debugger -> runtime | `ForceCatchHandlerFoundData` | `Portable<bool> enableEvents @0, Portable<VMPTR_Object> vmObj @8` | `H + 16` |
| `DB_IPCE_SET_ENABLE_CUSTOM_NOTIFICATION` | `0x025b` | Debugger -> runtime | `CustomNotificationData` | `Portable<VMPTR_Module> vmModule @0, Portable<mdTypeDef> classMetadataToken @8, Portable<bool> Enabled @12` | `H + 16` |

### Referenced structures

The layouts below come from
[`dbgipcevents.h`](https://github.com/dotnet/runtime/blob/bf500a0eea0ac203cba07587c29652474627cc6e/src/coreclr/debug/inc/dbgipcevents.h).
Offsets are relative to the named record, not the containing event.
`Portable<T>` preserves the size and alignment of `T`. `bool`/`BYTE` occupy
1 byte; enum values, metadata tokens, `DWORD`, `ULONG`, `UINT`, `HRESULT` and
`int` occupy 4; `CORDB_ADDRESS`, `ULONG64`, `VMPTR_*` and `LSPTR_*` occupy 8
with 8-byte alignment. `RSPTR_CORDBEVAL` is a 4-byte debugger cookie, not a
native pointer. Record lengths include trailing alignment padding.

| Record | Field layout | Length | Alignment |
| ------ | ------------ | ------ | --------- |
| `DebuggerIPCE_BasicTypeData` | `Portable<CorElementType> elementType @0, Portable<mdTypeDef> metadataToken @4, Portable<VMPTR_Assembly> vmAssembly @8, Portable<VMPTR_TypeHandle> vmTypeHandle @16` | 24 | 8 |
| `DebuggerIPCE_ExpandedTypeData` | `Portable<CorElementType> elementType @0, ClassTypeData @8, UnaryTypeData @8, ArrayTypeData @8, NaryTypeData @8` (overlapping union members) | 40 | 8 |
| `ClassTypeData` | `Portable<mdTypeDef> metadataToken @0, Portable<VMPTR_Assembly> vmAssembly @8, Portable<VMPTR_TypeHandle> typeHandle @16` | 24 | 8 |
| `UnaryTypeData` | `DebuggerIPCE_BasicTypeData unaryTypeArg @0` | 24 | 8 |
| `ArrayTypeData` | `DebuggerIPCE_BasicTypeData arrayTypeArg @0, Portable<DWORD> arrayRank @24` | 32 | 8 |
| `NaryTypeData` | `Portable<VMPTR_TypeHandle> typeHandle @0` | 8 | 8 |
| `DebuggerIPCE_TypeArgData` | `DebuggerIPCE_ExpandedTypeData data @0, Portable<UINT> numTypeArgs @40` | 48 | 8 |
| `RemoteAddress` | `Portable<RemoteAddressKind> kind @0, Portable<CorDebugRegister> reg1 @4, Portable<CORDB_ADDRESS> reg1Addr @8, Portable<ULONG64> reg1Value @16, u @24, Portable<CORDB_ADDRESS> addr @24, Portable<DWORD> floatIndex @24` (last three overlap) | 48 | 8 |
| `RemoteAddress.u` | `Portable<CorDebugRegister> reg2 @0, Portable<CORDB_ADDRESS> reg2Addr @8, Portable<ULONG64> reg2Value @16` | 24 | 8 |
| `DebuggerIPCE_FuncEvalArgData` | `RemoteAddress argHome @0, Portable<CORDB_ADDRESS> argAddr @48, Portable<CorElementType> argElementType @56, Portable<UINT> fullArgTypeNodeCount @60, Portable<CORDB_ADDRESS> fullArgType @64, Portable<BYTE> argLiteralData[8] @72, Portable<bool> argIsLiteral @80, Portable<bool> argIsHandleValue @81` | 88 | 8 |
| `COR_DEBUG_STEP_RANGE` | `ULONG32 startOffset @0, ULONG32 endOffset @4` | 8 | 4 |
| `DebuggerIPCFirstChanceData` | `LSPTR_CONTEXT pLeftSideContext @0, Portable<HijackAction> action @8, Portable<UINT> debugCounter @12` | 16 | 8 |

`DebuggerIPCE_FuncEvalInfo` is 64 bytes; its fields are shown in the
`DB_IPCE_FUNC_EVAL` event row.

`RemoteAddress.kind` selects the register, register-pair, register/memory,
memory/register or floating-point home. The `u`, `addr` and `floatIndex`
members are alternatives, not additional fields.
`kLeafFrameRegAddr=0` and `kNonLeafFrameRegAddr=0xffffffffffffffff` are the
defined register-address sentinels.

#### Func-eval setup buffer

`argDataArea` points to these contiguous regions, with no padding.
The request counts determine their sizes.

| Region | Byte offset | Byte length |
| ------ | ----------- | ----------- |
| Generic types (`DebuggerIPCE_TypeArgData[]`) | 0 | `48 * genericArgsNodeCount` |
| Call/new-object arguments (`DebuggerIPCE_FuncEvalArgData[]`) | `48 * genericArgsNodeCount` | `88 * argCount` |
| New-string data (instead of argument records) | 0 (`genericArgsNodeCount=0`) | `stringSize` UTF-16 bytes, without a required terminator |
| New-array dimensions (instead of argument records) | `48 * genericArgsNodeCount` | `arrayRank * sizeof(SIZE_T)` |

Generic-type and `fullArgType` records use depth-first preorder: each parent
precedes its complete child subtrees, with siblings and roots in argument order.

`numTypeArgs` counts immediate children,
`genericArgsCount` counts roots, and `genericArgsNodeCount` counts all nodes.
New arrays have one element-type root and native `SIZE_T` dimensions,
without lower bounds.

`fullArgType` points to a separate array of `fullArgTypeNodeCount`
`DebuggerIPCE_TypeArgData` records.

`DataBreakpointData` carries the platform's native `CONTEXT` in full.

### Enum values

IPC enum fields occupy 4 bytes, including `CorElementType` (not its one-byte
signature encoding). Prefixes below apply to every name in the values column;
numbers are decimal unless prefixed with `0x`. Flag values may be ORed.
An enum definition does not make every member valid in every event field.

Sources:
[`dbgipcevents.h`](https://github.com/dotnet/runtime/blob/bf500a0eea0ac203cba07587c29652474627cc6e/src/coreclr/debug/inc/dbgipcevents.h),
[`cordebug.idl`](https://github.com/dotnet/runtime/blob/bf500a0eea0ac203cba07587c29652474627cc6e/src/coreclr/inc/cordebug.idl),
and [`corhdr.h`](https://github.com/dotnet/runtime/blob/bf500a0eea0ac203cba07587c29652474627cc6e/src/coreclr/inc/corhdr.h).

| Enum | Prefix | Values |
| ---- | ------ | ------ |
| `CorElementType` | `ELEMENT_TYPE_` | `END=0x00, VOID=0x01, BOOLEAN=0x02, CHAR=0x03, I1=0x04, U1=0x05, I2=0x06, U2=0x07, I4=0x08, U4=0x09, I8=0x0a, U8=0x0b, R4=0x0c, R8=0x0d, STRING=0x0e, PTR=0x0f, BYREF=0x10, VALUETYPE=0x11, CLASS=0x12, VAR=0x13, ARRAY=0x14, GENERICINST=0x15, TYPEDBYREF=0x16, I=0x18, U=0x19, FNPTR=0x1b, OBJECT=0x1c, SZARRAY=0x1d, MVAR=0x1e, CMOD_REQD=0x1f, CMOD_OPT=0x20, INTERNAL=0x21, CMOD_INTERNAL=0x22, MAX=0x23, MODIFIER=0x40, SENTINEL=0x41, PINNED=0x45` |
| `CorDebugStepReason` | `STEP_` | `NORMAL=0, RETURN=1, CALL=2, EXCEPTION_FILTER=3, EXCEPTION_HANDLER=4, INTERCEPT=5, EXIT=6` |
| `CorDebugUnmappedStop` | `STOP_` | `NONE=0, PROLOG=0x01, EPILOG=0x02, NO_MAPPING_INFO=0x04, OTHER_UNMAPPED=0x08, UNMANAGED=0x10, ALL=0xffff` |
| `CorDebugIntercept` | `INTERCEPT_` | `NONE=0, CLASS_INIT=0x01, EXCEPTION_FILTER=0x02, SECURITY=0x04, CONTEXT_POLICY=0x08, INTERCEPTION=0x10, ALL=0xffff` |
| `CorDebugExceptionCallbackType` | `DEBUG_EXCEPTION_` | `FIRST_CHANCE=1, USER_FIRST_CHANCE=2, CATCH_HANDLER_FOUND=3, UNHANDLED=4` |
| `CorDebugExceptionFlags` | `DEBUG_EXCEPTION_` | `NONE=0, CAN_BE_INTERCEPTED=0x01` |
| `CorDebugExceptionUnwindCallbackType` | `DEBUG_EXCEPTION_` | `UNWIND_BEGIN=1, INTERCEPTED=2` |
| `CorDebugThreadState` | `THREAD_` | `RUN=0, SUSPEND=1` |
| `CorDebugHandleType` | `HANDLE_` | `STRONG=1, WEAK_TRACK_RESURRECTION=2, PINNED=3` |
| `DebuggerIPCE_FuncEvalType` | `DB_IPCE_FET_` | `NORMAL=0, NEW_OBJECT=1, NEW_OBJECT_NC=2, NEW_STRING=3, NEW_ARRAY=4` |
| `NameChangeType` | None | `APP_DOMAIN_NAME_CHANGE=0, THREAD_NAME_CHANGE=1` |
| `RemoteAddressKind` | `RAK_` | `NONE=0, REG=1, REGREG=2, REGMEM=3, MEMREG=4, FLOAT=5, END=6` |
| `HijackAction` | `HIJACK_ACTION_` | `EXIT_UNHANDLED=0, EXIT_HANDLED=1, WAIT=2` |

`CorDebugRegister` also occupies 4 bytes. Register numbers overlap across
architectures; use the target architecture to interpret them. In the following
table, `X0..X28=3..31` denotes consecutive names and consecutive numeric values.

| Register prefix | Values |
| --------------- | ------ |
| `REGISTER_` | `INSTRUCTION_POINTER=0, STACK_POINTER=1, FRAME_POINTER=2` |
| `REGISTER_X86_` | `EIP=0, ESP=1, EBP=2, EAX=3, ECX=4, EDX=5, EBX=6, ESI=7, EDI=8, FPSTACK_0..FPSTACK_7=9..16` |
| `REGISTER_AMD64_` | `RIP=0, RSP=1, RBP=2, RAX=3, RCX=4, RDX=5, RBX=6, RSI=7, RDI=8, R8..R15=9..16, XMM0..XMM15=17..32` |
| `REGISTER_IA64_` | `BSP=2, R0=3, F0=131` |
| `REGISTER_ARM_` | `PC=0, SP=1, R0..R12=2..14, LR=15, D0..D31=16..47` |
| `REGISTER_ARM64_` | `PC=0, SP=1, FP=2, X0..X28=3..31, LR=32, V0..V31=33..64` |
| `REGISTER_LOONGARCH64_` | `PC=0, SP=1, FP=2, RA=3, TP=4, A0..A7=5..12, T0..T8=13..21, X0=22, S0..S8=23..31, F0..F31=32..63` |
| `REGISTER_RISCV64_` | `PC=0, SP=1, FP=2, RA=3, GP=4, TP=5, T0..T2=6..8, S1=9, A0..A7=10..17, S2..S11=18..27, T3..T6=28..31, F0..F31=32..63` |

### Debugger IPC control block baseline

`CorDBIPC_BUFFER_SIZE` is 2092 for `TARGET_X86`/`TARGET_ARM` and 4016 for
other targets. These are byte capacities, not actual event lengths.
The native tables below describe same-bitness builds, not the debugger-only
wrapper containing `m_version` and `m_dcb2` or cross-bitness conversion.

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
Its buffer size is 4016; this describes the conditional structure definition,
not support for live debugging on those targets.

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

