// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Sdk;

namespace System.Tests
{
    public partial class SecureAppContextTests
    {
        // these tests only make sense on platforms where reflection is expected to forbid overwriting initonly fields
        public static bool RunForbidSettingInitonlyTests => PlatformDetection.IsNetCore && RemoteExecutor.IsSupported;

        [ConditionalFact(nameof(RunForbidSettingInitonlyTests))]
        public void CannotUseReflectionToChangeValues()
        {
            RemoteExecutor.Invoke(() =>
            {
                Type secureAppContextType = typeof(AppContext).Assembly.GetType("System.SecureAppContext", throwOnError: true);

                foreach (FieldInfo field in secureAppContextType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    try
                    {
                        Assert.True(field.FieldType.IsValueType, "Field is a reference type; instance members may be subject to mutation.");
                        Assert.True(field.IsInitOnly, "Field is mutable.");

                        object originalValue = field.GetValue(null);
                        Assert.Throws<FieldAccessException>(() => field.SetValue(null, originalValue));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failure when testing field {field.Name}.", ex);
                    }
                }
            }).Dispose();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData("Switch.System.Runtime.Serialization.SerializationGuard", "SerializationGuardEnabled", true)]
        [InlineData("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", "BinaryFormatterEnabled", true)]
        public void PopulatedAtAppDomainStart(string switchName, string propertyName, bool defaultValue)
        {
            RunTest(switchName, propertyName, null, defaultValue);
            RunTest(switchName, propertyName, true, true);
            RunTest(switchName, propertyName, false, false);

            static void RunTest(string switchName, string propertyName, bool? configuredValue, bool expectedValue)
            {
                try
                {
                    RemoteInvokeOptions options = null;
                    if (configuredValue != null)
                    {
                        options = new RemoteInvokeOptions();
                        options.RuntimeConfigurationOptions[switchName] = configuredValue.ToString();
                    }

                    RemoteExecutor.Invoke((switchName, propertyName, expectedValue) =>
                    {
                        Type secureAppContextType = typeof(AppContext).Assembly.GetType("System.SecureAppContext", throwOnError: true);
                        PropertyInfo pi = secureAppContextType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (pi == null)
                        {
                            throw new XunitException($"Property {propertyName} not found.");
                        }

                        Assert.Equal(bool.Parse(expectedValue), pi.GetValue(null));
                    },
                    switchName, propertyName, expectedValue.ToString(), options).Dispose();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Exception occurred for scenario configuredValue={configuredValue?.ToString() ?? "<null>"}, expectedValue={expectedValue}.", ex);
                }
            }
        }
    }
}
