// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Configuration;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ConfigurationTests
{
    public class BinaryFormatterDeprecationTests
    {
        private static bool AreBinaryFormatterAndRemoteExecutorSupportedOnThisPlatform => PlatformDetection.IsBinaryFormatterSupported && RemoteExecutor.IsSupported;

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "SettingsSerializeAs.Binary is deprecated only on Core")]
        [Fact]
        public void ThrowOnSettingsPropertyConstructorWithSettingsSerializeAsBinary()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Assert.Throws<NotSupportedException>(() => new SettingsProperty("Binary", typeof(byte[]), null, false,"AString", SettingsSerializeAs.Binary, new SettingsAttributeDictionary(), true, true));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "SettingsSerializeAs.Binary is deprecated only on Core")]
        [ConditionalFact(nameof(AreBinaryFormatterAndRemoteExecutorSupportedOnThisPlatform))]
        public void SerializeAndDeserializeWithSettingsSerializeAsBinary()
        {
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions.Add("System.Configuration.ConfigurationManager.EnableUnsafeBinaryFormatterInPropertyValueSerialization", bool.TrueString);
            RemoteExecutor.Invoke(() =>
            {
#pragma warning disable CS0618 // Type or member is obsolete
                SettingsProperty property = new SettingsProperty("Binary", typeof(string), null, false, "AString", SettingsSerializeAs.Binary, new SettingsAttributeDictionary(), true, true);
#pragma warning restore CS0618 // Type or member is obsolete
                SettingsPropertyValue value = new SettingsPropertyValue(property);
                value.PropertyValue = "AString"; // To force _changedSinceLastSerialized to true to allow for serialization in the next call
                object serializedValue = value.SerializedValue;
                Assert.NotNull(serializedValue);
                value.Deserialized = false;
                object deserializedValue = value.PropertyValue;
                Assert.Equal("AString", deserializedValue);
            }, options).Dispose();
        }
    }
}
