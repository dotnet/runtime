// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Runtime.CompilerServices;


namespace System.Globalization
{
    internal partial class GlobalizationMode
    {
        internal static bool Invariant { get; } = GetInvariantSwitchValue();
        internal static bool UseNls {
            get {
                return !Invariant && Interop.Globalization.LoadICU() == 0;
            }
        }
        private static bool GetGlobalizationInvariantMode()
        {
            return Invariant;
        }
    }
}
