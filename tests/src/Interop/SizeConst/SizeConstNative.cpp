// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <xplatform.h>

const int ARRAY_SIZE = 100;
typedef struct { char  arr[ARRAY_SIZE]; } S_CHARByValArray;
extern "C" DLL_EXPORT BOOL __cdecl TakeByValTStr(S_CHARByValArray s, int size)
{
    return true;
}
