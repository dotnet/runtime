// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class TextInfo
    {
        private unsafe void NlsChangeCase(char* pSource, int pSourceLen, char* pResult, int pResultLen, bool toUpper)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
#if TARGET_BROWSER
            Debug.Assert(!GlobalizationMode.Hybrid);
#endif
            Debug.Assert(GlobalizationMode.UseNls);
            Debug.Assert(pSource != null);
            Debug.Assert(pResult != null);
            Debug.Assert(pSourceLen >= 0);
            Debug.Assert(pResultLen >= 0);
            Debug.Assert(pSourceLen <= pResultLen);

            // Check for Invariant to avoid A/V in LCMapStringEx
            // We don't specify LCMAP_LINGUISTIC_CASING for Invariant because it will enable Turkish-I behavior too which is not
            // right for Invariant.
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
