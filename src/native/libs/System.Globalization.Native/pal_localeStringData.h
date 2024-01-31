// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#pragma once

#include "pal_locale.h"
#include "pal_compiler.h"

PALEXPORT int32_t GlobalizationNative_GetLocaleInfoString(const UChar* localeName,
                                                          LocaleStringData localeStringData,
                                                          UChar* value,
                                                          int32_t valueLength,
                                                          const UChar* uiLocaleName);
