// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security;
using System.Text;

namespace System.Globalization
{
    public partial class TextInfo
    {
        private Tristate _needsTurkishCasing = Tristate.NotInitialized;

        private void FinishInitialization()
        {
        }

        private unsafe string ChangeCase(string s, bool toUpper)
        {
            Debug.Assert(!_invariantMode);

            Debug.Assert(s != null);

            if (s.Length == 0)
            {
                return string.Empty;
            }

            string result = string.FastAllocateString(s.Length);

            fixed (char* pSource = s)
            {
                fixed (char* pResult = result)
                {
#if CORECLR
                    if (IsAsciiCasingSameAsInvariant && s.IsAscii())
                    {
                        int length = s.Length;
                        char* a = pSource, b = pResult;
                        if (toUpper)
                        {
                            while (length-- != 0)
                            {
                                *b++ = ToUpperAsciiInvariant(*a++);
                            }
                        }
                        else
                        {
                            while (length-- != 0)
                            {
                                *b++ = ToLowerAsciiInvariant(*a++);
                            }
                        }
                    }
                    else
#endif
                    {
                        ChangeCase(pSource, s.Length, pResult, result.Length, toUpper);
                    }
                }
            }

            return result;
        }

        private unsafe char ChangeCase(char c, bool toUpper)
        {
            Debug.Assert(!_invariantMode);

            char dst = default(char);

            ChangeCase(&c, 1, &dst, 1, toUpper);

            return dst;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private bool NeedsTurkishCasing(string localeName)
        {
            Debug.Assert(localeName != null);

            return CultureInfo.GetCultureInfo(localeName).CompareInfo.Compare("\u0131", "I", CompareOptions.IgnoreCase) == 0;
        }

        private bool IsInvariant { get { return _cultureName.Length == 0; } }

        internal unsafe void ChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper)
        {
            Debug.Assert(!_invariantMode);

            if (IsInvariant)
            {
                Interop.GlobalizationInterop.ChangeCaseInvariant(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
            }
            else
            {
                if (_needsTurkishCasing == Tristate.NotInitialized)
                {
                    _needsTurkishCasing = NeedsTurkishCasing(_textInfoName) ? Tristate.True : Tristate.False;
                }
                if (_needsTurkishCasing == Tristate.True)
                {
                    Interop.GlobalizationInterop.ChangeCaseTurkish(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
                }
                else
                {
                    Interop.GlobalizationInterop.ChangeCase(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
                }
            }
        }

    }
}
