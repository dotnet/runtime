// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef _COREHOST_CLI_FXR_RESOLVER_H_
#define _COREHOST_CLI_FXR_RESOLVER_H_

#include <pal.h>

bool resolve_fxr_path(const pal::string_t& host_path, pal::string_t* out_dotnet_root, pal::string_t* out_fxr_path);

#endif //_COREHOST_CLI_FXR_RESOLVER_H_
