// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader;

/// <summary>
/// HRESULT used by the cDAC reader's data-target delegates. Provides
/// named cDAC-relevant values; unknown HRESULTs returned from native code flow
/// through via explicit <c>(CdacHResults)</c> casts.
/// Expected values for ReadFromTargetDelegate: S_OK, E_FAIL
/// Expected values for WriteToTargetDelegate: S_OK, E_FAIL
/// Expected values for GetTargetThreadContextDelegate: E_NOTIMPL
/// Expected values for AllocVirtualDelegate: E_INVALIDARG
/// </summary>
public enum CdacHResults : int
{
    S_OK = 0,
    E_NOTIMPL = unchecked((int)0x80004001),
    E_FAIL = unchecked((int)0x80004005),
    E_INVALIDARG = unchecked((int)0x80070057),
}

public static class CdacHResultExtensions
{
    public static bool IsSuccess(this CdacHResults hr) => (int)hr >= 0;
    public static bool IsFailure(this CdacHResults hr) => (int)hr < 0;
}
