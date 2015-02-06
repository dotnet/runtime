//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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