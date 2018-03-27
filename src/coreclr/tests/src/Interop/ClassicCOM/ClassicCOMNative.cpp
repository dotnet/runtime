// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <xplatform.h>
#include <stdio.h>
#include <stdlib.h>

extern "C" DLL_EXPORT void PassObjectToNative(void * ptr)
{
    // TODO: Add check
}

extern "C" DLL_EXPORT void PassObjectArrayToNative(void ** pptr)
{
    // TODO: Add check
}

extern "C" DLL_EXPORT void GetObjectFromNative(void ** pptr)
{
    *pptr = NULL;
    // TODO: Add check
}

extern "C" DLL_EXPORT void GetObjectFromNativeAsRef(void ** pptr)
{
    // TODO: Add check
}