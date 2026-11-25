// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef PERFJITDUMP_H
#define PERFJITDUMP_H

// Start the jitdump file
int PerfJitDump_Start(const char* path);

bool PerfJitDump_IsStarted();

// Log a method to the jitdump file.
int PerfJitDump_LogMethod(void* pCode, size_t codeSize, const char* symbol, void* debugInfo, void* unwindInfo, bool reportCodeBlock);

// Finish the jitdump file
int PerfJitDump_Finish();

#endif // PERFJITDUMP_H
