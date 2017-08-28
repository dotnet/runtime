//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


//
// Base class for UWP hosts WinRT and Exe hosts
//
#ifndef __UWPHOST_H__
#define __UWPHOST_H__
#include "HostEnvironment.h"
#include "corerror.h"

class UWPHost
{
private:
    ICLRRuntimeHost2* m_CLRRuntimeHost;
public:
    static bool m_Inited;
    static CRITICAL_SECTION m_critSec;

    UWPHost()
    : m_CLRRuntimeHost(nullptr)
    {};

    ICLRRuntimeHost2* GetCLRRuntimeHost()
    {
        return m_CLRRuntimeHost;
    }

    HRESULT LoadAndStartCoreCLRRuntime();
};


#endif // __UWPHOST_H__
