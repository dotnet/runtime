// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Unity.CoreCLRHelpers;

// Needs to stay in sync with the MonoAssemblyName struct in Runtime/Mono/MonoTypes.h
public unsafe struct MonoAssemblyName
{
    public sbyte* name;
    public sbyte* culture;
    public sbyte* hash_value;
    public byte* public_key;
    // string of 16 hex chars + 1 NULL
    public fixed byte public_key_token[17];
    public uint hash_alg;
    public uint hash_len;
    public uint flags;
    public UInt16 major, minor, build, revision;
    // only used and populated by newer Mono
    public UInt16 arch;
    public byte without_version;
    public byte without_culture;
    public byte without_public_key_token;
}
