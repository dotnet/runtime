// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#pragma once

#include "shash.h"
#include "utilcode.h"

// Hash traits for mapping C strings (const char*) to void pointers.
// Used for string-to-thunk lookup tables, both for WASM thunk caches
// and for ReadyToRun pregenerated string thunks.
class StringThunkSHashTraits : public MapSHashTraits<const char*, void*>
{
public:
    static BOOL Equals(const char* s1, const char* s2) { return strcmp(s1, s2) == 0; }
    static count_t Hash(const char* key) { return HashStringA(key); }
};

typedef MapSHash<const char*, void*, NoRemoveSHashTraits<StringThunkSHashTraits>> StringToThunkHash;
