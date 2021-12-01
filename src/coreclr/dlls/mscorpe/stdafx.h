// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// stdafx.h
//
// Common include file for utility code.
//*****************************************************************************
#include <stdlib.h>		// for qsort
#include <windows.h>
#include <time.h>
#include <assert.h>
#include <stdio.h>
#include <stddef.h>

#define FEATURE_NO_HOST     // Do not use host interface
#include <utilcode.h>

#include <corpriv.h>

#include "pewriter.h"
#include "ceegen.h"
#include "ceefilegenwriter.h"
#include "ceesectionstring.h"
