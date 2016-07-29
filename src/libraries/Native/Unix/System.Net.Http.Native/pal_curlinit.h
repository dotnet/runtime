// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_types.h"

/**
 * Initializes curl.
 *
 * Thread-safe and idempotent. Must be called before using any other curl function.
 * EnsureOpenSSLIsInitialized from System.Security.Cryptography.Native must already
 * have been called before calling this.
 *
 * Returns 0 on success and non-zero on failure.
 */
extern "C" int32_t HttpNative_EnsureCurlIsInitialized();
