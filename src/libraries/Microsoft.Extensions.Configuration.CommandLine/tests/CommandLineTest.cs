// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration.Test;
using Xunit;

namespace Microsoft.Extensions.Configuration.CommandLine.Test
{
    public class CommandLineTest
    {
        [Fact]
        public void CanLoadSinglePair()
        {
            var args = new string[]
            {
                "--Key1=Value1"
            };
            var cmdLineConfig = new CommandLineConfigurationProvider(args);

            cmdLineConfig.Load();

            Assert.Equal("Value1", cmdLineConfig.Get("Key1"));
            Assert.Single(cmdLineConfig.GetChildKeys(new string[0], null));
        }

        [Fact]
        public void CanLoadSingleSwitch()
        {
            var args = new string[]
            {
                "--Key1"
            };
            var cmdLineConfig = new CommandLineConfigurationProvider(args);

            cmdLineConfig.Load();

            Assert.Equal("true", cmdLineConfig.Get("Key1"));
            Assert.Single(cmdLineConfig.GetChildKeys(new string[0], null));
        }

        [Fact]
        public void IgnoresOnlyUnknownArgs()
        {
            var args = new string[]
                {
                    "foo",
                    "/bar=baz"
                };
            var cmdLineConfig = new CommandLineConfigurationProvider(args);
            cmdLineConfig.Load();
            Assert.Single(cmdLineConfig.GetChildKeys(new string[0], null));
            Assert.Equal("baz", cmdLineConfig.Get("bar"));
        }

        [Fact]
        public void CanIgnoreValuesInMiddle()
        {
            var args = new string[]
                {
                    "Key1=Value1",
                    "--Key2=Value2",
                    "/Key3=Value3",
                    "Bogus1",
                    "--Key4", "Value4",
                    "Bogus2",
                    "/Key5", "Value5",
                    "Bogus3",
                    "-Key6",
                    "--Key7",
                    "/Key8"
                };
            var cmdLineConfig = new CommandLineConfigurationProvider(args);

            cmdLineConfig.Load();

            Assert.Equal("Value1", cmdLineConfig.Get("Key1"));
            Assert.Equal("Value2", cmdLineConfig.Get("Key2"));
            Assert.Equal("Value3", cmdLineConfig.Get("Key3"));
            Assert.Equal("Value4", cmdLineConfig.Get("Key4"));
            Assert.Equal("Value5", cmdLineConfig.Get("Key5"));
            Assert.Equal("true", cmdLineConfig.Get("Key6"));
            Assert.Equal("true", cmdLineConfig.Get("Key7"));
            Assert.Equal("true", cmdLineConfig.Get("Key8"));
            Assert.Equal(8, cmdLineConfig.GetChildKeys(new string[0], null).Count());
        }

        [Fact]
        public void CanEvalatePairsAfterSwitchesWithNoValues()
        {
            var args = new string[]
            {
                "-Key1",
                "Key2=Value2",
                "--Key3",
                "--Key4=Value4",
                "/Key5",
                "/Key6=Value6",
                "/Key7", "Value7",
            };
            var cmdLineConfig = new CommandLineConfigurationProvider(args);

            cmdLineConfig.Load();

            Assert.Equal("true", cmdLineConfig.Get("Key1"));
            Assert.Equal("Value2", cmdLineConfig.Get("Key2"));
            Assert.Equal("true", cmdLineConfig.Get("Key3"));
            Assert.Equal("Value4", cmdLineConfig.Get("Key4"));
            Assert.Equal("true", cmdLineConfig.Get("Key5"));
            Assert.Equal("Value6", cmdLineConfig.Get("Key6"));
            Assert.Equal("Value7", cmdLineConfig.Get("Key7"));
            Assert.Equal(7, cmdLineConfig.GetChildKeys(new string[0], null).Count());
        }

