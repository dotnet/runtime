// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Debug-only disassembler for the interpreter bytecode produced by codegeninterp.cpp.

#ifndef _INTERPDUMP_H_
#define _INTERPDUMP_H_

#ifdef DEBUG

// Disassemble a generated interpreter bytecode buffer. The output format matches the
// interpreter library's own dump (DumpCompiledCode in interpreter/compiler.cpp).
void dumpInterpCode(const int32_t* code, int32_t codeSizeSlots);

#endif // DEBUG

#endif // _INTERPDUMP_H_
