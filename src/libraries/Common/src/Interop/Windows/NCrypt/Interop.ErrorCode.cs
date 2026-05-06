// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        /// <summary>
        ///     Result codes from NCrypt APIs
        /// </summary>
        internal enum ErrorCode : int
        {
            ERROR_SUCCESS = 0,
            E_FAIL = unchecked((int)0x80004005),
            NTE_BAD_SIGNATURE = unchecked((int)0x80090006),
            NTE_NOT_FOUND = unchecked((int)0x80090011),
            NTE_BAD_KEYSET = unchecked((int)0x80090016),
            NTE_INVALID_PARAMETER = unchecked((int)0x80090027),
            NTE_BUFFER_TOO_SMALL = unchecked((int)0x80090028),
            NTE_NOT_SUPPORTED = unchecked((int)0x80090029),
            NTE_NO_MORE_ITEMS = unchecked((int)0x8009002a),
            TPM_E_PCP_BUFFER_TOO_SMALL = unchecked((int)0x80290406),
            STATUS_UNSUCCESSFUL = unchecked((int)0xC0000001),
        }
    }

    internal static bool IsBufferTooSmall(this NCrypt.ErrorCode errorCode)
    {
        return errorCode is NCrypt.ErrorCode.NTE_BUFFER_TOO_SMALL or NCrypt.ErrorCode.TPM_E_PCP_BUFFER_TOO_SMALL;
    }
}
