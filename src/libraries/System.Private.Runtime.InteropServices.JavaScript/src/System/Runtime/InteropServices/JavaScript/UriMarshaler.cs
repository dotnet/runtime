// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    public static class UriMarshaler
    {
        public static Uri FromJavaScript (string s)
        {
            return new Uri(s);
        }

        public static string ToJavaScript (Uri u)
        {
            // FIXME: Uri.ToString() escapes certain characters in URIs.
            // This may not be desirable, but the old marshaler seems to have had this limitation too.
            return u.ToString();
        }
    }
}
