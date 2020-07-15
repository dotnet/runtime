// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "xplatform.h"

extern "C" DLL_EXPORT int STDMETHODCALLTYPE GetZero()
{
    return 0;
}

#ifdef EXE

extern "C" int __cdecl main(int argc,  char **argv)
{
    return 0;
}

#endif
