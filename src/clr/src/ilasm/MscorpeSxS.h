//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//
// 
// File: MscorpeSxS.h
// 

// 
// This file defines a wrapper for SxS version of mscorpe.dll (dynamically loaded via shim).
// 

#pragma once

#include <MscorpeSxSWrapper.h>

// Loads mscorpe.dll (uses shim)
HRESULT LoadMscorpeDll(HMODULE * phModule);

// Wrapper for mscorpe.dll calls
typedef MscorpeSxSWrapper<LoadMscorpeDll> MscorpeSxS;
