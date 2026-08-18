// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Translates between the COM-side <see cref="CLRDataMethodCodeNotification"/> bit field
/// (a raw uint exposed through xclrdata.idl) and the contract-side
/// <see cref="CodeNotificationKind"/> enum. The two enums happen to share the same bit
/// values today, but each side is free to evolve independently — translation must be
/// explicit per bit, not a numeric cast.
/// </summary>
internal static class CodeNotificationFlagsConverter
{
    private const uint AllValidComFlags =
        (uint)(CLRDataMethodCodeNotification.CLRDATA_METHNOTIFY_GENERATED
             | CLRDataMethodCodeNotification.CLRDATA_METHNOTIFY_DISCARDED);

    /// <summary>
    /// Returns true if every set bit in <paramref name="flags"/> is a recognized
    /// <see cref="CLRDataMethodCodeNotification"/> value.
    /// </summary>
    public static bool IsValid(uint flags) => (flags & ~AllValidComFlags) == 0;

    /// <summary>
    /// Convert a raw COM <see cref="CLRDataMethodCodeNotification"/> bitmask to the
    /// contract enum, mapping each defined bit explicitly.
    /// </summary>
    public static CodeNotificationKind FromCom(uint flags)
    {
        CodeNotificationKind result = CodeNotificationKind.None;
        if ((flags & (uint)CLRDataMethodCodeNotification.CLRDATA_METHNOTIFY_GENERATED) != 0)
            result |= CodeNotificationKind.Generated;
        if ((flags & (uint)CLRDataMethodCodeNotification.CLRDATA_METHNOTIFY_DISCARDED) != 0)
            result |= CodeNotificationKind.Discarded;
        return result;
    }

    /// <summary>
    /// Convert a contract <see cref="CodeNotificationKind"/> value back to the raw COM
    /// <see cref="CLRDataMethodCodeNotification"/> bitmask, mapping each defined bit
    /// explicitly.
    /// </summary>
    public static uint ToCom(CodeNotificationKind kind)
    {
        uint result = 0;
        if ((kind & CodeNotificationKind.Generated) != 0)
            result |= (uint)CLRDataMethodCodeNotification.CLRDATA_METHNOTIFY_GENERATED;
        if ((kind & CodeNotificationKind.Discarded) != 0)
            result |= (uint)CLRDataMethodCodeNotification.CLRDATA_METHNOTIFY_DISCARDED;
        return result;
    }
}
