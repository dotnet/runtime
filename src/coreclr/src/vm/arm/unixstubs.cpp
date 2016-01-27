// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
