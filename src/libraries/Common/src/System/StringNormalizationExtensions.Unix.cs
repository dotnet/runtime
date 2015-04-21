// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;
using System.Text;

namespace System
{
    // TODO: We need an actual implementation here, but since all ASCII strings are normalized under all normalization forms
    // we can handle the simple cases, which will actually allow a lot of code to just work.

    static partial class StringNormalizationExtensions
    {
        [SecurityCritical]
        public static bool IsNormalized(this string value, NormalizationForm normalizationForm)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] > 0x7F)
                {
                    throw NotImplemented.ByDesign;
                }
            }

            return true;
        }

        [SecurityCritical]
        public static string Normalize(this string value, NormalizationForm normalizationForm)
        {
            if (IsNormalized(value, normalizationForm))
            {
                return value;
            }

            throw NotImplemented.ByDesign;
        }
    }
}

