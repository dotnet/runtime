// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Security.AccessControl
{
    public sealed partial class RegistrySecurity : NativeObjectSecurity
    {
#pragma warning disable IDE0060
        private static Exception _HandleErrorCodeCore(int errorCode)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }
#pragma warning restore IDE0060
    }
}
