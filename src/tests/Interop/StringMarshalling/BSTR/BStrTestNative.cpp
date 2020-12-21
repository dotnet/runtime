// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "platformdefines.h"
#include "../Native/StringMarshalingNative.h"

using StringType = BSTR;
using Tests = BStrMarshalingTests<TP_SysStringLen, WCHAR, CoreClrBStrAlloc>;

#define FUNCTION_NAME TP_SysAllocString(__FUNCTIONW__)

#include "../Native/StringTestEntrypoints.inl"
