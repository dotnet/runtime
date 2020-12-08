// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Libraries
    {
#if TARGET_WINDOWS
        internal const string GlobalizationNative = "System.Globalization.Native";
        internal const string CompressionNative = "System.IO.Compression.Native";
#else
        internal const string GlobalizationNative = "libSystem.Globalization.Native";
        internal const string CompressionNative = "libSystem.IO.Compression.Native";
#endif
    }
}
