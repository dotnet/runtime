// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Text.NormalizationForm))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Globalization.IdnMapping))]

namespace System
{
    public static class StringNormalizationExtensions
    {
        public static bool IsNormalized(this string value) { return default(bool); }
        [System.Security.SecurityCritical]
        public static bool IsNormalized(this string value, System.Text.NormalizationForm normalizationForm) { return default(bool); }
        public static String Normalize(this string value) { return default(string); }
        [System.Security.SecurityCritical]
        public static String Normalize(this string value, System.Text.NormalizationForm normalizationForm) { return default(string); }
    }
}

namespace System.Globalization
{
    public static partial class GlobalizationExtensions
    {
        public static StringComparer GetStringComparer(this CompareInfo compareInfo, CompareOptions options) { return default(StringComparer); }
    }
}