// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"

#include <curl/curl.h>

/*
Appends a specified string to a linked list of strings.

Returns a null pointer if anything went wrong, otherwise the new list pointer.
*/
extern "C" curl_slist* HttpNative_SListAppend(curl_slist* list, const char* headerValue);

/*
Removes all traces of a previously built curl_slist linked list.
*/
extern "C" void HttpNative_SListFreeAll(curl_slist* list);
