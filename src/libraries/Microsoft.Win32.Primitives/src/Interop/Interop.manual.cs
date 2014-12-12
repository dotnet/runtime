// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport("api-ms-win-core-localization-l1-2-0.dll", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = true)]
        public static extern int FormatMessage(int dwFlags, IntPtr lpSource_mustBeNull, uint dwMessageId,
            int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr[] arguments);

        public const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
        public const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
        public const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

        public const int ERROR_INSUFFICIENT_BUFFER = 0x7A;

        public const int E_FAIL = unchecked((int)0x80004005);
    }
}




