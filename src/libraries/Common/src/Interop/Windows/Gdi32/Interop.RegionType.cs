// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal enum RegionType : int
    {
        ERROR = 0,
        NULLREGION = 1,
        SIMPLEREGION = 2,
        COMPLEXREGION = 3,
    }
}
