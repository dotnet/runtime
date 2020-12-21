// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// stdafx.h
//

//
// Common include file for utility code.
//*****************************************************************************

#define _CRT_DEPENDENCY_  //this code depends on the crt file functions
#include <crtwrap.h>
#include <string.h>
#include <limits.h>
#include <stdio.h>
#include <stddef.h>
#include <stdlib.h>		// for qsort
#include <windows.h>
#include <time.h>

#include <corerror.h>
#include <utilcode.h>

#include <corpriv.h>

#include "pesectionman.h"

#include "ceegen.h"
#include "ceesectionstring.h"
