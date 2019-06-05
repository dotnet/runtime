// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdio.h>
#include <stdlib.h>
#include <xplatform.h>
#include <platformdefines.h>

using FAKE_HANDLE = HANDLE;

extern "C" void DLL_EXPORT GetFakeHandle(void* value, FAKE_HANDLE* pHandle, void** pCookie)
{
    *pHandle = (FAKE_HANDLE)value;
    *pCookie = (void*)4567; // the value here does not matter. It just needs to not be nullptr.
}
