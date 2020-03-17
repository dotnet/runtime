// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "compiler.h"

EXTERN_C PALEXPORT int32_t GlobalizationNative_GetLocales(UChar *value, int32_t valueLength);

EXTERN_C PALEXPORT int32_t GlobalizationNative_GetLocaleName(const UChar* localeName, UChar* value, int32_t valueLength);

EXTERN_C PALEXPORT int32_t GlobalizationNative_GetDefaultLocaleName(UChar* value, int32_t valueLength);

EXTERN_C PALEXPORT int32_t GlobalizationNative_IsPredefinedLocale(const UChar* localeName);
