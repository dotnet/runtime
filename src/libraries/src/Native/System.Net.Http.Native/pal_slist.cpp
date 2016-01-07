// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_slist.h"

extern "C" curl_slist* HttpNative_SListAppend(curl_slist* list, const char* headerValue)
{
    return curl_slist_append(list, headerValue);
}

extern "C" void HttpNative_SListFreeAll(curl_slist* list)
{
    curl_slist_free_all(list);
}
