//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
//
// File: Currency.cpp
//

//

#include "common.h"
#include "object.h"
#include "excep.h"
#include "frames.h"
#include "vars.hpp"
#include "currency.h"
#include "string.h"


FCIMPL2(void, COMCurrency::DoToDecimal, DECIMAL * result, CY c)
{
    FCALL_CONTRACT;

    // GC could only happen when exception is thrown, no need to protect result
    HELPER_METHOD_FRAME_BEGIN_0();

    _ASSERTE(result);
    HRESULT hr = VarDecFromCy(c, result);
    if (FAILED(hr))
    {
        // Didn't expect to get here.  Update code for this HR.
        _ASSERTE(S_OK == hr);
        COMPlusThrowHR(hr);
    }

    if (FAILED(DecimalCanonicalize(result)))
        COMPlusThrow(kOverflowException, W("Overflow_Currency"));
    
    result->wReserved = 0;

    HELPER_METHOD_FRAME_END();
}
FCIMPLEND
