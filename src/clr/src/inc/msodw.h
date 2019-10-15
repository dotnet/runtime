// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#pragma once

/****************************************************************************
    This files contains the handshake structure with which apps will launch
    Watson.
****************************************************************************/

#ifndef MSODW_H
#define MSODW_H
#pragma pack(push, msodw_h)
#pragma pack(4)

#define DW_MAX_BUCKETPARAM_CWC  255

typedef struct _GenericModeBlock
{
    BOOL fInited;
    WCHAR wzEventTypeName[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP1[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP2[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP3[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP4[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP5[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP6[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP7[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP8[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP9[DW_MAX_BUCKETPARAM_CWC];
    WCHAR wzP10[DW_MAX_BUCKETPARAM_CWC];
} GenericModeBlock;

#pragma pack(pop, msodw_h)
#endif // MSODW_H
