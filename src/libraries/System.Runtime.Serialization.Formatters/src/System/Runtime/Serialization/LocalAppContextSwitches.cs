// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_binaryFormatterEnabled;
        public static bool BinaryFormatterEnabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", ref s_binaryFormatterEnabled);
        }
    }
}
