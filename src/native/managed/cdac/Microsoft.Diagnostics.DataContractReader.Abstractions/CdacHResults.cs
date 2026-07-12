// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// cDAC-specific <c>HRESULT</c>s surfaced when creating a data-access interface
/// for a target fails for a reason this cDAC can describe precisely. This covers two stages of
/// creation: locating and reading the target's contract descriptor (the <c>DESCRIPTOR</c> codes),
/// and eager validation that this cDAC can service the contracts the descriptor advertises (the
/// <c>CONTRACT</c> codes, see
/// <see cref="Microsoft.Diagnostics.DataContractReader.Contracts.CoreCLRContracts.ValidateForDataAccess"/>).
/// </summary>
/// <remarks>
/// The customer bit (bit 29, <c>0x20000000</c>) is set on every value so these codes can never
/// collide with system or CLR (corerror.h) <c>HRESULT</c>s. All values are failure codes, so
/// existing <c>FAILED(hr)</c> checks in native callers continue to behave correctly. Native code
/// and tooling that wants to distinguish the failure modes (for example, to decide whether to fall
/// back to the brittle DAC) can compare against the literal values documented here.
/// </remarks>
public static class CdacHResults
{
    /// <summary>
    /// A contract required to service the SOS interface is not advertised by the target's
    /// contract descriptor. The target runtime predates the contract or does not expose it.
    /// </summary>
    public const int CDAC_E_CONTRACT_NOT_ADVERTISED = unchecked((int)0xA0DAC001);

    /// <summary>
    /// The target advertises a required contract at a version this cDAC does not recognize,
    /// typically because the target runtime is newer than this cDAC.
    /// </summary>
    public const int CDAC_E_CONTRACT_UNRECOGNIZED = unchecked((int)0xA0DAC002);

    /// <summary>
    /// The target advertises a required contract at a version this cDAC recognizes but
    /// intentionally does not implement, typically because the target runtime is too old.
    /// </summary>
    public const int CDAC_E_CONTRACT_UNSUPPORTED = unchecked((int)0xA0DAC003);

    /// <summary>
    /// No usable cDAC contract descriptor could be located for the target: the contract locator
    /// did not produce a descriptor address, the descriptor header could not be read, or the bytes
    /// at the descriptor address are not a contract descriptor (bad magic). This typically means the
    /// target is not a cDAC-capable .NET runtime (for example, a non-.NET target or a runtime that
    /// predates the cDAC contract descriptor), and tooling should fall back to the brittle DAC
    /// rather than treat it as a hard error.
    /// </summary>
    public const int CDAC_E_DESCRIPTOR_NOT_FOUND = unchecked((int)0xA0DAC011);

    /// <summary>
    /// A cDAC contract descriptor was found but could not be consumed: a field could not be read,
    /// the embedded JSON failed to parse, or the descriptor content is internally inconsistent
    /// (for example, duplicate names or an out-of-range pointer-data index). This indicates a
    /// corrupt dump or a descriptor format this cDAC cannot understand, and is distinct from a
    /// recognized-but-unsupported contract version (<see cref="CDAC_E_CONTRACT_UNSUPPORTED"/>).
    /// </summary>
    public const int CDAC_E_DESCRIPTOR_MALFORMED = unchecked((int)0xA0DAC012);
}
