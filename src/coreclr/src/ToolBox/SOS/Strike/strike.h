// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
#ifndef __strike_h__
#define __strike_h__

#ifndef _countof
#define _countof(x) (sizeof(x)/sizeof(x[0]))
#endif

#if defined(_MSC_VER)
#pragma warning(disable:4245)   // signed/unsigned mismatch
#pragma warning(disable:4100)   // unreferenced formal parameter
#pragma warning(disable:4201)   // nonstandard extension used : nameless struct/union
#pragma warning(disable:4127)   // conditional expression is constant
#pragma warning(disable:6255)   // Prefast: alloca indicates failure by raising a stack overflow exception
#endif

#ifdef PAL_STDCPP_COMPAT
#define _wcslen     PAL_wcslen
#define _wcsncmp    PAL_wcsncmp
#define _wcsrchr    PAL_wcsrchr
#define _wcscmp     PAL_wcscmp
#define _wcschr     PAL_wcschr
#define _wcscspn    PAL_wcscspn
#define _wcscat     PAL_wcscat
#define _wcsstr     PAL_wcsstr
#else // PAL_STDCPP_COMPAT
#define _wcslen     wcslen
#define _wcsncmp    wcsncmp
#define _wcsrchr    wcsrchr
#define _wcscmp     wcscmp
#define _wcschr     wcschr
#define _wcscspn    wcscspn
#define _wcscat     wcscat
#define _wcsstr     wcsstr
#endif // !PAL_STDCPP_COMPAT

#define ___in       _SAL1_Source_(__in, (), _In_)
#define ___out      _SAL1_Source_(__out, (), _Out_)

#define _max(a, b) (((a) > (b)) ? (a) : (b))
#define _min(a, b) (((a) < (b)) ? (a) : (b))

#include <winternl.h>
#include <winver.h>
#include <windows.h>
    
#include <wchar.h>

//#define NOEXTAPI
#define KDEXT_64BIT
#include <wdbgexts.h>
#undef DECLARE_API
#undef GetContext
#undef SetContext
#undef ReadMemory
#undef WriteMemory
#undef GetFieldValue
#undef StackTrace

#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>


#ifndef PAL_STDCPP_COMPAT
#include <malloc.h>
#endif

#ifdef FEATURE_PAL
#ifndef alloca
#define alloca  __builtin_alloca
#endif
#ifndef _alloca
#define _alloca __builtin_alloca
#endif
#endif // FEATURE_PAL

#include <stddef.h>

#ifndef FEATURE_PAL
#include <basetsd.h>  
#endif

#define  CORHANDLE_MASK 0x1

#include "static_assert.h"

// exts.h includes dbgeng.h which has a bunch of IIDs we need instantiated.
#define INITGUID
#include "guiddef.h"

#ifdef FEATURE_PAL
#define SOS_PTR(x) (size_t)(x)
#else // FEATURE_PAL
#define SOS_PTR(x) (unsigned __int64)(x)
#endif // FEATURE_PAL else

#include "exts.h"

//Alignment constant for allocation
#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)
#define ALIGNCONST 3
#else
#define ALIGNCONST 7
#endif

//The large object heap uses a different alignment
#define ALIGNCONSTLARGE 7

#ifdef _WIN64
#define SIZEOF_OBJHEADER    8
#else // !_WIN64
#define SIZEOF_OBJHEADER    4
#endif // !_WIN64

#define plug_skew           SIZEOF_OBJHEADER
#define min_obj_size        (sizeof(BYTE*)+plug_skew+sizeof(size_t))

extern BOOL CallStatus;


#ifndef NT_SUCCESS
#define NT_SUCCESS(Status) (((NTSTATUS)(Status)) >= 0)
#endif

HRESULT SetNGENCompilerFlags(DWORD flags);


#endif // __strike_h__
