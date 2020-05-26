// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    internal partial class GlobalizationMode
    {
        internal static bool Invariant { get; } = GetInvariantSwitchValue();
        internal static bool UseNls { get; } = !Invariant &&
            (GetSwitchValue("DOTNET_SYSTEM_GLOBALIZATION_USENLS") ||
                Interop.Globalization.LoadICU() == 0);
    }
}