        [Fact]
        public void CanEvalatePairsWithLinuxPathValues()
        {
            var args = new string[]
            {
                "-Key1",
                "/Key2", "/path/to/value2/in/linux",
                "--Key3",
                "--Key4=Value4",
                "/Key5",
                "/Key6=Value6",
                "/Key7", "Value7",
            };
            var cmdLineConfig = new CommandLineConfigurationProvider(args);

            cmdLineConfig.Load();

            Assert.Equal("true", cmdLineConfig.Get("Key1"));
            Assert.Equal("/path/to/value2/in/linux", cmdLineConfig.Get("Key2"));
            Assert.Equal("true", cmdLineConfig.Get("Key3"));
            Assert.Equal("Value4", cmdLineConfig.Get("Key4"));
            Assert.Equal("true", cmdLineConfig.Get("Key5"));
            Assert.Equal("Value6", cmdLineConfig.Get("Key6"));
            Assert.Equal("Value7", cmdLineConfig.Get("Key7"));
            Assert.Equal(7, cmdLineConfig.GetChildKeys(new string[0], null).Count());
        }

        [Fact]
        public void LoadKeyValuePairsFromCommandLineArgumentsWithoutSwitchMappings()
        {
            var args = new string[]
                {
                    "Key1=Value1",
                    "--Key2=Value2",
                    "/Key3=Value3",
                    "--Key4", "Value4",
                    "/Key5", "Value5",
                    "-Key6",
                    "--Key7",
                    "/Key8"
                };
            var cmdLineConfig = new CommandLineConfigurationProvider(args);

            cmdLineConfig.Load();

            Assert.Equal("Value1", cmdLineConfig.Get("Key1"));
            Assert.Equal("Value2", cmdLineConfig.Get("Key2"));
            Assert.Equal("Value3", cmdLineConfig.Get("Key3"));
            Assert.Equal("Value4", cmdLineConfig.Get("Key4"));
            Assert.Equal("Value5", cmdLineConfig.Get("Key5"));
            Assert.Equal("true", cmdLineConfig.Get("Key6"));
            Assert.Equal("true", cmdLineConfig.Get("Key7"));
            Assert.Equal("true", cmdLineConfig.Get("Key8"));
        }

        [Fact]
        public void LoadKeyValuePairsFromCommandLineArgumentsWithSwitchMappings()
        {
            var args = new string[]
                {
                    "-K1=Value1",
                    "--Key2=Value2",
                    "/Key3=Value3",
                    "--Key4", "Value4",
                    "/Key5", "Value5",
                    "/Key6=Value6",
                    "/Key7"
                };
            var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "-K1", "LongKey1" },
                    { "--Key2", "SuperLongKey2" },
                    { "--Key6", "SuchALongKey6"},
                    { "--Key7", "BoyWhatAKey7"}
                };
            var cmdLineConfig = new CommandLineConfigurationProvider(args, switchMappings);

            cmdLineConfig.Load();

