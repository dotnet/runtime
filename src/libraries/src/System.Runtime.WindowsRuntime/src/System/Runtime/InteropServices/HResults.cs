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
        internal const Int32 S_OK = unchecked((Int32)0x00000000);
        internal const Int32 E_BOUNDS = unchecked((Int32)0x8000000B);
        internal const Int32 E_ILLEGAL_STATE_CHANGE = unchecked((Int32)0x8000000D);
        internal const Int32 E_ILLEGAL_METHOD_CALL = unchecked((Int32)0x8000000E);
        internal const Int32 RO_E_CLOSED = unchecked((Int32)0x80000013);
        internal const Int32 E_ILLEGAL_DELEGATE_ASSIGNMENT = unchecked((Int32)0x80000018);
        internal const Int32 E_NOTIMPL = unchecked((Int32)0x80004001);
        internal const Int32 E_FAIL = unchecked((Int32)0x80004005);
        internal const Int32 E_INVALIDARG = unchecked((Int32)0x80070057);
    }  // internal static class HResults
}  // namespace

// HResults.cs
