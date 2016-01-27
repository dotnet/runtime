// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "common.h"

#include "coverage.h"


//
//  This is part of the runtime test teams Code Coverge Tools. Due to the special nature of MSCORLIB.dll
//  We have to work around several issues (Like the initilization of the Secutiry Manager) to be able to get
//  Code coverage on mscorlib.dll
// 

FCIMPL1(unsigned __int64, COMCoverage::nativeCoverBlock, INT32 id)
{
    FCALL_CONTRACT;

    unsigned __int64 retVal = 0;
    HELPER_METHOD_FRAME_BEGIN_RET_0();

    HMODULE ilcovnat = 0;
    if (id == 1)
    {
        ilcovnat = CLRLoadLibrary(W("Ilcovnat.dll"));

        if (ilcovnat)
        {
            retVal = (unsigned __int64)GetProcAddress(ilcovnat, "CoverBlockNative");
        }
    }
    else if (id == 2)
    {
        ilcovnat = CLRLoadLibrary(W("coverage.dll"));

        if (ilcovnat)
        {
            retVal = (unsigned __int64)GetProcAddress(ilcovnat, "CoverageRegisterBinaryWithStruct");
        }
    }
    else if (id == 3)
    {
        ilcovnat = CLRLoadLibrary(W("Ilcovnat.dll"));
        if (ilcovnat)
        {
            retVal = (unsigned __int64)GetProcAddress(ilcovnat, "CoverMonRegisterMscorlib");
        }
    }

    HELPER_METHOD_FRAME_END();
    return retVal;
}
FCIMPLEND
