// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Text.Json.SourceGeneration
{
    internal enum ClassType
    {
        TypeUnsupportedBySourceGen = 0,
        TypeUnsupportedNoDefaultConverter = 1,
        Object = 2,
        KnownType = 3,
        TypeWithDesignTimeProvidedCustomConverter = 4,
        Enumerable = 5,
        Dictionary = 6,
        Nullable = 7,
        Enum = 8
    }
}
