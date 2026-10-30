// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Token values from CORCOMPILE_GCREFMAP_TOKENS (corcompile.h).
/// These indicate the type of GC reference at each transition block slot.
/// </summary>
internal enum GCRefMapToken
{
    Skip = 0,
    Ref = 1,
    Interior = 2,
    MethodParam = 3,
    TypeParam = 4,
    ThisRef = 5,
    ThisByRef = 6,
    VASigCookie = 7,
}

/// <summary>
/// Managed port of the native GCRefMapDecoder (gcrefmap.h).
///
/// A GCRefMap is a compact bitstream that describes which transition block slots
/// contain GC references for a given call site (e.g., in ReadyToRun stubs).
/// It is used by ExternalMethodFrame and StubDispatchFrame to report GC roots
/// without needing the full MethodDesc/signature decoding path.
///
/// Encoding: two-bit tokens use escape 3 for extended tokens and longer skips.
/// Extended integers use three payload bits and a continuation bit per group.
/// THIS_REF and THIS_BYREF encode the original callsite receiver kind. Each byte
/// carries seven stream bits and a high continuation bit; trailing zero bits
/// need not be stored.
///
/// The native implementation lives in coreclr/inc/gcrefmap.h (GCRefMapDecoder class).
/// </summary>
internal ref struct GCRefMapDecoder
{
    internal const byte LegacyVASigCookie = 5;

    // The decoder can source its bytes either from target memory (the R2R image,
    // for ExternalMethodFrame / StubDispatchFrame) or from a host-side byte[]
    // (for blobs we synthesize via ICallingConvention.TryComputeArgGCRefMapBlob).
    // Exactly one of (_target + _currentByte) or (_blob) is non-null per instance;
    // see UsesBlob below for the invariant that lets GetBit pick the right path
    // without runtime null checks.
    private readonly Target? _target;
    private readonly bool _legacyTokens;
    private TargetPointer _currentByte;
    private readonly byte[]? _blob;
    private int _blobIndex;
    private int _pendingByte;
    private int _pos;

    // StackWalk version 2 targets require R2R 33 and its new token assignments.
    internal static bool UsesLegacyTokens(Target target) =>
        target.Contracts.TryGetContractVersion<IStackWalk>(out string? version) && version == "c1";

    public GCRefMapDecoder(Target target, TargetPointer blob)
    {
        _target = target;
        _legacyTokens = UsesLegacyTokens(target);
        _currentByte = blob;
        _blob = null;
        _blobIndex = 0;
        _pendingByte = 0x80; // Forces first byte read
        _pos = 0;
    }

    public GCRefMapDecoder(byte[] blob, bool legacyTokens = false)
    {
        _target = null;
        _legacyTokens = legacyTokens;
        _currentByte = TargetPointer.Null;
        _blob = blob;
        _blobIndex = 0;
        _pendingByte = 0x80; // Forces first byte read
        _pos = 0;
    }

    public readonly bool AtEnd => _pendingByte == 0;

    public readonly int CurrentPos => _pos;

    [MemberNotNullWhen(true, nameof(_blob))]
    [MemberNotNullWhen(false, nameof(_target))]
    private readonly bool UsesBlob => _blob is not null;

    private int GetBit()
    {
        int x = _pendingByte;
        if ((x & 0x80) != 0)
        {
            if (UsesBlob)
            {
                x = _blobIndex < _blob.Length ? _blob[_blobIndex] : 0;
                _blobIndex++;
            }
            else
            {
                x = _target.Read<byte>(_currentByte);
                _currentByte = new TargetPointer(_currentByte.Value + 1);
            }
            x |= (x & 0x80) << 7;
        }
        _pendingByte = x >> 1;
        return x & 1;
    }

    private int GetTwoBit()
    {
        int result = GetBit();
        result |= GetBit() << 1;
        return result;
    }

    private int GetInt()
    {
        int result = 0;
        int bit = 0;
        do
        {
            result |= GetBit() << (bit++);
            result |= GetBit() << (bit++);
            result |= GetBit() << (bit++);
        }
        while (GetBit() != 0);
        return result;
    }

    /// <summary>
    /// x86 only: Read the stack pop count from the stream.
    /// </summary>
    public uint ReadStackPop()
    {
        int x = GetTwoBit();
        if (x == 3)
            x = GetInt() + 3;
        return (uint)x;
    }

    /// <summary>
    /// Read the next GC reference token from the stream.
    /// Advances CurrentPos as appropriate.
    /// </summary>
    public GCRefMapToken ReadToken()
    {
        int val = GetTwoBit();
        if (val == 3)
        {
            int ext = GetInt();
            if ((ext & 1) == 0)
            {
                _pos += (ext >> 1) + 4;
                return GCRefMapToken.Skip;
            }
            else
            {
                _pos++;
                GCRefMapToken token = (GCRefMapToken)((ext >> 1) + 3);
                if (_legacyTokens && token is GCRefMapToken.ThisRef)
                    return GCRefMapToken.VASigCookie;
                return token;
            }
        }
        _pos++;
        return (GCRefMapToken)val;
    }
}
