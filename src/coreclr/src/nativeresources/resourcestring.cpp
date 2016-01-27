// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include <stdio.h>
#include <utilcode.h>
#include "resourcestring.h"

static int CompareNativeStringResources(const void *a, const void *b)
{
    unsigned int resourceIdA = ((NativeStringResource*)a)->resourceId;
    unsigned int resourceIdB = ((NativeStringResource*)b)->resourceId;

    if (resourceIdA < resourceIdB)
        return -1;

    if (resourceIdA == resourceIdB)
        return 0;

    return 1;
}

int LoadNativeStringResource(const NativeStringResourceTable &nativeStringResourceTable, unsigned int iResourceID, WCHAR* szBuffer, int iMax, int *pcwchUsed)
{
    int len = 0;
    if (szBuffer && iMax)
    {
        // Search the sorted set of resources for the ID we're interested in.
        NativeStringResource searchEntry = {iResourceID, NULL};
        NativeStringResource *resourceEntry = (NativeStringResource*)bsearch(
            &searchEntry,
            nativeStringResourceTable.table,
            nativeStringResourceTable.size,
            sizeof(NativeStringResource),
            CompareNativeStringResources);

        if (resourceEntry != NULL)
        {
            len = PAL_GetResourceString(NULL, resourceEntry->resourceString, szBuffer, iMax);
        }
        else
        {
            // The resource ID wasn't found in our array. Fall back on returning the ID as a string.
            len = _snwprintf(szBuffer, iMax - 1, W("[Undefined resource string ID:0x%X]"), iResourceID);
            if ((len < 0) || (len == (iMax - 1)))
            {
                // Add string terminator if the result of _snwprintf didn't fit the buffer.
                szBuffer[iMax - 1] = W('\0');
                len = iMax - 1;
            }
        }
    }

    if (pcwchUsed)
    {
        *pcwchUsed = len;
    }

    return S_OK;
}

