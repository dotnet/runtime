// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "platformdefines.h"
#include "../Native/StringMarshalingNative.h"

using StringType = BSTR;
using Tests = BStrMarshalingTests<TP_SysStringByteLen, char, CoreClrBStrAlloc>;

#define FUNCTION_NAME CoreClrBStrAlloc(__func__, STRING_LENGTH(__func__))

#include "../Native/StringTestEntrypoints.inl"
