//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// RuntimeDetails.h - the collection of runtime includes that we need to access.
//----------------------------------------------------------
#ifndef _RuntimeDetails
#define _RuntimeDetails

//Our little collection of enough of the CLR data to get the JIT up and working...
#define FEATURE_CLRSQM
#ifdef _M_X64
#define _TARGET_AMD64_ 1
#endif
#ifdef _M_IX86
#define _TARGET_X86_ 1
#endif
#define __EXCEPTION_RECORD_CLR //trick out clrntexception.h to not include another exception record....
#include <mscoree.h>
#include <corjit.h>
#include <utilcode.h>

///Turn back on direct access to a few OS level things...
#undef HeapCreate
#undef HeapAlloc
#undef HeapFree
#undef HeapDestroy
#undef TlsAlloc
#undef TlsGetValue
#undef TlsSetValue

//Jit Exports
typedef ICorJitCompiler* (__stdcall *PgetJit)();
typedef void (__stdcall *PjitStartup)(ICorJitHost* host);
typedef void (__stdcall *PsxsJitStartup)(CoreClrCallbacks const & cccallbacks);

#endif
