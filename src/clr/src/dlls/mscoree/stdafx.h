//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//*****************************************************************************
// stdafx.h
//
// Precompiled headers.
//
//*****************************************************************************
#ifndef __STDAFX_H__
#define __STDAFX_H__

#include <crtwrap.h>
#include <winwrap.h>                    // Windows wrappers.

#include <ole2.h>						// OLE definitions


#include "intrinsic.h"					// Functions to make intrinsic.


// Helper function returns the instance handle of this module.
HINSTANCE GetModuleInst();

#endif  // __STDAFX_H__
