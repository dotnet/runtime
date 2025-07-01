// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Schema
{
    [Flags]
    internal enum JsonSchemaType
    {
        Any = 0,
        Null = 1,
        Boolean = 2,
        Integer = 4,
        Number = 8,
        String = 16,
        Object = 32,
        Array = 64,
    }
}
