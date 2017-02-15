// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef _H_CONTEXT_
#define _H_CONTEXT_

#include "specialstatics.h"
#include "fcall.h"

#ifdef FEATURE_COMINTEROP
class RCWCache;
#endif // FEATURE_COMINTEROP

typedef DPTR(class Context) PTR_Context;


// there will be only the default context for each appdomain
// and contexts will not be exposed to users (so there will be no managed Context class)

class Context
{
    PTR_AppDomain m_pDomain;

public:
#ifndef DACCESS_COMPILE
    Context(AppDomain *pDomain)
    {
        m_pDomain = pDomain;
    }
#endif

    PTR_AppDomain GetDomain()
    {
        LIMITED_METHOD_DAC_CONTRACT;
        return m_pDomain;
    }

    static void Initialize()
    {
    }

    typedef void (*ADCallBackFcnType)(LPVOID);

#ifdef DACCESS_COMPILE
    void EnumMemoryRegions(CLRDataEnumMemoryFlags flags);
#endif
};


#endif
