//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
/* ------------------------------------------------------------------------- *
 * DebuggerRegDisplayHelper.cpp -- implementation of the platform-dependent 
// 

 *                                 methods for transferring information between
 *                                 REGDISPLAY and DebuggerREGDISPLAY
 * ------------------------------------------------------------------------- */

#include "stdafx.h"


void CopyREGDISPLAY(REGDISPLAY* pDst, REGDISPLAY* pSrc)
{
    *pDst = *pSrc;
}
