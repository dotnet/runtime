// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Test.Cryptography
{
    internal sealed class CngPlatformProviderKey : IDisposable
    {
        public CngPlatformProviderKey(
            CngAlgorithm algorithm,
            string keySuffix = null,
            [CallerMemberName] string testName = null,
            params CngProperty[] additionalParameters)
        {
            CngKeyCreationParameters cngCreationParameters = new CngKeyCreationParameters
            {
                Provider = CngProvider.MicrosoftPlatformCryptoProvider,
                KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey,
            };

            foreach (CngProperty parameter in additionalParameters)
            {
                cngCreationParameters.Parameters.Add(parameter);
            }

            Key = CngKey.Create(algorithm, $"{testName}{algorithm.Algorithm}{keySuffix}", cngCreationParameters);
        }

        internal CngKey Key { get; }

        public void Dispose()
        {
            Key.Delete();
        }
    }
}
