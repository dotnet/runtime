// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP

using System;

public static class ExtensionsForNetFramework
{
    public static string[] Split(this string s, char delimiter, int count, StringSplitOptions options = StringSplitOptions.None)
        => s.Split(new char[] { delimiter }, count, options);
}
#endif
