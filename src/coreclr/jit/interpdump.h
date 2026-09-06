// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file declares the debug-only disassembler for interpreter bytecode
// produced by the JIT interpreter backend. See interpdump.cpp for the
// implementation.

#ifndef _INTERPDUMP_H_
#define _INTERPDUMP_H_

#if defined(FEATURE_INTERPRETER) && defined(DEBUG)

// Disassemble a generated interpreter bytecode buffer to stdout. The output
// format matches the interpreter library's own dump (DumpCompiledCode in
// interpreter/compiler.cpp).
void dumpInterpCode(const int32_t* code, int32_t codeSizeSlots);

#endif // FEATURE_INTERPRETER && DEBUG

#endif // _INTERPDUMP_H_
