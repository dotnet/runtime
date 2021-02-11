// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <xplatform.h>


extern "C" int DLL_EXPORT STDMETHODCALLTYPE NativeParseInt(LPCSTR str)
{
    return atoi(str);
}
