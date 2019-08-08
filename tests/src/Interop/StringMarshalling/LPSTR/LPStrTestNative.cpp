// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "../Native/StringMarshalingNative.h"

using StringType = LPSTR;
using Tests = StringMarshalingTests<StringType, default_callconv_strlen>;

#define FUNCTION_NAME __func__

#include "../Native/StringTestEntrypoints.inl"
