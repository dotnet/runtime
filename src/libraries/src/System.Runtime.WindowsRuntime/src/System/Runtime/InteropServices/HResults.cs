// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// HRESULT values used in this assembly.
    /// </summary>
    internal static class __HResults
    {
        internal const int S_OK = unchecked((int)0x00000000);
        internal const int E_BOUNDS = unchecked((int)0x8000000B);
        internal const int E_ILLEGAL_STATE_CHANGE = unchecked((int)0x8000000D);
        internal const int E_ILLEGAL_METHOD_CALL = unchecked((int)0x8000000E);
        internal const int RO_E_CLOSED = unchecked((int)0x80000013);
        internal const int E_ILLEGAL_DELEGATE_ASSIGNMENT = unchecked((int)0x80000018);
        internal const int E_NOTIMPL = unchecked((int)0x80004001);
        internal const int E_FAIL = unchecked((int)0x80004005);
        internal const int E_INVALIDARG = unchecked((int)0x80070057);
    }  // internal static class HResults
}  // namespace

// HResults.cs
