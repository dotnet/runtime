// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    VASigCookie = 5,
}

/// <summary>
/// Managed port of the native GCRefMapDecoder (gcrefmap.h).
///
/// A GCRefMap is a compact bitstream that describes which transition block slots
/// contain GC references for a given call site (e.g., in ReadyToRun stubs).
/// It is used by ExternalMethodFrame and StubDispatchFrame to report GC roots
/// without needing the full MethodDesc/signature decoding path.
///
/// Encoding: each slot is encoded as a variable-length integer using 3 bits per
/// token (see <see cref="GCRefMapToken"/>), with a high-bit continuation flag.
/// A "skip" token advances the slot position without reporting. The stream ends
/// when all slots have been consumed (indicated by a zero byte after the last token).
///
/// The native implementation lives in coreclr/inc/gcrefmap.h (GCRefMapDecoder class).
/// </summary>
internal ref struct GCRefMapDecoder
{
    private readonly Target _target;
    private TargetPointer _currentByte;
    private int _pendingByte;
    private int _pos;

    public GCRefMapDecoder(Target target, TargetPointer blob)
    {
        _target = target;
        _currentByte = blob;
        _pendingByte = 0x80; // Forces first byte read
        _pos = 0;
    }

    public readonly bool AtEnd => _pendingByte == 0;

    public readonly int CurrentPos => _pos;

    private int GetBit()
    {
        int x = _pendingByte;
        if ((x & 0x80) != 0)
        {
            x = _target.Read<byte>(_currentByte);
            _currentByte = new TargetPointer(_currentByte.Value + 1);
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
                return (GCRefMapToken)((ext >> 1) + 3);
            }
        }
        _pos++;
        return (GCRefMapToken)val;
    }
}
