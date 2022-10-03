// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

#pragma warning disable IDE0060

namespace Internal.Runtime.InteropServices
{
    internal static class ComActivator
    {
        /// <summary>
        /// Internal entry point for unmanaged COM activation API from native code
        /// </summary>
        /// <param name="pCxtInt">Pointer to a <see cref="ComActivationContextInternal"/> instance</param>
        [UnmanagedCallersOnly]
        private static unsafe int GetClassFactoryForTypeInternal(ComActivationContextInternal* pCxtInt)
            => throw new PlatformNotSupportedException();

        /// <summary>
        /// Internal entry point for registering a managed COM server API from native code
        /// </summary>
        /// <param name="pCxtInt">Pointer to a <see cref="ComActivationContextInternal"/> instance</param>
        [UnmanagedCallersOnly]
        private static unsafe int RegisterClassForTypeInternal(ComActivationContextInternal* pCxtInt)
            => throw new PlatformNotSupportedException();

        /// <summary>
        /// Internal entry point for unregistering a managed COM server API from native code
        /// </summary>
        /// <param name="pCxtInt">Pointer to a <see cref="ComActivationContextInternal"/> instance</param>
        [UnmanagedCallersOnly]
        private static unsafe int UnregisterClassForTypeInternal(ComActivationContextInternal* pCxtInt)
            => throw new PlatformNotSupportedException();
    }
}
