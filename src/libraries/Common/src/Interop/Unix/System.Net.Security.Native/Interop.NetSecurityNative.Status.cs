// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class NetSecurityNative
    {
        // https://www.gnu.org/software/gss/reference/gss.pdf Page 65
        internal const int GSS_C_ROUTINE_ERROR_OFFSET = 16;

        // https://www.gnu.org/software/gss/reference/gss.pdf Page 9
        internal enum Status : uint
        {
            GSS_S_COMPLETE = 0,
            GSS_S_CONTINUE_NEEDED = 1,
            GSS_S_BAD_MECH = 1 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_NAME = 2 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_NAMETYPE = 3 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_BINDINGS = 4 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_STATUS = 5 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_SIG = 6 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_NO_CRED = 7 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_NO_CONTEXT = 8 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_DEFECTIVE_TOKEN = 9 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_DEFECTIVE_CREDENTIAL = 10 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_CREDENTIALS_EXPIRED = 11 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_CONTEXT_EXPIRED = 12 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_FAILURE = 13 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_BAD_QOP = 14 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_UNAUTHORIZED = 15 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_UNAVAILABLE = 16 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_DUPLICATE_ELEMENT = 17 << GSS_C_ROUTINE_ERROR_OFFSET,
            GSS_S_NAME_NOT_MN = 18 << GSS_C_ROUTINE_ERROR_OFFSET,
        }
    }
}
