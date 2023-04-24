// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_allowArbitraryTypeInstantiation;
        private static int s_allowUnsafeSerializationFormatBinary;

        public static bool AllowArbitraryTypeInstantiation
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("Switch.System.Data.AllowArbitraryDataSetTypeInstantiation", ref s_allowArbitraryTypeInstantiation);
        }

        public static bool AllowUnsafeSerializationFormatBinary
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("Switch.System.Data.AllowUnsafeSerializationFormatBinary", ref s_allowUnsafeSerializationFormatBinary);
        }
    }
}
