// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef IJWHOST_H
#define IJWHOST_H

#include "pal.h"
#include "pedecoder.h"

using DllMain_t = BOOL(STDMETHODCALLTYPE*)(HINSTANCE hInst, DWORD dwReason, LPVOID lpReserved);

bool patch_vtable_entries(PEDecoder& decoder);
void release_bootstrap_thunks(PEDecoder& decoder);
bool are_thunks_installed_for_module(HMODULE instance);

using load_in_memory_assembly_fn = void(STDMETHODCALLTYPE*)(pal::dll_t handle, const pal::char_t* path);

pal::hresult_t get_load_in_memory_assembly_delegate(pal::dll_t handle, load_in_memory_assembly_fn* delegate);

extern HANDLE g_heapHandle;

#endif
