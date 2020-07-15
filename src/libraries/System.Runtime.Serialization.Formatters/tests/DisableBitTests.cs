// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public static class DisableBitTests
    {
        // these tests only make sense on platforms with both SecureAppContext and RemoteExecutor support
        public static bool ShouldRunFullAppContextEnablementChecks => !PlatformDetection.IsNetFramework && RemoteExecutor.IsSupported;

        // determines whether BinaryFormatter will always fail, regardless of config, on this platform
        public static bool IsBinaryFormatterSuppressedOnThisPlatform => !PlatformDetection.IsBinaryFormatterSupported;

        private const string EnableBinaryFormatterSwitchName = "System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization";
        private const string MoreInfoUrl = "https://aka.ms/binaryformatter";

        [ConditionalFact(nameof(IsBinaryFormatterSuppressedOnThisPlatform))]
        public static void DisabledAlwaysInBrowser()
        {
            // First, test serialization

            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            var ex = Assert.Throws<PlatformNotSupportedException>(() => bf.Serialize(ms, "A string to serialize."));
            Assert.Contains(MoreInfoUrl, ex.Message, StringComparison.Ordinal); // error message should link to the more info URL

            // Then test deserialization

            ex = Assert.Throws<PlatformNotSupportedException>(() => bf.Deserialize(ms));
            Assert.Contains(MoreInfoUrl, ex.Message, StringComparison.Ordinal); // error message should link to the more info URL
        }

        [ConditionalFact(nameof(ShouldRunFullAppContextEnablementChecks))]
        public static void DisabledThroughAppContext()
        {
            RemoteExecutor.Invoke(() =>
            {
                AppContext.SetSwitch(EnableBinaryFormatterSwitchName, isEnabled: false);

                // First, test serialization

                MemoryStream ms = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                var ex = Assert.Throws<NotSupportedException>(() => bf.Serialize(ms, "A string to serialize."));
                Assert.Contains(MoreInfoUrl, ex.Message, StringComparison.Ordinal); // error message should link to the more info URL

                // Then test deserialization

                ex = Assert.Throws<NotSupportedException>(() => bf.Deserialize(ms));
                Assert.Contains(MoreInfoUrl, ex.Message, StringComparison.Ordinal); // error message should link to the more info URL
            }).Dispose();
        }

        [ConditionalFact(nameof(ShouldRunFullAppContextEnablementChecks))]
        public static void DisabledThroughSecureAppContext_CannotOverride()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions[EnableBinaryFormatterSwitchName] = bool.FalseString;

            RemoteExecutor.Invoke(() =>
            {
                AppContext.SetSwitch(EnableBinaryFormatterSwitchName, isEnabled: true); // shouldn't override SecureAppContext

                // First, test serialization

                MemoryStream ms = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                var ex = Assert.Throws<NotSupportedException>(() => bf.Serialize(ms, "A string to serialize."));
                Assert.Contains(MoreInfoUrl, ex.Message, StringComparison.Ordinal); // error message should link to the more info URL

                // Then test deserialization

                ex = Assert.Throws<NotSupportedException>(() => bf.Deserialize(ms));
                Assert.Contains(MoreInfoUrl, ex.Message, StringComparison.Ordinal); // error message should link to the more info URL
            }, options).Dispose();
        }
    }
}
