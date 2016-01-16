//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once


#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN		// Exclude rarely-used stuff from Windows headers
#endif

#include <stdio.h>
#include <tchar.h>
#include <string.h>
#include <comdef.h>

#define _WIN32_FUSION 0x0100 // this causes activation context 

// TODO: reference additional headers your program requires here
#include "dia2.h"

