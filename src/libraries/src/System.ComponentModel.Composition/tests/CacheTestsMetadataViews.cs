// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.ComponentModel.Composition
{
    public interface ITrans_CacheTests_SimpleMetadataView
    {
        string String { get; }
        int Int { get; }
        float Float { get; }
        Type Type { get; }
        object Object { get; }
        IEnumerable<string> Collection { get; }
    }
}
