// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <stdio.h>
#include <stdlib.h>
#include <locale.h>
#include <xplatform.h>

#pragma warning( push )
#pragma warning( disable : 4996)

extern "C" DLL_EXPORT int __cdecl Sum(int a, int b)
{
    return a + b;
}