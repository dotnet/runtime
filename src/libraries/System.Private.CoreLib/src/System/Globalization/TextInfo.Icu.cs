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

            // ICU applies the Turkish dotted/dotless "i" casing rules to the "tr" and "az"
            // languages. This is determined from the locale name rather than by probing the
            // collation tailoring, because some platforms (notably Android, which uses the
            // system ICU) do not ship the collation data that probe relies on, which would
            // silently fall back to non-Turkish casing.
            ReadOnlySpan<char> language = localeName.AsSpan();
            int separatorIndex = language.IndexOfAny('-', '_');
            if (separatorIndex >= 0)
            {
                language = language.Slice(0, separatorIndex);
            }

            return language.Equals("tr", StringComparison.OrdinalIgnoreCase) ||
                   language.Equals("az", StringComparison.OrdinalIgnoreCase);
        }

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
