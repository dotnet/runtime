// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// Contexts.CPP
//

// 
// Implementation for class Context
//


#include "common.h"


#ifdef DACCESS_COMPILE

void
Context::EnumMemoryRegions(CLRDataEnumMemoryFlags flags)
{
    SUPPORTS_DAC;
    DAC_ENUM_DTHIS();

    if (m_pDomain.IsValid())
    {
        m_pDomain->EnumMemoryRegions(flags, true);
    }
}
#endif // #ifdef DACCESS_COMPILE
