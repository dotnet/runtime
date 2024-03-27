// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal unsafe struct IOVector
        {
#pragma warning disable CS0649
            public byte* Base;
            public UIntPtr Count;
#pragma warning restore CS0649
        }
    }
}
