// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Http.Tests
{
    public class RuntimeSettingParserTest
    {
        public static bool SupportsRemoteExecutor = RemoteExecutor.IsSupported;

        [ConditionalTheory(nameof(SupportsRemoteExecutor))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task QueryRuntimeSettingSwitch_WhenNotSet_DefaultIsUsed(bool defaultValue)
        {
            static void RunTest(string defaultValueStr)
            {
                bool expected = bool.Parse(defaultValueStr);
                bool actual = RuntimeSettingParser.QueryRuntimeSettingSwitch("Foo.Bar", "FOO_BAR", expected);
                Assert.Equal(expected, actual);
            }

            await RemoteExecutor.Invoke(RunTest, defaultValue.ToString()).DisposeAsync();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task QueryRuntimeSettingSwitch_AppContextHasPriority()
        {
            static void RunTest()
            {
                AppContext.SetSwitch("Foo.Bar", false);
                bool actual = RuntimeSettingParser.QueryRuntimeSettingSwitch("Foo.Bar", "FOO_BAR", true);
                Assert.False(actual);
            }
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "true";

            await RemoteExecutor.Invoke(RunTest, options).DisposeAsync();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task QueryRuntimeSettingSwitch_EnvironmentVariable()
        {
            static void RunTest()
            {
                bool actual = RuntimeSettingParser.QueryRuntimeSettingSwitch("Foo.Bar", "FOO_BAR", true);
                Assert.False(actual);
            }
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "false";

            await RemoteExecutor.Invoke(RunTest, options).DisposeAsync();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task QueryRuntimeSettingSwitch_InvalidValue_FallbackToDefault()
        {
            static void RunTest()
            {
                bool actual = RuntimeSettingParser.QueryRuntimeSettingSwitch("Foo.Bar", "FOO_BAR", true);
                Assert.True(actual);
            }
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "cheese";

            await RemoteExecutor.Invoke(RunTest, options).DisposeAsync();
        }

        [ConditionalTheory(nameof(SupportsRemoteExecutor))]
        [InlineData(0)]
        [InlineData(42)]
        public async Task QueryRuntimeSettingInt32_WhenNotSet_DefaultIsUsed(int defaultValue)
        {
            static void RunTest(string defaultValueStr)
            {
                int expected = int.Parse(defaultValueStr);
                int actual = RuntimeSettingParser.QueryRuntimeSettingInt32("Foo.Bar", "FOO_BAR", expected);
                Assert.Equal(expected, actual);
            }

            await RemoteExecutor.Invoke(RunTest, defaultValue.ToString()).DisposeAsync();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task QueryRuntimeSettingInt32_AppContextHasPriority()
        {
            static void RunTest()
            {
                AppContext.SetData("Foo.Bar", 1);
                int actual = RuntimeSettingParser.QueryRuntimeSettingInt32("Foo.Bar", "FOO_BAR", 2);
                Assert.Equal(1, actual);
            }
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "3";

            await RemoteExecutor.Invoke(RunTest, options).DisposeAsync();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task QueryRuntimeSettingInt32_EnvironmentVariable()
        {
            static void RunTest()
            {
                int actual = RuntimeSettingParser.QueryRuntimeSettingInt32("Foo.Bar", "FOO_BAR", 2);
                Assert.Equal(1, actual);
            }
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "1";

            await RemoteExecutor.Invoke(RunTest, options).DisposeAsync();
        }


        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task QueryRuntimeSettingInt32_InvalidValue_FallbackToDefault()
        {
            static void RunTest()
            {
                int actual = RuntimeSettingParser.QueryRuntimeSettingInt32("Foo.Bar", "FOO_BAR", 1);
                Assert.Equal(1, actual);
            }
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "cheese";

            await RemoteExecutor.Invoke(RunTest, options).DisposeAsync();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task ParseInt32EnvironmentVariableValue_WhenNotSet_DefaultIsUsed()
        {
            static void RunTest()
            {
                int actual = RuntimeSettingParser.ParseInt32EnvironmentVariableValue("FOO_BAR", -42);
                Assert.Equal(-42, actual);
            }
            await RemoteExecutor.Invoke(RunTest).DisposeAsync();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task ParseInt32EnvironmentVariableValue_ValidValue()
        {
            static void RunTest()
            {
                int actual = RuntimeSettingParser.ParseInt32EnvironmentVariableValue("FOO_BAR", -42);
                Assert.Equal(84, actual);
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "84";

            await RemoteExecutor.Invoke(RunTest, options).DisposeAsync();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task ParseInt32EnvironmentVariableValue_InvalidValue_FallbackToDefault()
        {
            static void RunTest()
            {
                int actual = RuntimeSettingParser.ParseInt32EnvironmentVariableValue("FOO_BAR", -42);
                Assert.Equal(-42, actual);
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "-~4!";

            await RemoteExecutor.Invoke(RunTest, options).DisposeAsync();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task ParseDoubleEnvironmentVariableValue_WhenNotSet_DefaultIsUsed()
        {
            static void RunTest()
            {
                double actual = RuntimeSettingParser.ParseDoubleEnvironmentVariableValue("FOO_BAR", -0.42);
                Assert.Equal(-0.42, actual);
            }
            await RemoteExecutor.Invoke(RunTest).DisposeAsync();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task ParseDoubleEnvironmentVariableValue_ValidValue()
        {
            static void RunTest()
            {
                double actual = RuntimeSettingParser.ParseDoubleEnvironmentVariableValue("FOO_BAR", -0.42);
                Assert.Equal(0.84, actual);
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "0.84";

            await RemoteExecutor.Invoke(RunTest, options).DisposeAsync();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public async Task ParseDoubleEnvironmentVariableValue_InvalidValue_FallbackToDefault()
        {
            static void RunTest()
            {
                double actual = RuntimeSettingParser.ParseDoubleEnvironmentVariableValue("FOO_BAR", -0.42);
                Assert.Equal(-0.42, actual);
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "-~4!";

            await RemoteExecutor.Invoke(RunTest, options).DisposeAsync();
        }
    }
}
