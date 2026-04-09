// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public partial class RSA : AsymmetricAlgorithm
    {
        public static new partial RSA Create()
        {
            return new RSAWrapper(new RSAOpenSsl());
        }
    }
}
