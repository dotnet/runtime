// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Globalization
{
    public partial class TextInfo
    {
        private unsafe void FinishInitialization()
        {
            if (GlobalizationMode.Invariant)
            {
                _sortHandle = IntPtr.Zero;
                return;
            }

            const uint LCMAP_SORTHANDLE = 0x20000000;

            IntPtr handle;
            int ret = Interop.Kernel32.LCMapStringEx(_textInfoName, LCMAP_SORTHANDLE, null, 0, &handle, IntPtr.Size, null, null, IntPtr.Zero);
            _sortHandle = ret > 0 ? handle : IntPtr.Zero;
        }

        private unsafe void ChangeCase(char* pSource, int pSourceLen, char* pResult, int pResultLen, bool toUpper)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(pSource != null);
            Debug.Assert(pResult != null);
            Debug.Assert(pSourceLen >= 0);
            Debug.Assert(pResultLen >= 0);
            Debug.Assert(pSourceLen <= pResultLen);

            // Check for Invariant to avoid A/V in LCMapStringEx
            uint linguisticCasing = IsInvariantLocale(_textInfoName) ? 0 : LCMAP_LINGUISTIC_CASING;

            int ret = Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _textInfoName,
                                                     linguisticCasing | (toUpper ? LCMAP_UPPERCASE : LCMAP_LOWERCASE),
                                                     pSource,
                                                     pSourceLen,
                                                     pResult,
                                                     pSourceLen,
                                                     null,
                                                     null,
                                                     _sortHandle);
            if (ret == 0)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }

            Debug.Assert(ret == pSourceLen, "Expected getting the same length of the original string");
        }

        // PAL Ends here

        private IntPtr _sortHandle;

        private const uint LCMAP_LINGUISTIC_CASING = 0x01000000;
        private const uint LCMAP_LOWERCASE = 0x00000100;
        private const uint LCMAP_UPPERCASE = 0x00000200;

        private static bool IsInvariantLocale(string localeName) => localeName == "";
    }
}
