// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: stdafx.h
//

//
//*****************************************************************************
/* XXX Fri 10/14/2005
 * prevent winioctl from defining something called "Unknown"
 */
#define _WINIOCTL_

// Define ALLOW_VMPTR_ACCESS to grant DAC access to VMPTR
#define ALLOW_VMPTR_ACCESS

// Prevent the inclusion of Random.h from disabling rand().  rand() is used by some other headers we include
// and there's no reason why DAC should be forbidden from using it.
#define DO_NOT_DISABLE_RAND

#define USE_COM_CONTEXT_DEF

#include <stddef.h>
#include <stdint.h>
#include <windows.h>

#include <winwrap.h>

#ifdef HOST_WINDOWS
#include <dbghelp.h>
#endif

#include <wchar.h>
#include <stdio.h>

#include <dbgtargetcontext.h>

#include <cor.h>
#include <dacprivate.h>
#include <sospriv.h>

#include <common.h>
#include <codeman.h>
#include <debugger.h>
#include <controller.h>
#include <eedbginterfaceimpl.h>
#include <methoditer.h>

#include <xcordebug.h>
#include "dacimpl.h"


#define STRSAFE_NO_DEPRECATE
#include <strsafe.h>
#undef _ftcscat
#undef _ftcscpy

// from ntstatus.h
#define STATUS_STOWED_EXCEPTION          ((NTSTATUS)0xC000027BL)

// unpublished Windows structures. these will be published soon in a new header.
// copying here for now and we'll use the windows header when it's available.
// this is tracked with issue 824225
#ifndef _STOWED_EXCEPTION_TEMP_DEFINITION
#define _STOWED_EXCEPTION_TEMP_DEFINITION

typedef struct _STOWED_EXCEPTION_INFORMATION_HEADER {
    ULONG Size;
    ULONG Signature;
} STOWED_EXCEPTION_INFORMATION_HEADER, *PSTOWED_EXCEPTION_INFORMATION_HEADER;

typedef struct _STOWED_EXCEPTION_INFORMATION_V2 {
    STOWED_EXCEPTION_INFORMATION_HEADER Header;

    HRESULT ResultCode;

    struct {
        DWORD ExceptionForm : 2;
        DWORD ThreadId : 30;
    };

    union {
        struct {
            PVOID ExceptionAddress;

            ULONG StackTraceWordSize;   // sizeof (PVOID)
            ULONG StackTraceWords;      // number of words pointed to by StackTrace
            PVOID StackTrace;           // StackTrace buffer
        };
        struct {
            PWSTR ErrorText;
        };
    };

    ULONG NestedExceptionType;
    PVOID NestedException;              // opaque exception addendum
} STOWED_EXCEPTION_INFORMATION_V2, *PSTOWED_EXCEPTION_INFORMATION_V2;

//
// Nested exception: type definition macro (byte swap). Assumes little-endian.
//
#define STOWED_EXCEPTION_NESTED_TYPE(t) ((((((ULONG)(t)) >> 24) & 0xFF) <<  0) | \
                                         (((((ULONG)(t)) >> 16) & 0xFF) <<  8) | \
                                         (((((ULONG)(t)) >>  8) & 0xFF) << 16) | \
                                         (((((ULONG)(t)) >>  0) & 0xFF) << 24))

#define STOWED_EXCEPTION_INFORMATION_V2_SIGNATURE 'SE02'
#define STOWED_EXCEPTION_NESTED_TYPE_LEO    STOWED_EXCEPTION_NESTED_TYPE('LEO1')    // language exception object

#endif // _STOWED_EXCEPTION_TEMP_DEFINITION
