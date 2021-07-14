// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef WIN32
#include <windows.h>
#else
#ifndef WINAPI
#define WINAPI 
#endif
#endif

bool WINAPI isAsanShadowAddress(void* address);
