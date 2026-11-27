// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace SuperPmi.Mcs;

internal static class Program
{
    private static int Main(string[] args) =>
        new McsRootCommand()
            .Parse(args)
            .Invoke(new()
            {
                EnableDefaultExceptionHandler = false
            });
}
