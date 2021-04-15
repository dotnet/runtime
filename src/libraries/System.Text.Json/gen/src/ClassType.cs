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
        Object = 1,
        KnownType = 2,
        TypeWithDesignTimeProvidedCustomConverter = 3,
        Enumerable = 4,
        Dictionary = 5,
        Nullable = 6,
        Enum = 7,
    }
}
