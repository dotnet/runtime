// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: variant.cpp
//
// PALRT variant conversion functions
// ===========================================================================

#include "common.h"

/***
*PUBLIC void VariantInit(VARIANT*)
*Purpose:
*  Initialize the given VARIANT to VT_EMPTY.
*
*Entry:
*  None
*
*Exit:
*  return value = void
*
*  pvarg = pointer to initialized VARIANT
*
***********************************************************************/
STDAPI_(void)
VariantInit(VARIANT FAR* pvarg)
{
    V_VT(pvarg) = VT_EMPTY;
}
