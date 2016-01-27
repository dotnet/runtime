// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ===========================================================================
// File: pch.h
//

// ===========================================================================

#ifndef _ILDBSYMLIB_PCH_H_
#define _ILDBSYMLIB_PCH_H_

#include "ole2.h"

#include "winwrap.h"
#include "umisc.h"

#include "corhdr.h"
#include "corsym.h"
#include "palclr.h"
#include "cor.h"
#include "genericstackprobe.h"

// I'm not sure why this code uses these macros for memory management (they should at least be
// in-line functions).  DELETE is a symbol defined in WinNt.h as an access-type.  We're probably
// not going to try and use that, so we'll just override it for now. 
#ifdef DELETE
#undef DELETE
#endif


#define NEW( x ) ( ::new (nothrow) x )
#define DELETE( x ) ( ::delete(x) )
#define DELETEARRAY( x ) (::delete[] (x))

#include "ildbsymlib.h"
#include "symwrite.h"
#include "symread.h"
#include "symbinder.h"

#endif
