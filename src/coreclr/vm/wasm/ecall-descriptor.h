// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef ECALL_DESCRIPTOR_H
#define ECALL_DESCRIPTOR_H

// Use the same entry identity when compiling descriptors and exporting the manifest.
#define ECALL_SYMBOL_IMPL(line) ecall_entry_ ## line
#define ECALL_SYMBOL_EXPAND(line) ECALL_SYMBOL_IMPL(line)
#define ECALL_SYMBOL ECALL_SYMBOL_EXPAND(__LINE__)

#endif // ECALL_DESCRIPTOR_H
