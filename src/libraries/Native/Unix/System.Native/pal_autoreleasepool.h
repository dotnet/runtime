// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

/**
 * Creates an pool to automatically release applicable ref-counted resources.
 */
PALEXPORT void* SystemNative_CreateAutoreleasePool(void);

/**
 * Drains and releases a pool created by SystemNative_CreateAutoreleasePool.
 */
PALEXPORT void SystemNative_DrainAutoreleasePool(void* pool);
