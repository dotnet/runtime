// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Header:  ShimLoad.hpp
**
** Purpose: Delay load hook used to images to bind to
**          dll's shim shipped with the EE
**
**
===========================================================*/
#ifndef _SHIMLOAD_H
#define _SHIMLOAD_H


//*****************************************************************************
// Sets/Gets the directory based on the location of the module. This routine
// is called at COR setup time. Set is called during EEStartup and by the
// MetaData dispenser.
//*****************************************************************************
HRESULT SetInternalSystemDirectory();
HRESULT GetInternalSystemDirectory(_Out_writes_opt_(*pdwLength) LPWSTR buffer, __inout DWORD* pdwLength);

#endif

