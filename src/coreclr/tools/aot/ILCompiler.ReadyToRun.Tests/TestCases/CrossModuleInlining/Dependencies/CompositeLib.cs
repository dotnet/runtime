// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public static class CompositeLib
{
    public static int GetCompositeValue() => 100;

    public class CompositeType
    {
        public string Name { get; set; } = "Composite";
    }
}
