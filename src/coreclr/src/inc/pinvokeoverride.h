// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 **                                                                         **
 ** pinvokeoverride.h - PInvoke binding override                            **
 **                                                                         **
 *****************************************************************************/

#ifndef _PINVOKEOVERRIDE_H_
#define _PINVOKEOVERRIDE_H_

#ifndef _MSC_VER
#define __stdcall
#endif

typedef const void* (__stdcall PInvokeOverrideFn)(const char* libraryName, const char* entrypointName);

class PInvokeOverride
{
private:
    static PInvokeOverrideFn* s_overrideImpl;

public:
    static void SetPInvokeOverride(PInvokeOverrideFn* overrideImpl);
    static const void* TryGetMethodImpl(const char* libraryName, const char* entrypointName);
};

#endif // _PINVOKEOVERRIDE_H_
// EOF =======================================================================
