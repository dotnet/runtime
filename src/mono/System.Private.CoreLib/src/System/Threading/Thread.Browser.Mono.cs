// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Threading
{
    public partial class Thread
    {
        [UnsupportedOSPlatformGuard("browser")]
        internal static bool IsThreadStartSupported => false;

        [UnsupportedOSPlatform("browser")]
        public void Start() => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public void Start(object parameter) => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public void UnsafeStart() => throw new PlatformNotSupportedException();

        [UnsupportedOSPlatform("browser")]
        public void UnsafeStart(object parameter) => throw new PlatformNotSupportedException();
    }
}
