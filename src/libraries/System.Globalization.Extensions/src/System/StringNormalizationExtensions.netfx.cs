// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public static class StringNormalizationExtensions
    {
        public static bool IsNormalized(this string value)
        {
            return value.IsNormalized();
        }

        public static bool IsNormalized(this string value, System.Text.NormalizationForm normalizationForm)
        {
            return value.IsNormalized(normalizationForm);
        }

        public static String Normalize(this string value)
        {
            return value.Normalize();
        }

        public static String Normalize(this string value, System.Text.NormalizationForm normalizationForm)
        {
            return value.Normalize(normalizationForm);
        }
    }
}
