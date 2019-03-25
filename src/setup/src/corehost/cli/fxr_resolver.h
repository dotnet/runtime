// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef _COREHOST_CLI_FXR_RESOLVER_H_
#define _COREHOST_CLI_FXR_RESOLVER_H_

#include <pal.h>

namespace fxr_resolver
{
    bool try_get_path(const pal::string_t& root_path, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path);
}

#endif //_COREHOST_CLI_FXR_RESOLVER_H_
