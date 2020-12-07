
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

constexpr GUID JITEEVersionIdentifier = { /* f4746286-33ad-45a2-a37e-d226d689524d */
    0xf4746286,
    0x33ad,
    0x45a2,
    {0xa3, 0x7e, 0xd2, 0x26, 0xd6, 0x89, 0x52, 0x4d}
};

//////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// END JITEEVersionIdentifier
//
//////////////////////////////////////////////////////////////////////////////////////////////////////////
