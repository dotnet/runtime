// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Encryption.Des.Tests
{
    public class DESCryptoServiceProviderProvider : IDESProvider
    {
        public DES Create()
        {
            return new DESCryptoServiceProvider();
        }
    }

    public partial class DESFactory
    {
        private static readonly IDESProvider s_provider = new DESCryptoServiceProviderProvider();
    }
}
