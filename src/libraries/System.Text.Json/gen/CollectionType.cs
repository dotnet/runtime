// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Text.Json.SourceGeneration
{
    internal enum CollectionType
    {
        NotApplicable = 0,
        Array = 1,
        List = 2,
        IEnumerable = 3,
        IList = 4,
        Dictionary = 5
    }
}
