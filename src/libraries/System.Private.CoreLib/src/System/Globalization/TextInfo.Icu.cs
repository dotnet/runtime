// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Globalization
{
    public partial class TextInfo
    {
        private NullableBool _needsTurkishCasing;

        private static bool NeedsTurkishCasing(string localeName)
        {
            Debug.Assert(localeName != null);

            return CultureInfo.GetCultureInfo(localeName).CompareInfo.Compare("\u0131", "I", CompareOptions.IgnoreCase) == 0;
        }

        [RequiresUnsafe]
        internal unsafe void IcuChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, bool bToUpper)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            if (HasEmptyCultureName)
            {
                Interop.Globalization.ChangeCaseInvariant(src, srcLen, dstBuffer, dstBufferCapacity, bToUpper);
            }
            else
            {
                if (_needsTurkishCasing == NullableBool.Undefined)
                {
                    _needsTurkishCasing = NeedsTurkishCasing(_textInfoName) ? NullableBool.True : NullableBool.False;
                }
                if (_needsTurkishCasing == NullableBool.True)
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
