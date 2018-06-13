// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"
#include "pal_compiler.h"

#include <curl/curl.h>

/*
Appends a specified string to a linked list of strings.

Returns a null pointer if anything went wrong, otherwise the new list pointer.
*/
DLLEXPORT struct curl_slist* HttpNative_SListAppend(struct curl_slist* list, const char* headerValue);

/*
Removes all traces of a previously built curl_slist linked list.
*/
DLLEXPORT void HttpNative_SListFreeAll(struct curl_slist* list);
