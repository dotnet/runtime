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
// You can use "uuidgen.exe -s" to generate this value.
//
// Note that this file is parsed by some tools, namely superpmi.py, so make sure the first line is exactly
// of the form:
//
//   constexpr GUID JITEEVersionIdentifier = { /* a7bb194e-4e7c-4850-af12-ea9f30ea5a13 */
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

constexpr GUID JITEEVersionIdentifier = { /* 71a99045-a4fe-43b3-abd9-c1742ab020b3 */
    0x71a99045,
    0xa4fe,
    0x43b3,
    {0xab, 0xd9, 0xc1, 0x74, 0x2a, 0xb0, 0x20, 0xb3}
};

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// END JITEEVersionIdentifier
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////
