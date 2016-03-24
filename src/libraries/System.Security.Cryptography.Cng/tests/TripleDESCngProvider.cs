// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Encryption.TripleDes.Tests
{
    public class TripleDESCngProvider : ITripleDESProvider
    {
        // Issue 7201: Windows 7 (Microsoft Windows 6.1) KSP does not support
        // 3DES, so temporarily recycle the BCrypt.dll-based implementation
        // from the Algorithms library, pending a change to make ephemeral
        // keys always use BCrypt.
        private static readonly Func<TripleDES> s_creator =
            RuntimeInformation.OSDescription.Contains("Windows 6.1") ? TripleDES.Create : (Func<TripleDES>)(() => new TripleDESCng());

        public TripleDES Create()
        {
            return s_creator();
        }
    }

    public partial class TripleDESFactory
    {
        private static readonly ITripleDESProvider s_provider = new TripleDESCngProvider();
    }
}
