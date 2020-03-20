// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

PALEXPORT void SystemNative_Log(uint8_t* buffer, int32_t length);

// Called by pal_signal.cpp to reinitialize the console on SIGCONT/SIGCHLD.
void ReinitializeTerminal(void) {}
void UninitializeTerminal(void) {}