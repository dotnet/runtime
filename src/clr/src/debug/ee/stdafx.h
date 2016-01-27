// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: stdafx.h
// 

//
//*****************************************************************************
#include <stdint.h>
#include <wchar.h>
#include <stdio.h>

#include <windows.h>
#if !defined(FEATURE_CORECLR)
#undef GetCurrentTime // works around a macro def conflict of GetCurrentTime
#include <windows.ui.xaml.h>
#endif // !FEATURE_CORECLR

#include <switches.h>
#include <winwrap.h>

#ifdef DACCESS_COMPILE
#include <specstrings.h>
#endif

#include <util.hpp>

#include <dbgtargetcontext.h>

#include <cordbpriv.h>
#include <dbgipcevents.h>
#include "debugger.h"
#include "walker.h"
#include "controller.h"
#include "frameinfo.h"
#include <corerror.h>
#include "../inc/common.h"

