// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public static class AsyncExternalLib
{
    public static int ExternalValue => 77;

    public class AsyncExternalType
    {
        public string Label { get; set; } = "external";
    }
}