            Assert.Equal("Value1", cmdLineConfig.Get("LongKey1"));
            Assert.Equal("Value2", cmdLineConfig.Get("SuperLongKey2"));
            Assert.Equal("Value3", cmdLineConfig.Get("Key3"));
            Assert.Equal("Value4", cmdLineConfig.Get("Key4"));
            Assert.Equal("Value5", cmdLineConfig.Get("Key5"));
            Assert.Equal("Value6", cmdLineConfig.Get("SuchALongKey6"));
            Assert.Equal("true", cmdLineConfig.Get("BoyWhatAKey7"));
        }

        [Fact]
        public void ThrowExceptionWhenPassingSwitchMappingsWithDuplicatedKeys()
        {
            // Arrange
            var args = new string[]
                {
                    "-K1=Value1",
                    "--Key2=Value2",
                    "/Key3=Value3",
                    "--Key4", "Value4",
                    "/Key5", "Value5"
                };
            var switchMappings = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "--KEY1", "LongKey1" },
                    { "--key1", "SuperLongKey1" },
                    { "-Key2", "LongKey2" },
                    { "-KEY2", "LongKey2"}
                };

            // Find out the duplicate expected be be reported
            var expectedDup = string.Empty;
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mapping in switchMappings)
            {
                if (set.Contains(mapping.Key))
                {
                    expectedDup = mapping.Key;
                    break;
                }

                set.Add(mapping.Key);
            }

            var expectedMsg = new ArgumentException(SR.
                Format(SR.Error_DuplicatedKeyInSwitchMappings, expectedDup), "switchMappings").Message;

            // Act
            var exception = Assert.Throws<ArgumentException>(
                () => new CommandLineConfigurationProvider(args, switchMappings));

            // Assert
            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenSwitchMappingsContainInvalidKey()
        {
            var args = new string[]
                {
                    "-K1=Value1",
                    "--Key2=Value2",
                    "/Key3=Value3",
                    "--Key4", "Value4",
                    "/Key5", "Value5"
                };
            var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "-K1", "LongKey1" },
                    { "--Key2", "SuperLongKey2" },
                    { "/Key3", "AnotherSuperLongKey3" }
                };
            var expectedMsg = new ArgumentException(SR.Format(SR.Error_InvalidSwitchMapping,"/Key3"),
                "switchMappings").Message;

            var exception = Assert.Throws<ArgumentException>(
                () => new CommandLineConfigurationProvider(args, switchMappings));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void ThrowExceptionWhenNullIsPassedToConstructorAsArgs()
        {
            string[] args = null;
            var expectedMsg = new ArgumentNullException("args").Message;

            var exception = Assert.Throws<ArgumentNullException>(() => new CommandLineConfigurationProvider(args));

            Assert.Equal(expectedMsg, exception.Message);
        }

        [Fact]
        public void OverrideValueWhenKeyIsDuplicated()
        {
            var args = new string[]
                {
                    "/Key1=Value1",
                    "--Key1=Value2",
                    "-Key2", // As switch, evaluates "true"
                    "/Key2=false" // Should be overridden to "false" here
                };
            var cmdLineConfig = new CommandLineConfigurationProvider(args);

            cmdLineConfig.Load();

            Assert.Equal("Value2", cmdLineConfig.Get("Key1"));
            Assert.Equal("false", cmdLineConfig.Get("Key2"));
        }

        [Fact]
        public void EvaluateAsTrueWhenValueForAKeyIsMissing()
        {
            var args = new string[]
            {
                "--Key1", "Value1",
                "-Key2", /* The value for Key2 is missing here, so treat as a switch */
                "--Key3", /* The value for Key2 is missing here, so treat as a switch */
                "/Key4"   /* The value for Key3 is missing here, so treat as a switch */
            };

            var cmdLineConfig = new CommandLineConfigurationProvider(args);
            cmdLineConfig.Load();
            Assert.Equal(4, cmdLineConfig.GetChildKeys(new string[0], null).Count());
            Assert.Equal("Value1", cmdLineConfig.Get("Key1"));
            Assert.Equal("true", cmdLineConfig.Get("Key2"));
            Assert.Equal("true", cmdLineConfig.Get("Key3"));
            Assert.Equal("true", cmdLineConfig.Get("Key4"));
        }

        [Fact]
        public void IgnoreWhenAnArgumentCannotBeRecognized()
        {
            var args = new string[]
            {
                "ArgWithoutPrefixAndEqualSign"
            };
            var cmdLineConfig = new CommandLineConfigurationProvider(args);
            cmdLineConfig.Load();
            Assert.Empty(cmdLineConfig.GetChildKeys(new string[0], null));
        }

        [Fact]
        public void IgnoreWhenShortSwitchNotDefined()
        {
            var args = new string[]
            {
                "-Key1", "Value1",
            };
            var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "-Key2", "LongKey2" }
            };
            var cmdLineConfig = new CommandLineConfigurationProvider(args, switchMappings);
            cmdLineConfig.Load();
            Assert.Empty(cmdLineConfig.GetChildKeys(new string[0], ""));
        }
    }
}
