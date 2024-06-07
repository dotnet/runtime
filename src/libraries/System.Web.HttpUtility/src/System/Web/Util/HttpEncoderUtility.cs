// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Web.Util
{
    internal static class HttpEncoderUtility
    {
        //  Helper to encode spaces only
        [return: NotNullIfNotNull(nameof(str))]
        internal static string? UrlEncodeSpaces(string? str) => str != null && str.Contains(' ') ? str.Replace(" ", "%20") : str;
    }
}
