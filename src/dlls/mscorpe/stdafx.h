//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//
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
