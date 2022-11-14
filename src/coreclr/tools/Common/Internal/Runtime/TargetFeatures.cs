// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace Internal.Runtime
{
    internal static class TargetFeatures
    {
        internal static bool TargetSupportsObjectiveCMarshal(TargetDetails target)
        {
            return target.IsOSX;
        }
    }
}
