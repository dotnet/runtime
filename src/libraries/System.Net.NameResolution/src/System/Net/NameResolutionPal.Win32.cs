// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net
{
    internal static partial class NameResolutionPal
    {
        private static bool GetAddrInfoExSupportsOverlapped()
        {
            if (!NativeLibrary.TryLoad(Interop.Libraries.Ws2_32, typeof(NameResolutionPal).Assembly, null, out IntPtr libHandle))
                return false;

            // We can't just check that 'GetAddrInfoEx' exists, because it existed before supporting overlapped.
            // The existence of 'GetAddrInfoExCancel' indicates that overlapped is supported.
            return NativeLibrary.TryGetExport(libHandle, Interop.Winsock.GetAddrInfoExCancelFunctionName, out _);
        }
    }
}
