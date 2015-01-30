//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// 

#include "stdafx.h"
#include "threads.h"
#include "../../shared/arm64/primitives.cpp"

void CopyREGDISPLAY(REGDISPLAY* pDst, REGDISPLAY* pSrc)
{
    CONTEXT tmp;
    CopyRegDisplay(pSrc, pDst, &tmp);
}
