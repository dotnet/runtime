//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
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
