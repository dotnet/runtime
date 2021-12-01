// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
////////////////////////////////////////////////////////////////////////////////
// This module contains the implementation of the native methods for the
//  varargs class(es)..
//

////////////////////////////////////////////////////////////////////////////////

#ifndef _CLRVARARGS_H_
#define _CLRVARARGS_H_


struct VARARGS
{
    VASigCookie *ArgCookie;
    SigPointer  SigPtr;
    BYTE        *ArgPtr;
    int         RemainingArgs;

    static DWORD CalcVaListSize(VARARGS *data);
    static void MarshalToManagedVaList(va_list va, VARARGS *dataout);
    static void MarshalToUnmanagedVaList(va_list va, DWORD cbVaListSize, const VARARGS *data);
};

#endif // _CLRVARARGS_H_
