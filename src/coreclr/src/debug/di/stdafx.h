// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// stdafx.h
// 

//
// Common include file for utility code.
//*****************************************************************************
#include <stdio.h>
#include <windows.h>
#include <winnt.h>

#include <dbgtargetcontext.h>

#define RIGHT_SIDE_COMPILE

//-----------------------------------------------------------------------------
// Contracts for RS threading.
// We only do this for debug builds and not for inproc
//-----------------------------------------------------------------------------
#if defined(_DEBUG) 
    #define RSCONTRACTS
#endif


// In case of FEATURE_DBGIPC_TRANSPORT_DI we use pipe for debugger debugee communication
// and event redirection is not needed. (won't work anyway)
#ifndef FEATURE_DBGIPC_TRANSPORT_DI
// Currently, we only can redirect exception events. Since real interop-debugging
// neeeds all events, redirection can't work in real-interop. 
// However, whether we're interop-debugging is determined at runtime, so we always
// enable at compile time and then we need a runtime check later.
#define ENABLE_EVENT_REDIRECTION_PIPELINE
#endif

#include "ex.h"

#include "sigparser.h"
#include "corpub.h"
#include "rspriv.h"

// This is included to deal with GCC limitations around templates.
// For GCC, if a compilation unit refers to a templated class (like Ptr<T>), GCC requires the compilation
// unit to have T's definitions for anything that Ptr may call. 
// RsPriv.h has a RSExtSmartPtr<ShimProcess>, which will call ShimProcess::AddRef, which means the same compilation unit 
// must have the definition of ShimProcess::AddRef, and therefore the whole ShimProcess class. 
// CL.exe does not have this problem.
// Practically, this means that anybody that includes rspriv.h must include shimpriv.h. 
#include "shimpriv.h"

#ifdef _DEBUG
#include "utilcode.h"
#endif

#ifndef _TARGET_ARM_
#define DbiGetThreadContext(hThread, lpContext) ::GetThreadContext(hThread, (CONTEXT*)(lpContext))
#define DbiSetThreadContext(hThread, lpContext) ::SetThreadContext(hThread, (CONTEXT*)(lpContext))
#else
BOOL DbiGetThreadContext(HANDLE hThread, DT_CONTEXT *lpContext);
BOOL DbiSetThreadContext(HANDLE hThread, const DT_CONTEXT *lpContext);
#endif
