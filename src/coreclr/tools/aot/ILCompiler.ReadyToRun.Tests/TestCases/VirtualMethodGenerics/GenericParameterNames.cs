// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class GenericParameterNames
{
    public static bool CompareUnicodeProperties()
    {
        var value = new { \u3042 = "", \u3044 = "" };
        return value.Equals(new { \u3042 = "a", \u3044 = "b" });
    }

    public static bool CompareUnderscoreProperties()
    {
        var value = new { a_b = "", a\uFF3Fb = "" };
        return value.Equals(new { a_b = "a", a\uFF3Fb = "b" });
    }

    public static object[] GetComparers() =>
        GenericParameters<string, object>.GetComparers<string, object>();
}

public static class GenericParameters<\u3042, \u3044>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object[] GetComparers<\u3046, \u3048>() =>
    [
        EqualityComparer<\u3042>.Default,
        EqualityComparer<\u3044>.Default,
        EqualityComparer<\u3046>.Default,
        EqualityComparer<\u3048>.Default,
    ];
}
