// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_errno.h"

int32_t SystemNative_ConvertErrorPlatformToPal(int32_t platformErrno)
{
    return ConvertErrorPlatformToPal(platformErrno);
}

int32_t SystemNative_ConvertErrorPalToPlatform(int32_t error)
{
    return ConvertErrorPalToPlatform(error);
}

const char* SystemNative_StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize)
{
    return StrErrorR(platformErrno, buffer, bufferSize);
}

int32_t SystemNative_GetErrNo(void)
{
    return errno;
}

void SystemNative_SetErrNo(int32_t errorCode)
{
    errno = errorCode;
}
