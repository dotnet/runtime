// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
extern "C" int32_t EnsureCurlIsInitialized();
