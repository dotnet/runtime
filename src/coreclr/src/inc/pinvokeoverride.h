// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 **                                                                         **
 ** pinvokeoverride.h - PInvoke binding override                            **
 **                                                                         **
 *****************************************************************************/

#ifndef _PINVOKEOVERRIDE_H_
#define _PINVOKEOVERRIDE_H_

typedef const void* (PInvokeOverrideFn)(const char* libraryName, const char* entrypointName);

class PInvokeOverride
{
public:
    static void SetPInvokeOverride(PInvokeOverrideFn* overrideImpl);
    static const void* GetMethodImpl(const char* libraryName, const char* entrypointName);
};

#endif // _PINVOKEOVERRIDE_H_
