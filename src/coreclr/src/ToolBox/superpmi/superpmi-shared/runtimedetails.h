//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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
