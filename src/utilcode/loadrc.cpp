//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// Loads an localized string resource file
// this is used by sn.exe, ildasm.exe, peverify.exe, gacutil.exe, and fuslogvw.exe
// To use it by itself (not requiring utilcode*.lib), you'll need to use this CPP file
// along with safewrap, sstring
//


#include "stdafx.h"
#include "sstring.h"
#define USE_SSTRING
#include "loadrc-impl.cpp"

