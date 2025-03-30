// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        /// <summary>
        /// Gets the extra handle associated with the EVP_PKEY. Some tests need to access
        /// the interop layer and achieve this by adding the relevant classes to the test
        /// project as links. However, accesses to internal members like <see cref="SafeEvpPKeyHandle.ExtraHandle"/>
        /// in the product project will not work in the test project. In this particular case,
        /// the test project does not need the value of the handle, so it can implement this
        /// method to return a null pointer.
        /// </summary>
        /// <param name="handle">
        ///  The extra handle associated with the EVP_PKEY.</param>
        /// <returns>
        ///  The extra handle associated with the EVP_PKEY.
        /// </returns>
        private static IntPtr GetExtraHandle(SafeEvpPKeyHandle handle) => handle.ExtraHandle;
    }
}
