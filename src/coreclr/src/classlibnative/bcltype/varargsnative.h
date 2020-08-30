// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: VarArgsNative.h
//

//
// This module contains the implementation of the native methods for the
//  varargs class(es)..
//

#ifndef _VARARGSNATIVE_H_
#define _VARARGSNATIVE_H_

#include "clrvarargs.h"

class VarArgsNative
{
public:
    static FCDECL3(void, Init2, VARARGS* _this, LPVOID cookie, LPVOID firstArg);
    static FCDECL2(void, Init, VARARGS* _this, LPVOID cookie);
    static FCDECL1(int, GetRemainingCount, VARARGS* _this);
    static FCDECL1(void*, GetNextArgType, VARARGS* _this);
    //TypedByRef can not be passed by ref, so has to pass it as void pointer
    static FCDECL2(void, DoGetNextArg, VARARGS* _this, void * value);
    //TypedByRef can not be passed by ref, so has to pass it as void pointer
    static FCDECL3(void, GetNextArg2, VARARGS* _this, void * value, ReflectClassBaseObject *pTypeUNSAFE);

    static void GetNextArgHelper(VARARGS *data, TypedByRef *value, BOOL fData);
};

#endif // _VARARGSNATIVE_H_
