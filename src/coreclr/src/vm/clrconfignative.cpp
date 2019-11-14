// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// File: clrconfiguration.cpp
//

#include "common.h"
#include "clrconfignative.h"
#include <configuration.h>

BOOL QCALLTYPE ClrConfigNative::GetConfigBoolValue(LPCWSTR name, BOOL *exist)
{
    QCALL_CONTRACT;

    BOOL retValue = FALSE;
    *exist = FALSE;
    BEGIN_QCALL;

    if (Configuration::GetKnobStringValue(name) != nullptr)
    {
        *exist = TRUE;
        retValue = Configuration::GetKnobBooleanValue(name, FALSE);
    }
    END_QCALL;
    return(retValue);
}
