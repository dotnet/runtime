// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal static partial class Interop
{
    internal static partial class Globalization
    {
        // needs to be kept in sync with ResultCode in System.Globalization.Native
        internal enum ResultCode
        {
            Success = 0,
            UnknownError = 1,
            InsufficentBuffer = 2,
            OutOfMemory = 3
        }
    }
}
