// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Test.Cryptography
{
    // See System.Security.Cryptography/tests/osslplugins/README.md for instructions on how to setup for TPM tests.
    public static class OpenSslNamedKeysHelpers
    {
        private const string EnvVarPrefix = "DOTNET_CRYPTOGRAPHY_TESTS_";

        private const string EngineEnvVarPrefix = EnvVarPrefix + "ENGINE_";
        private const string TestEngineEnabledEnvVarName = EngineEnvVarPrefix + "ENABLE";

        private const string TpmEnvVarPrefix = EnvVarPrefix + "TPM_";
        private const string TpmEcDsaKeyHandleEnvVarName = TpmEnvVarPrefix + "ECDSA_KEY_HANDLE";
        private const string TpmEcDhKeyHandleEnvVarName = TpmEnvVarPrefix + "ECDH_KEY_HANDLE";
        private const string TpmRsaKeyHandleEnvVarName = TpmEnvVarPrefix + "RSA_KEY_HANDLE";

        public const string NonExistingEngineName = "dntestnonexisting";
        public const string NonExistingEngineOrProviderKeyName = "nonexisting";

        public const string TestEngineName = "dntest";
        public const string TestEngineKeyId = "first";
        public const string TpmTssEngineName = "tpm2tss";

        public const string Tpm2ProviderName = "tpm2";

        public static string TpmEcDsaKeyHandle { get; } = Environment.GetEnvironmentVariable(TpmEcDsaKeyHandleEnvVarName);
        public static string TpmEcDsaKeyHandleUri { get; } = GetHandleKeyUri(TpmEcDsaKeyHandle);

        private static string TpmEcDhKeyHandle { get; } = Environment.GetEnvironmentVariable(TpmEcDhKeyHandleEnvVarName);
        public static string TpmEcDhKeyHandleUri { get; } = GetHandleKeyUri(TpmEcDhKeyHandle);

        private static string TpmRsaKeyHandle { get; } = Environment.GetEnvironmentVariable(TpmRsaKeyHandleEnvVarName);
        public static string TpmRsaKeyHandleUri { get; } = GetHandleKeyUri(TpmRsaKeyHandle);

        public static bool ShouldRunEngineTests { get; } = PlatformDetection.OpenSslPresentOnSystem && StringToBool(Environment.GetEnvironmentVariable(TestEngineEnabledEnvVarName));

        public static bool ProvidersSupported { get; } = PlatformDetection.IsOpenSsl3;
        public static bool ProvidersNotSupported => !ProvidersSupported;
        public static bool ShouldRunProviderEcDsaTests { get; } = ProvidersSupported && !string.IsNullOrEmpty(TpmEcDsaKeyHandleUri);
        public static bool ShouldRunProviderEcDhTests { get; } = ProvidersSupported && !string.IsNullOrEmpty(TpmEcDhKeyHandleUri);
        public static bool ShouldRunProviderRsaTests { get; } = ProvidersSupported && !string.IsNullOrEmpty(TpmRsaKeyHandleUri);
        public static bool ShouldRunAnyProviderTests => ShouldRunProviderEcDsaTests || ShouldRunProviderEcDhTests || ShouldRunProviderRsaTests;

        public static bool ShouldRunTpmTssTests => ShouldRunEngineTests && !string.IsNullOrEmpty(TpmEcDsaKeyHandle);

        public static readonly string AnyProviderKeyUri = TpmEcDsaKeyHandleUri ?? TpmEcDhKeyHandleUri ?? TpmRsaKeyHandleUri ?? "test";

        private static bool StringToBool(string? value)
            => "true".Equals(value, StringComparison.OrdinalIgnoreCase) || value == "1";

        private static string GetHandleKeyUri(string handle)
            => string.IsNullOrEmpty(handle) ? null : $"handle:{handle}";

        public static IEnumerable<object[]> RSASignaturePaddingValues()
        {
            yield return new object[] { RSASignaturePadding.Pkcs1 };
            yield return new object[] { RSASignaturePadding.Pss };
        }
    }
}
