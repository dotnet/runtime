// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
//*****************************************************************************
// File: IPCFuncCallImpl.cpp
//
// Implement support for a cross process function call. 
//
//*****************************************************************************

#include "stdafx.h"
#include "ipcfunccall.h"
#include "ipcshared.h"



// Telesto stubs

//-----------------------------------------------------------------------------
// Wrap an unsafe call in a mutex to assure safety
// Biggest error issues are:
// 1. Timeout (probably handler doesn't exist)
// 2. Handler can be destroyed at any time.
//-----------------------------------------------------------------------------
IPCFuncCallSource::EError IPCFuncCallSource::DoThreadSafeCall()
{
    return Ok;
}

