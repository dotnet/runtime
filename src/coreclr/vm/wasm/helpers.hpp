// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#pragma once

// Forward declaration for explicit initialization
void InitializeWasmThunkCaches();

class MethodDesc;

// Look up a pregenerated R2R-to-interpreter thunk for the given MethodDesc.
// Returns NULL if no thunk is available for the method's signature.
void* GetPortableEntryPointToInterpreterThunk(MethodDesc *pMD);

// Look up a pregenerated vtable dispatch thunk for the given MethodDesc.
// Returns NULL if no thunk is available for the method's signature.
void* GetVirtualDispatchThunk(MethodDesc *pMD);

// Returns true when the method's result uses the hidden return-buffer form of the Wasm ABI.
bool WasmMethodReturnsViaRetBuf(MethodDesc* pMD);

// Look up a pregenerated closed-static return-buffer thunk for a delegate Invoke method.
// Returns NULL if no thunk is available for the method's signature.
void* GetClosedStaticRetBufThunk(MethodDesc* pDelegateInvoke);

// Get a pregenerated unboxing stub for pMD, the MethodDesc used as its generic context,
// and the portable entrypoint of its target.
// Returns NULL if no stub is available for the method's signature.
void* GetUnboxingStub(MethodDesc* pMD, MethodDesc** ppTargetMethodDesc, PCODE* pTargetEntryPoint);
