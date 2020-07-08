// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Libraries.Advapi32, ExactSpelling = true)]
        internal static extern unsafe int EventSetInformation(
            long registrationHandle,
            EVENT_INFO_CLASS informationClass,
            void* eventInformation,
            int informationLength);
    }
}
