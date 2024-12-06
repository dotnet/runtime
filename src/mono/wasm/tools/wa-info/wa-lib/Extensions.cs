// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace WebAssemblyInfo
{
    public static class Extensions
    {
        public static string Indent(this string str, string? indent)
        {
            return indent + str.Replace("\n", "\n" + indent);
        }
    }
}
