// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

