// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef __callhelpers_h__
#define __callhelpers_h__

struct StringToWasmSigThunk
{
    const char* key;
    void*       value;
};

extern const StringToWasmSigThunk g_wasmThunks[];
extern const size_t g_wasmThunksCount;

#endif // __callhelpers_h__
