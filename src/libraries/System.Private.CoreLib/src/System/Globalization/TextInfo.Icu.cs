// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    public partial class TextInfo
    {
        private Tristate _needsTurkishCasing = Tristate.NotInitialized;

        private static bool NeedsTurkishCasing(string localeName)
        {
            Debug.Assert(localeName != null);

            return CultureInfo.GetCultureInfo(localeName).CompareInfo.Compare("\u0131", "I", CompareOptions.IgnoreCase) == 0;
        }

        internal unsafe void IcuChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
#if TARGET_BROWSER
            Debug.Assert(!GlobalizationMode.Hybrid);
#endif
            Debug.Assert(!GlobalizationMode.UseNls);

            if (HasEmptyCultureName)
            {
                Interop.Globalization.ChangeCaseInvariant(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
            }
            else
            {
                if (_needsTurkishCasing == Tristate.NotInitialized)
                {
                    _needsTurkishCasing = NeedsTurkishCasing(_textInfoName) ? Tristate.True : Tristate.False;
                }
                if (_needsTurkishCasing == Tristate.True)
                {
                    Interop.Globalization.ChangeCaseTurkish(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
                }
                else
                {
                    Interop.Globalization.ChangeCase(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
                }
            }
        }

    }
}
