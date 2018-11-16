// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Marshal_InOut(int expected[], int actual[], int numElements, int newValue[])
{
    bool correctPassedIn = memcmp(expected, actual, numElements * sizeof(int)) == 0;

    memcpy(actual, newValue, numElements * sizeof(int));

    return correctPassedIn ? TRUE : FALSE;
}

extern "C" DLL_EXPORT BOOL STDMETHODCALLTYPE Marshal_Invalid(void* invalid)
{
    return FALSE;
}
