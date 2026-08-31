// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

extern "C"
{
DLL_EXPORT int GetInt32Const() {return 7;}
DLL_EXPORT int64_t GetInt64Const() {return 7;}
DLL_EXPORT float GetFloatConst() {return 7.777777f;}
DLL_EXPORT double GetDoubleConst() {return 7.777777;}
}
