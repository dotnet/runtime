// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Security.Cryptography
{
    public abstract partial class ECDsa : System.Security.Cryptography.AsymmetricAlgorithm
    {
        protected ECDsa() { }
        public static System.Security.Cryptography.ECDsa Create() { return default(System.Security.Cryptography.ECDsa); }
        public static System.Security.Cryptography.ECDsa Create(string algorithm) { return default(System.Security.Cryptography.ECDsa); }
        public abstract byte[] SignHash(byte[] hash);
        public abstract bool VerifyHash(byte[] hash, byte[] signature);
    }
}
