// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "corinfoexception.h"
#include "dllexport.h"

DLL_EXPORT CorInfoException* AllocException(const WCHAR* message, int messageLength)
{
    return new CorInfoException(message, messageLength);
}

DLL_EXPORT void FreeException(CorInfoException* pException)
{
    delete pException;
}

DLL_EXPORT const WCHAR* GetExceptionMessage(const CorInfoException* pException)
{
    return pException->GetMessage();
}
