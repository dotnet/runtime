// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_AMD64) && defined(TARGET_WINDOWS)
#include <intrin.h>

extern "C" __declspec(dllexport) __int64 ReadShadowStackPointer()
{
    return _rdsspq();
}
#endif
