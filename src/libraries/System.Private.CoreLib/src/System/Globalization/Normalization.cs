// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.Globalization
{
    internal static partial class Normalization
    {
        internal static bool IsNormalized(string strInput, NormalizationForm normalizationForm)
        {
            return GlobalizationMode.UseIcu ?
                IcuIsNormalized(strInput, normalizationForm) :
                NlsIsNormalized(strInput, normalizationForm);
        }

        internal static string Normalize(string strInput, NormalizationForm normalizationForm)
        {
            return GlobalizationMode.UseIcu ?
                IcuNormalize(strInput, normalizationForm) :
                NlsNormalize(strInput, normalizationForm);
        }
    }
}
