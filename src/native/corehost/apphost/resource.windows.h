// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __RESOURCE_H__
#define __RESOURCE_H__

// We don't want to set an automatically recognized manifest for apphost.
// Use a non-reserved manifest resource ID. 1-16 are reserved (winuser.h)
#define IDR_ACTCTX_MANIFEST 101

#endif // __RESOURCE_H__
