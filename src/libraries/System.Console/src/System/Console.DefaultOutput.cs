// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System;

public static partial class Console
{
    private static Stream CreateStandardOutputStream(bool isError) =>
        isError ? ConsolePal.OpenStandardError() : ConsolePal.OpenStandardOutput();

    private static void ClearConsole() => ConsolePal.Clear();
}
