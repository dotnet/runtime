// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public partial class CryptoConfig
    {
        internal const string CreateFromNameUnreferencedCodeMessage = "The default algorithm implementations might be removed, use strong type references like 'RSA.Create()' instead.";

        // .NET Core does not support AllowOnlyFipsAlgorithms
        public static bool AllowOnlyFipsAlgorithms => false;
    }
}
