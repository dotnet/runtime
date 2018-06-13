// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_slist.h"

struct curl_slist* HttpNative_SListAppend(struct curl_slist* list, const char* headerValue)
{
    return curl_slist_append(list, headerValue);
}

void HttpNative_SListFreeAll(struct curl_slist* list)
{
    curl_slist_free_all(list);
}
