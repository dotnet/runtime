// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include "pal_compiler.h"
#include "pal_locale.h"
#include "pal_errors.h"

/*
These values should be kept in sync with the managed Interop.GlobalizationInterop.TimeZoneDisplayNameType enum.
*/
typedef enum
{
    TimeZoneDisplayName_Generic = 0,
    TimeZoneDisplayName_Standard = 1,
    TimeZoneDisplayName_DaylightSavings = 2,
} TimeZoneDisplayNameType;

DLLEXPORT ResultCode GlobalizationNative_GetTimeZoneDisplayName(const UChar* localeName,
                                                                const UChar* timeZoneId,
                                                                TimeZoneDisplayNameType type,
                                                                UChar* result,
                                                                int32_t resultLength);
