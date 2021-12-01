// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


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
            len = MultiByteToWideChar(CP_UTF8, 0, resourceEntry->resourceString, -1, szBuffer, iMax);
            if (len == 0)
            {
                int hr = HRESULT_FROM_GetLastError();

                // Tell the caller if the buffer isn't big enough
                if (hr == HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER) && pcwchUsed)
                    *pcwchUsed = iMax;

                return hr;
            }
        }
        else
        {
            // The resource ID wasn't found in our array. Fall back on returning the ID as a string.
            len = _snwprintf_s(szBuffer, iMax, _TRUNCATE, W("[Undefined resource string ID:0x%X]"), iResourceID);
            if (len < 0)
            {
                // The only possible failure is that that string didn't fit the buffer. So the buffer contains
                // partial string terminated by '\0'
                // We could return ERROR_INSUFFICIENT_BUFFER, but we'll error on the side of caution here and
                // actually show something (given that this is likely a scenario involving a bug/deployment issue)
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

