// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System
{
    public static partial class StringNormalizationExtensions
    {
        public static bool IsNormalized(this string strInput)
        {
            return IsNormalized(strInput, NormalizationForm.FormC);
        }

        public static bool IsNormalized(this string strInput, NormalizationForm normalizationForm)
        {
            ArgumentNullException.ThrowIfNull(strInput);

            return strInput.IsNormalized(normalizationForm);
        }

        public static string Normalize(this string strInput)
        {
            // Default to Form C
            return Normalize(strInput, NormalizationForm.FormC);
        }

        public static string Normalize(this string strInput, NormalizationForm normalizationForm)
        {
            ArgumentNullException.ThrowIfNull(strInput);

            return strInput.Normalize(normalizationForm);
        }
    }
}
