// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
        int8_t      i1;
        uint8_t     u1;
        int16_t     i2;
        uint16_t    u2;
        int32_t     i4;
        uint32_t    u4;
        int64_t     i8;
        uint64_t    u8;
        float       r4;
        double      r8;
        struct
        {
            LPCUTF8         pStr;
            ULONG           cbStr;
        } str;
    };
    uint8_t         tag;
};

#endif // __CAHLPR_H__
