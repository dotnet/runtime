// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//----------------------------------------------------------
// RuntimeDetails.h - the collection of runtime includes that we need to access.
//----------------------------------------------------------
#ifndef _RuntimeDetails
#define _RuntimeDetails

#include <mscoree.h>
#include <corjit.h>
#include <utilcode.h>
#include <patchpointinfo.h>

// Jit Exports
typedef ICorJitCompiler*(__stdcall* PgetJit)();
typedef void(__stdcall* PjitStartup)(ICorJitHost* host);

#endif
