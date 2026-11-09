// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

// Re-executing the application with this argument enters the dump writer before
// managed startup, without using the crashing process's runtime state.
#define CREATEDUMP_SENTINEL "--{E89B44A7-9C62-4AB5-B292-FC7E1A25E949}"

extern bool g_createdumpLinked;
extern bool g_createdumpDispatchSupported;

// Returns true when the invocation was handled, with its success or failure result in exitCode.
bool nativeaot_createdump(int argc, char* argv[], int* exitCode);
