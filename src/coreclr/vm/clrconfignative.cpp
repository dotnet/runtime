// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: clrconfiguration.cpp
//

#include "common.h"
#include "clrconfignative.h"
#include <configuration.h>

extern "C" BOOL QCALLTYPE ClrConfig_GetConfigBoolValue(LPCWSTR name, BOOL *exist, QCallExceptionStatus* qcallError)
{
    QCALL_CONTRACT;

    *exist = FALSE;
    BOOL retValue = FALSE;

    BEGIN_QCALL;

    if (Configuration::GetKnobStringValue(name) != nullptr)
    {
        *exist = TRUE;
        retValue = Configuration::GetKnobBooleanValue(name, FALSE);
    }
    END_QCALL;
    return(retValue);
}
