// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class TextInfo
    {
        private unsafe void FinishInitialization()
        {
            if (_invariantMode)
            {
                _sortHandle = IntPtr.Zero;
                return;
            }

            const uint LCMAP_SORTHANDLE = 0x20000000;

            IntPtr handle;
            int ret = Interop.Kernel32.LCMapStringEx(_textInfoName, LCMAP_SORTHANDLE, null, 0, &handle, IntPtr.Size, null, null, IntPtr.Zero);
            _sortHandle = ret > 0 ? handle : IntPtr.Zero;
        }

        private unsafe string ChangeCase(string s, bool toUpper)
        {
            Debug.Assert(!_invariantMode);

            Debug.Assert(s != null);

            //
            //  Get the length of the string.
            //
            int nLengthInput = s.Length;

            //
            //  Check if we have the empty string.
            //
            if (nLengthInput == 0)
            {
                return s;
            }

            int ret;

            // Check for Invariant to avoid A/V in LCMapStringEx
            uint linguisticCasing = IsInvariantLocale(_textInfoName) ? 0 : LCMAP_LINGUISTIC_CASING;

            //
            //  Create the result string.
            //
            string result = string.FastAllocateString(nLengthInput);

            fixed (char* pSource = s)
            fixed (char* pResult = result)
            {
                ret = Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _textInfoName,
                                                    linguisticCasing | (toUpper ? LCMAP_UPPERCASE : LCMAP_LOWERCASE),
                                                    pSource,
                                                    nLengthInput,
                                                    pResult,
                                                    nLengthInput,
                                                    null,
                                                    null,
                                                    _sortHandle);
            }

            if (ret == 0)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }

            Debug.Assert(ret == nLengthInput, "Expected getting the same length of the original string");
            return result;
        }

        private unsafe char ChangeCase(char c, bool toUpper)
        {
            Debug.Assert(!_invariantMode);

            char retVal = '\0';

            // Check for Invariant to avoid A/V in LCMapStringEx
            uint linguisticCasing = IsInvariantLocale(_textInfoName) ? 0 : LCMAP_LINGUISTIC_CASING;

            Interop.Kernel32.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : _textInfoName,
                                          toUpper ? LCMAP_UPPERCASE | linguisticCasing : LCMAP_LOWERCASE | linguisticCasing,
                                          &c,
                                          1,
                                          &retVal,
                                          1,
                                          null,
                                          null,
                                          _sortHandle);

            return retVal;
        }

        // PAL Ends here

        private IntPtr _sortHandle;

        private const uint LCMAP_LINGUISTIC_CASING = 0x01000000;
        private const uint LCMAP_LOWERCASE = 0x00000100;
        private const uint LCMAP_UPPERCASE = 0x00000200;

        private static bool IsInvariantLocale(string localeName)
        {
            return localeName == "";
        }
    }
}
