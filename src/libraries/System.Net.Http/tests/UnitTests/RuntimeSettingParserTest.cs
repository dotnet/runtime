// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public void QueryRuntimeSettingSwitch_WhenNotSet_DefaultIsUsed(bool defaultValue)
        {
            static void RunTest(string defaultValueStr)
            {
                bool expected = bool.Parse(defaultValueStr);
                bool actual = RuntimeSettingParser.QueryRuntimeSettingSwitch("Foo.Bar", "FOO_BAR", expected);
                Assert.Equal(expected, actual);
            }

            RemoteExecutor.Invoke(RunTest, defaultValue.ToString()).Dispose();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public void QueryRuntimeSettingSwitch_AppContextHasPriority()
        {
            static void RunTest()
            {
                AppContext.SetSwitch("Foo.Bar", false);
                bool actual = RuntimeSettingParser.QueryRuntimeSettingSwitch("Foo.Bar", "FOO_BAR", true);
                Assert.False(actual);
            }
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "true";
            
            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public void QueryRuntimeSettingSwitch_EnvironmentVariable()
        {
            static void RunTest()
            {
                bool actual = RuntimeSettingParser.QueryRuntimeSettingSwitch("Foo.Bar", "FOO_BAR", true);
                Assert.False(actual);
            }
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "false";

            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public void QueryRuntimeSettingSwitch_InvalidValue_FallbackToDefault()
        {
            static void RunTest()
            {
                bool actual = RuntimeSettingParser.QueryRuntimeSettingSwitch("Foo.Bar", "FOO_BAR", true);
                Assert.True(actual);
            }
            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "cheese";

            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public void ParseInt32EnvironmentVariableValue_WhenNotSet_DefaultIsUsed()
        {
            static void RunTest()
            {
                int actual = RuntimeSettingParser.ParseInt32EnvironmentVariableValue("FOO_BAR", -42);
                Assert.Equal(-42, actual);
            }
            RemoteExecutor.Invoke(RunTest).Dispose();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public void ParseInt32EnvironmentVariableValue_ValidValue()
        {
            static void RunTest()
            {
                int actual = RuntimeSettingParser.ParseInt32EnvironmentVariableValue("FOO_BAR", -42);
                Assert.Equal(84, actual);
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "84";

            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public void ParseInt32EnvironmentVariableValue_InvalidValue_FallbackToDefault()
        {
            static void RunTest()
            {
                int actual = RuntimeSettingParser.ParseInt32EnvironmentVariableValue("FOO_BAR", -42);
                Assert.Equal(-42, actual);
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "-~4!";

            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public void ParseDoubleEnvironmentVariableValue_WhenNotSet_DefaultIsUsed()
        {
            static void RunTest()
            {
                double actual = RuntimeSettingParser.ParseDoubleEnvironmentVariableValue("FOO_BAR", -0.42);
                Assert.Equal(-0.42, actual);
            }
            RemoteExecutor.Invoke(RunTest).Dispose();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public void ParseDoubleEnvironmentVariableValue_ValidValue()
        {
            static void RunTest()
            {
                double actual = RuntimeSettingParser.ParseDoubleEnvironmentVariableValue("FOO_BAR", -0.42);
                Assert.Equal(0.84, actual);
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "0.84";

            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }

        [ConditionalFact(nameof(SupportsRemoteExecutor))]
        public void ParseDoubleEnvironmentVariableValue_InvalidValue_FallbackToDefault()
        {
            static void RunTest()
            {
                double actual = RuntimeSettingParser.ParseDoubleEnvironmentVariableValue("FOO_BAR", -0.42);
                Assert.Equal(-0.42, actual);
            }

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.EnvironmentVariables["FOO_BAR"] = "-~4!";

            RemoteExecutor.Invoke(RunTest, options).Dispose();
        }
    }
}
