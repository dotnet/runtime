// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    [Flags]
    internal enum MetadataPropertyName : byte
    {
        None       = 0,
        Values     = 1,
        Id         = 2,
        Ref        = 4,
        Type       = 8,
    }
}
