// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Metadata
{
    public enum ExceptionRegionKind : ushort
    {
        Catch = 0,
        Filter = 1,
        Finally = 2,
        Fault = 4,
    }
}
