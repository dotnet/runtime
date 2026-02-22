// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Minimal ELF core dump writer for NativeAOT.
// Writes full process dumps in ELF core format.
// Pure C — no C++ runtime dependency.

#ifndef ELF_DUMP_WRITER_H
#define ELF_DUMP_WRITER_H

#include <stdbool.h>
#include "process_reader.h"

// Write an ELF core dump of the given process to the specified file path.
// All readable memory regions are included in the dump.
// Returns true on success, false on failure.
bool WriteElfCoreDump(const char* dumpPath, ProcessInfo* info, bool diagnostics);

#endif // ELF_DUMP_WRITER_H
