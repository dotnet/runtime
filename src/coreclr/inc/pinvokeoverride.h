// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************
 **                                                                         **
 ** pinvokeoverride.h - PInvoke binding override                            **
 **                                                                         **
 *****************************************************************************/

#ifndef _PINVOKEOVERRIDE_H_
#define _PINVOKEOVERRIDE_H_

#include "coreclrhost.h"

class PInvokeOverride
{
public:
    // Override source. This represents the priority order in which overrides will be called.
    enum class Source
    {
        RuntimeConfiguration,
        ObjectiveCInterop,
        Last = ObjectiveCInterop,
    };

    static void SetPInvokeOverride(_In_ PInvokeOverrideFn* overrideImpl, _In_ Source source);
    static const void* GetMethodImpl(_In_z_ const char* libraryName, _In_z_ const char* entrypointName);
};

#endif // _PINVOKEOVERRIDE_H_
