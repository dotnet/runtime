// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Security.Authentication.ExtendedProtection
{
    public partial class ExtendedProtectionPolicy
    {
        public static bool OSSupportsExtendedProtection
        {
            get
            {
                // .NET Core is supported only on Win7+ where ExtendedProtection is supported.
                // and unsupported on the browser
                return !OperatingSystem.IsBrowser();
            }
        }
    }
}
