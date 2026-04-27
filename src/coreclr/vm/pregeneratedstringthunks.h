// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#pragma once

#include "daccess.h"

class LoaderAllocator;

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

#ifdef FEATURE_PORTABLE_ENTRYPOINTS
// Initialize the lock used for pending thunk resolution tracking.
void InitializePendingThunkResolutionLock();

// Add a MethodDesc to its LoaderAllocator's pending list under the global lock.
// Registers the LoaderAllocator if not already registered.
void AddPendingPortableEntryPointThunkUnderLock(LoaderAllocator* pLoaderAllocator, MethodDesc* pMD);

// Unregister a LoaderAllocator from the global pending thunk resolution list.
// Called during LoaderAllocator::Destroy.
void UnregisterLoaderAllocatorForPendingThunkResolution(LoaderAllocator* pLoaderAllocator);

// After new thunks are injected, resolve pending methods across all registered LoaderAllocators.
void ResolvePendingPortableEntryPointThunksGlobal();
#endif // FEATURE_PORTABLE_ENTRYPOINTS
