// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

internal sealed partial class TerminalFormatStrings
{
    /// <summary>The cached instance for the current terminal.</summary>
    public static TerminalFormatStrings Instance => s_instance;
    private static readonly TerminalFormatStrings s_instance = new(TermInfo.DatabaseFactory.ReadActiveDatabase());
}
