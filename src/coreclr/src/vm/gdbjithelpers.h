// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: gdbjithelpers.h
//
//
// Helper file with managed delegate for GDB JIT interface implemenation.
//
//*****************************************************************************


#ifndef __GDBJITHELPERS_H__
#define __GDBJITHELPERS_H__

struct SequencePointInfo
{
    int lineNumber, ilOffset;
    char16_t* fileName;
};

struct MethodDebugInfo
{
    SequencePointInfo* points;
    int size;
    char16_t** locals;
    int localsSize;
};

typedef BOOL (*GetInfoForMethodDelegate)(const char*, unsigned int, MethodDebugInfo& methodDebugInfo);
extern GetInfoForMethodDelegate getInfoForMethodDelegate;

#endif // !__GDBJITHELPERS_H__
