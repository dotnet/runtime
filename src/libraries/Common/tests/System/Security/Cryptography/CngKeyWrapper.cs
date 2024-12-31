// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Xunit;
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace Test.Cryptography
{
    internal sealed class CngKeyWrapper : IDisposable
    {
        private CngKeyWrapper(
            CngAlgorithm algorithm,
            CngKeyCreationParameters cngCreationParameters,
            string? keySuffix = null,
            [CallerMemberName] string? testName = null)
        {
            Key = CngKey.Create(algorithm, $"{testName}{algorithm.Algorithm}{keySuffix}", cngCreationParameters);
        }

        public static CngKeyWrapper CreateMicrosoftPlatformCryptoProvider(
            CngAlgorithm algorithm,
            string? keySuffix = null,
            [CallerMemberName] string? testName = null,
            CngKeyCreationOptions creationOption = CngKeyCreationOptions.None,
            params CngProperty[] additionalParameters)
        {
            const string MicrosoftPlatformCryptoProvider = "Microsoft Platform Crypto Provider";

#if NETFRAMEWORK
            CngProvider cngProvider = new(MicrosoftPlatformCryptoProvider);
#else
            Assert.Equal(MicrosoftPlatformCryptoProvider, CngProvider.MicrosoftPlatformCryptoProvider.Provider);
            CngProvider cngProvider = CngProvider.MicrosoftPlatformCryptoProvider;
#endif
            CngKeyCreationParameters cngCreationParameters = new()
            {
                Provider = cngProvider,
                KeyCreationOptions = creationOption | CngKeyCreationOptions.OverwriteExistingKey,
            };

            foreach (CngProperty parameter in additionalParameters)
            {
                cngCreationParameters.Parameters.Add(parameter);
            }

            return new CngKeyWrapper(algorithm, cngCreationParameters, keySuffix, testName);
        }

        public static CngKeyWrapper CreateMicrosoftSoftwareKeyStorageProvider(
            CngAlgorithm algorithm,
            CngKeyCreationOptions creationOption,
            string? keySuffix = null,
            [CallerMemberName] string? testName = null)
        {
            CngKeyCreationParameters cngCreationParameters = new()
            {
                Provider = CngProvider.MicrosoftSoftwareKeyStorageProvider,
                KeyCreationOptions = creationOption | CngKeyCreationOptions.OverwriteExistingKey,
            };

            return new CngKeyWrapper(algorithm, cngCreationParameters, keySuffix, testName);
        }

        public CngKey Key { get; }

        public void Dispose()
        {
            Key.Delete();
        }
    }
}
