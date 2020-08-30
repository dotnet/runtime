// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public sealed partial class Thread
    {
        private static Exception GetApartmentStateChangeFailedException() => new PlatformNotSupportedException(SR.PlatformNotSupported_ComInterop);
    }
}
