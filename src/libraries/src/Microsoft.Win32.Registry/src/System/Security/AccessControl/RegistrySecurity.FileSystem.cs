// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.AccessControl
{
    public sealed partial class RegistrySecurity : NativeObjectSecurity
    {
        private static Exception _HandleErrorCodeCore(int errorCode, string name, SafeHandle handle, object context)
        {
            // TODO: Implement this
            throw new PlatformNotSupportedException();
        }
    }
}
