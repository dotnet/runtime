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

constexpr GUID JITEEVersionIdentifier = { /* 2fa691c0-f8d3-46e1-a09d-31811952b85f */
    0x2fa691c0,
    0xf8d3,
    0x46e1,
    {0xa0, 0x9d, 0x31, 0x81, 0x19, 0x52, 0xb8, 0x5f}
  };

#endif // JIT_EE_VERSIONING_GUID_H
