// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <stdlib.h>
#include "pal_icushim_internal.h"
#include "pal_icushim.h"
#include <unicode/putil.h>
#include <unicode/uversion.h>

int32_t GlobalizationNative_LoadICU(void)
{
    const char* icudir = getenv("DOTNET_ICU_DIR");
    if (!icudir)
        return 0;

    // path to a directory with icudt___.dat (e.g. icudt67l.dat)
    // we can also use `udata_setCommonData(const void *data, UErrorCode *err)` API here
    u_setDataDirectory(icudir);
    return 1;
}

void GlobalizationNative_InitICUFunctions(void* icuuc, void* icuin, const char* version, const char* suffix)
{
    // no-op for static
}

int32_t GlobalizationNative_GetICUVersion(void)
{
    UVersionInfo versionInfo;
    u_getVersion(versionInfo);

    return (versionInfo[0] << 24) + (versionInfo[1] << 16) + (versionInfo[2] << 8) + versionInfo[3];
}
