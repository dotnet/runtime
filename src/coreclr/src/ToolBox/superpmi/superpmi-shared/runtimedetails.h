//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// RuntimeDetails.h - the collection of runtime includes that we need to access.
//----------------------------------------------------------
#ifndef _RuntimeDetails
#define _RuntimeDetails

// Our little collection of enough of the CLR data to get the JIT up and working...
#define FEATURE_CLRSQM

#if !defined(TARGET_AMD64) && !defined(TARGET_X86) && !defined(TARGET_ARM64) && !defined(TARGET_ARM)
#if defined(_M_X64)
#define TARGET_AMD64 1
#elif defined(_M_IX86)
#define TARGET_X86 1
#endif
#endif // _TARGET_* not previously defined

#define __EXCEPTION_RECORD_CLR // trick out clrntexception.h to not include another exception record....

#include <mscoree.h>
#include <corjit.h>
#include <utilcode.h>

/// Turn back on direct access to a few OS level things...
#undef HeapCreate
#undef HeapAlloc
#undef HeapFree
#undef HeapDestroy

// Jit Exports
typedef ICorJitCompiler*(__stdcall* PgetJit)();
typedef void(__stdcall* PjitStartup)(ICorJitHost* host);

#endif
