// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Crash report process-name lookup helper.
// Uses only open/read/close and manual parsing so it remains usable from
// crash-time code without stdio or heap allocation.

#pragma once

#include <stdint.h>

// Returns the basename of the current process image. Prefers /proc/self/cmdline
// and falls back to the first executable mapping in /proc/self/maps.
int CrashModulesTryGetProcessName(char* filename, int filenameLen);
