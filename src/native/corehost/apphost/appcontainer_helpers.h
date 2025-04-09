// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __APPCONTAINER_HELPERS_H__
#define __APPCONTAINER_HELPERS_H__

#include "pal.h"

namespace appcontainer_helpers
{
    bool is_appcontainer();
    bool is_uwp();
    void open_url_for_appcontainer(const pal::char_t* url);
}

#endif // __APPCONTAINER_HELPERS_H__
