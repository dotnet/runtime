// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// CrtWrap.h
//
// Wrapper code for the C runtime library.
//
//*****************************************************************************

#ifndef __CrtWrap_h__
#define __CrtWrap_h__

#include <stdint.h>
#include <stddef.h>
#include <windows.h>
#include <objbase.h>
#include "debugmacros.h"
#include <stdlib.h>
#if !defined(CLR_CMAKE_HOST_APPLE)
#include <malloc.h>
#endif
#include <wchar.h>
#include <stdio.h>

#ifdef HOST_WINDOWS
// CoreCLR.dll uses linker .def files to control the exported symbols.
// Define DLLEXPORT macro as empty on Windows.
#define DLLEXPORT
#endif

#endif // __CrtWrap_h__
