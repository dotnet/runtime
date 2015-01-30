//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

/*============================================================
**
** Class: COMMemoryFailPoint
**
**
** Purpose: Native methods for System.Runtime.MemoryFailPoint.
** These are to implement memory gates to limit allocations
** when progress will likely result in an OOM.
**
**
===========================================================*/

#ifndef _COMMEMORYFAILPOINT_H
#define _COMMEMORYFAILPOINT_H

#include "fcall.h"

class COMMemoryFailPoint
{
public:
    static FCDECL2(void, GetMemorySettings, UINT64* pMaxGCSegmentSize, UINT64* pTopOfMemory);
};

#endif // _COMMEMORYFAILPOINT_H
