// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++ BUILD Version: 0000     Increment this if a change has global effects



Module Name:

    dbghelp.h

Abstract:

    This module defines the prototypes and constants required for the image
    help routines.

    Contains debugging support routines that are redistributable.

Revision History:

--*/

#ifndef _DBGHELP_
#define _DBGHELP_

#if _MSC_VER > 1020
#pragma once
#endif

//
// options that are set/returned by SymSetOptions() & SymGetOptions()
// these are used as a mask
//
#define SYMOPT_LOAD_LINES                0x00000010

#endif // _DBGHELP_
