// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef __ERROR_CODES_H__
#define __ERROR_CODES_H__
enum StatusCode
{
    Success = 0,
    InvalidArgFailure = 0x81,
    CoreHostLibLoadFailure = 0x82,
    CoreHostLibMissingFailure = 0x83,
    CoreHostEntryPointFailure = 0x84,
    CoreHostCurExeFindFailure = 0x85,
    CoreHostResolveModeFailure = 0x86,
    CoreClrResolveFailure = 0x87,
    CoreClrBindFailure = 0x88,
    CoreClrInitFailure = 0x89,
    CoreClrExeFailure = 0x90,
    ResolverInitFailure = 0x91,
    ResolverResolveFailure = 0x92,
    LibHostCurExeFindFailure = 0x93,
    LibHostInitFailure = 0x94,
    LibHostMuxFailure = 0x95,
    LibHostExecModeFailure = 0x96,
    LibHostSdkFindFailure = 0x97,
    LibHostInvalidArgs = 0x98,
    InvalidConfigFile = 0x99,
};
#endif // __ERROR_CODES_H__
