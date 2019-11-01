// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: CAHLPR.H
//
// 
//
//*****************************************************************************
#ifndef __CAHLPR_H__
#define __CAHLPR_H__

#include "caparser.h"

//*****************************************************************************
// This class assists in the parsing of CustomAttribute blobs.
//*****************************************************************************
struct CaValue
{
    union
    {
        signed __int8       i1;
        unsigned __int8     u1;
        signed __int16      i2;
        unsigned __int16    u2;
        signed __int32      i4;
        unsigned __int32    u4;
        signed __int64      i8;
        unsigned __int64    u8;
        float               r4;
        double              r8;
        struct
        {
            LPCUTF8         pStr;
            ULONG           cbStr;
        } str;
    };
    unsigned __int8         tag;
};

#endif // __CAHLPR_H__
