// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
//
// #JITEEVersionIdentifier
//
// This GUID represents the version of the JIT/EE interface. Any time the interface between the JIT and
// the EE changes (by adding or removing methods to any interface shared between them), this GUID should
// be changed. This is the identifier verified by ICorJitCompiler::getVersionIdentifier().
//
// You can use src/coreclr/tools/Common/JitInterface/ThunkGenerator/gen.bat (or .sh on Unix) to update this file.
//
// Note that this file is parsed by some tools, namely superpmi.py, so make sure the first line is exactly
// of the form:
//
//   constexpr GUID JITEEVersionIdentifier = { /* 1776ab48-edfa-49be-a11f-ec216b28174c */
//
// (without the leading slashes or spaces).
//
// See docs/project/updating-jitinterface.md for details
//
// **** NOTE TO INTEGRATORS:
//
// If there is a merge conflict here, because the version changed in two different places, you must
// create a **NEW** GUID, not simply choose one or the other!
//
// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////
//

#ifndef JIT_EE_VERSIONING_GUID_H
#define JIT_EE_VERSIONING_GUID_H

#include <minipal/guid.h>

<<<<<<< wasm_eh_infra
constexpr GUID JITEEVersionIdentifier = { /* 61e50dd4-fda2-4184-a348-f64e501d6c60 */
    0x61e50dd4,
    0xfda2,
    0x4184,
    {0xa3, 0x48, 0xf6, 0x4e, 0x50, 0x1d, 0x6c, 0x60}
=======
constexpr GUID JITEEVersionIdentifier = { /* 59df85b8-c0fd-4e40-aea1-68cb2cd916cc */
    0x59df85b8,
    0xc0fd,
    0x4e40,
    {0xae, 0xa1, 0x68, 0xcb, 0x2c, 0xd9, 0x16, 0xcc}
>>>>>>> main
  };

#endif // JIT_EE_VERSIONING_GUID_H
