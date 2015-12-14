// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

public class C
{
    public static string teststr1 = "static \uC09C\u7B8B field";
    public static string[] teststr2 = new string[] { "\u3F2Aarray element 0", "array element 1\uCB53", "array \u47BBelement 2" };
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static string teststr3()
    {
        return @"method return\\";
    }
    public const string teststr4 = "const string\"";
    public static string teststr5 = String.Empty;
}
