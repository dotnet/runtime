// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Encryption.Aes.Tests
{
    using Aes = System.Security.Cryptography.Aes;

    public class AesProvider : IAesProvider
    {
        // Issue 7201: Windows 7 (Microsoft Windows 6.1) KSP does not support
        // AES, so temporarily recycle the BCrypt.dll-based implementation
        // from the Algorithms library, pending a change to make ephemeral
        // keys always use BCrypt.
        private static readonly Func<Aes> s_creator =
            RuntimeInformation.OSDescription.Contains("Windows 6.1") ? Aes.Create : (Func<Aes>)(() => new AesCng());

        public Aes Create()
        {
            return s_creator();
        }
    }

    public partial class AesFactory
    {
        private static readonly IAesProvider s_provider = new AesProvider();
    }
}
