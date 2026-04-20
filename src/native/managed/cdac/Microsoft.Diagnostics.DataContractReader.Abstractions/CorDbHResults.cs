// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader;

public static class CorDbgHResults
{
    public const int CORDBG_E_NOTREADY = unchecked((int)0x80131c10);
    public const int CORDBG_E_BAD_THREAD_STATE = unchecked((int)0x8013132d);
    public const int CORDBG_E_READVIRTUAL_FAILURE = unchecked((int)0x80131c49);
    public const int ERROR_BUFFER_OVERFLOW = unchecked((int)0x8007006F); // HRESULT_FROM_WIN32(ERROR_BUFFER_OVERFLOW)
    public const int CORDBG_E_CLASS_NOT_LOADED = unchecked((int)0x80131303);
}
