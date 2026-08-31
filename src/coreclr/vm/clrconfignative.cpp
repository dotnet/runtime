// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: clrconfiguration.cpp
//

#include "common.h"
#include "clrconfignative.h"
#include <configuration.h>

extern "C" QCallExceptionStatus QCALLTYPE ClrConfig_GetConfigBoolValue(LPCWSTR name, BOOL *exist, BOOL* pReturnValue)
{
    QCALL_CONTRACT;

    *exist = FALSE;
    BEGIN_QCALL;

    BOOL retValue = FALSE;

    if (Configuration::GetKnobStringValue(name) != nullptr)
    {
        *exist = TRUE;
        retValue = Configuration::GetKnobBooleanValue(name, FALSE);
    }
    *pReturnValue = (retValue);

    END_QCALL;
}
