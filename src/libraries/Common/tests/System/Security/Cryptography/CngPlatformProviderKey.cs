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
            : this(algorithm, CreateDefaultParameters(), keySuffix, testName, additionalParameters)
        {
        }

        public CngPlatformProviderKey(
            CngAlgorithm algorithm,
            CngKeyCreationParameters cngCreationParameters,
            string keySuffix = null,
            [CallerMemberName] string testName = null,
            params CngProperty[] additionalParameters)
        {
            foreach (CngProperty parameter in additionalParameters)
            {
                cngCreationParameters.Parameters.Add(parameter);
            }

            Key = CngKey.Create(algorithm, $"{testName}{algorithm.Algorithm}{keySuffix}", cngCreationParameters);
        }

        private static CngKeyCreationParameters CreateDefaultParameters() =>
            new CngKeyCreationParameters
            {
                Provider = CngProvider.MicrosoftPlatformCryptoProvider,
                KeyCreationOptions = CngKeyCreationOptions.OverwriteExistingKey,
            };

        internal CngKey Key { get; }

        public void Dispose()
        {
            Key.Delete();
        }
    }
}
