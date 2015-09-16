// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography.X509Certificates
{
    internal static class X509StoreExtensions
    {
        // TODO (Issue #3114): remove this shim as soon as the implementation is available.
        // Placeholder for the X509Store(IntPtr) ctor.
        internal static X509Store CreateFromNativeHandle(IntPtr storeHandle)
        {
            return new X509Store(StoreName.My, StoreLocation.CurrentUser);
        }
    }
}
