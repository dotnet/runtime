//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

#include "common.h"

extern "C"
{
    void RedirectForThrowControl()
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

    void GenericPInvokeCalliHelper()
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

    void PInvokeStubForHostInner(DWORD dwStackSize, LPVOID pStackFrame, LPVOID pTarget)
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }

    void VarargPInvokeStub()
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }
    
    void VarargPInvokeStub_RetBuffArg()
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }
    
    void RedirectForThreadAbort()
    {
        PORTABILITY_ASSERT("Implement for PAL");
    }
};
