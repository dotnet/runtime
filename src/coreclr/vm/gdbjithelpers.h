// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// File: gdbjithelpers.h
//
//
// Helper file with managed delegate for GDB JIT interface implementation.
//
//*****************************************************************************


#ifndef __GDBJITHELPERS_H__
#define __GDBJITHELPERS_H__

struct SequencePointInfo
{
    int lineNumber, ilOffset;
    WCHAR* fileName;
};

struct LocalVarInfo
{
    int startOffset;
    int endOffset;
    WCHAR *name;
};

struct MethodDebugInfo
{
    SequencePointInfo* points;
    int size;
    LocalVarInfo* locals;
    int localsSize;

    MethodDebugInfo(int numPoints, int numLocals);
    ~MethodDebugInfo();
};

typedef BOOL (CALLBACK *GetInfoForMethodDelegate)(const char*, unsigned int, MethodDebugInfo& methodDebugInfo);
extern GetInfoForMethodDelegate getInfoForMethodDelegate;

#endif // !__GDBJITHELPERS_H__
