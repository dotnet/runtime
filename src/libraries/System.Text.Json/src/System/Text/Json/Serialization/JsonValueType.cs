// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Text.Json.Serialization
{
    [Flags]
    internal enum JsonValueType : byte
    {
        None = 0,
        Object = 1,
        Array = 2,
        String = 4,
        Number = 8,
        Boolean = 16,
        Null = 32,
    }
}
