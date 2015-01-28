// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace System
{
#if INTERNAL_GLOBALIZATION_EXTENSIONS
    internal 
#else
    public 
#endif
    static partial class StringNormalizationExtensions
    {
        public static bool IsNormalized(this string value)
        {
            return IsNormalized(value, NormalizationForm.FormC);
        }

        public static string Normalize(this string value)
        {
            // Default to Form C
            return Normalize(value, NormalizationForm.FormC);
        }
    }
}
