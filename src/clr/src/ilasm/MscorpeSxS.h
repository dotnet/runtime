// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
