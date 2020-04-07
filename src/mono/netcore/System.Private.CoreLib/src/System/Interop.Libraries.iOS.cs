// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Libraries
    {
        internal const string SystemNative = "@executable_path/libSystem.Native.dylib";
        internal const string GlobalizationNative = "@executable_path/libSystem.Globalization.Native.dylib";
        internal const string NetSecurityNative = "@executable_path/libSystem.Net.Security.Native.dylib";
        internal const string CryptoNative = "@executable_path/libSystem.Security.Cryptography.Native.OpenSsl.dylib";
        internal const string CompressionNative = "@executable_path/libSystem.IO.Compression.Native.dylib";
        internal const string IOPortsNative = "@executable_path/libSystem.IO.Ports.Native.dylib";
    }
}
