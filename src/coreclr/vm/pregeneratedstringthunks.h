// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#pragma once

#include "daccess.h"

// Initialize the global pregenerated string thunk hash table.
// Must be called during EE startup before any R2R module loading.
void InitializePregeneratedStringThunkHash();

// Look up a pregenerated thunk by its string key.
// Returns NULL if the string is not found in the table.
PCODE LookupPregeneratedThunkByString(const char* str);

// Process a READYTORUN_FIXUP_InjectStringThunks fixup, adding new entries to the global hash.
// moduleBase is the base address of the R2R image.
// pBlob points to the first byte after the fixup kind byte in the signature.
void ProcessInjectStringThunksFixup(TADDR moduleBase, PCCOR_SIGNATURE pBlob);
