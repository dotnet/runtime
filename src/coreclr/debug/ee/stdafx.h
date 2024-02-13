// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: stdafx.h
//

//
//*****************************************************************************

#define USE_COM_CONTEXT_DEF

#include <stdint.h>
#include <wchar.h>
#include <stdio.h>
#include <algorithm>

#include <windows.h>

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

