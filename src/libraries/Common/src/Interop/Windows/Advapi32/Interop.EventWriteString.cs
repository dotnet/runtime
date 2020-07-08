// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, ExactSpelling = true)]
        internal static extern int EventWriteString(
            long registrationHandle,
            byte level,
            long keyword,
            string msg);
    }
}
