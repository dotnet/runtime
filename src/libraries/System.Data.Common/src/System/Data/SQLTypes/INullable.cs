// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data.SqlTypes
{
    public interface INullable
    {
        bool IsNull { get; }
    }
}
