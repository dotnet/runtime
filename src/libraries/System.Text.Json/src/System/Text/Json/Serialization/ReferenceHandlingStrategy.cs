// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.Serialization
{
    internal enum ReferenceHandlingStrategy
    {
        None = 0,
        Preserve = 1,
        IgnoreCycles = 2,
    }
}
