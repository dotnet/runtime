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
