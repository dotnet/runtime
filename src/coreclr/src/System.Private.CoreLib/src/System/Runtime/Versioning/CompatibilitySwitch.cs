// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Versioning
{
    internal static class CompatibilitySwitch
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string? GetValueInternal(string compatibilitySwitchName);
    }
}
