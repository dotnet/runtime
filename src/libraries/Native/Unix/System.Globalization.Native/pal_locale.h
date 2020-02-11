// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"

EXTERN_C DLLEXPORT int32_t GlobalizationNative_GetLocales(uint16_t *value, int32_t valueLength);

EXTERN_C DLLEXPORT int32_t GlobalizationNative_GetLocaleName(const uint16_t* localeName, uint16_t* value, int32_t valueLength);

EXTERN_C DLLEXPORT int32_t GlobalizationNative_GetDefaultLocaleName(uint16_t* value, int32_t valueLength);

EXTERN_C DLLEXPORT int32_t GlobalizationNative_IsPredefinedLocale(const uint16_t* localeName);
