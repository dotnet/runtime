//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//
// 
// File: MscorpeSxS.cpp
// 

// 
// This file defines a wrapper for SxS version of mscorpe.dll (dynamically loaded via shim).
// 
#include "ilasmpch.h"

#include "MscorpeSxS.h"

#include <LegacyActivationShim.h>

// Loads mscorpe.dll (uses shim)
HRESULT 
LoadMscorpeDll(HMODULE * phModule)
{
    // Load SxS version of mscorpe.dll (i.e. mscorpehost.dll) and initialize it
    return LegacyActivationShim::LoadLibraryShim(L"mscorpe.dll", NULL, NULL, phModule);
}
