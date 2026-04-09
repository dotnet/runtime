// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "corinfoexception.h"
#include "dllexport.h"

DLL_EXPORT CorInfoExceptionClass* AllocException(const WCHAR* message, int messageLength)
{
    return new CorInfoExceptionClass(message, messageLength);
}

DLL_EXPORT void FreeException(CorInfoExceptionClass* pException)
{
    delete pException;
}

DLL_EXPORT const WCHAR* GetExceptionMessage(const CorInfoExceptionClass* pException)
{
    return pException->GetMessage();
}
