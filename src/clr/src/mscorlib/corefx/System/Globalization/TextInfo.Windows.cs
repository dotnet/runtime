// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Diagnostics.Contracts;

namespace System.Globalization
{
    public partial class TextInfo
    {
        //////////////////////////////////////////////////////////////////////////
        ////
        ////  TextInfo Constructors
        ////
        ////  Implements CultureInfo.TextInfo.
        ////
        //////////////////////////////////////////////////////////////////////////
        internal unsafe TextInfo(CultureData cultureData)
        {
            // This is our primary data source, we don't need most of the rest of this
            this.m_cultureData = cultureData;
            this.m_cultureName = this.m_cultureData.CultureName;
            this.m_textInfoName = this.m_cultureData.STEXTINFO;
            FinishInitialization(this.m_textInfoName);
        }

        private void FinishInitialization(string textInfoName)
        {
            const uint LCMAP_SORTHANDLE = 0x20000000;

            long handle;
            int ret = Interop.mincore.LCMapStringEx(textInfoName, LCMAP_SORTHANDLE, null, 0, (IntPtr)(&handle), IntPtr.Size, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            _sortHandle = ret > 0 ? (IntPtr)handle : IntPtr.Zero;
        }

        private unsafe string ChangeCase(string s, bool toUpper)
        {
            Contract.Assert(s != null);   

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
            else
            {
                int result;

                // Check for Invariant to avoid A/V in LCMapStringEx
                uint linguisticCasing = IsInvariantLocale(m_textInfoName) ? 0 : LCMAP_LINGUISTIC_CASING;

                //
                //  Create the result string.
                //
                char[] buffer = new char[nLengthInput];
                fixed (char* pBuffer = buffer)
                {
                    result = Interop.mincore.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : m_textInfoName,
                                                           toUpper ? LCMAP_UPPERCASE | linguisticCasing : LCMAP_LOWERCASE | linguisticCasing,
                                                           s,
                                                           nLengthInput,
                                                           (IntPtr)pBuffer,
                                                           nLengthInput,
                                                           IntPtr.Zero,
                                                           IntPtr.Zero,
                                                           _sortHandle);
                }

                if (0 == result)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }

                Contract.Assert(result == nLengthInput, "Expected getting the same length of the original string");
                return new string(buffer, 0, result);
            }
        }

        private unsafe char ChangeCase(char c, bool toUpper)
        {
            char retVal = '\0';

            // Check for Invariant to avoid A/V in LCMapStringEx
            uint linguisticCasing = IsInvariantLocale(m_textInfoName) ? 0 : LCMAP_LINGUISTIC_CASING;

            Interop.mincore.LCMapStringEx(_sortHandle != IntPtr.Zero ? null : m_textInfoName,
                                          toUpper ? LCMAP_UPPERCASE | linguisticCasing : LCMAP_LOWERCASE | linguisticCasing,
                                          new string(c, 1),
                                          1,
                                          (IntPtr)(&retVal),
                                          1,
                                          IntPtr.Zero,
                                          IntPtr.Zero,
                                          _sortHandle);

            return retVal;
        }

        // PAL Ends here

        private readonly IntPtr _sortHandle;

        private const uint LCMAP_LINGUISTIC_CASING = 0x01000000;
        private const uint LCMAP_LOWERCASE = 0x00000100;
        private const uint LCMAP_UPPERCASE = 0x00000200;

        private static bool IsInvariantLocale(string localeName)
        {
            return localeName == "";
        }
    }
}
