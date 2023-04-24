// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public class SetEnvironmentVariable
    {
        private const string NullString = "\u0000";

        internal static bool IsSupportedTarget(EnvironmentVariableTarget target)
        {
            if (target == EnvironmentVariableTarget.User && PlatformDetection.IsWindowsNanoServer)
            {
                // Windows Nano Server does not have full per-user registry hives
                return false;
            }

            return target == EnvironmentVariableTarget.Process || OperatingSystem.IsWindows();
        }

        [Fact]
        public void NullVariableThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => Environment.SetEnvironmentVariable(null, "test"));
        }

        [Fact]
        public void IncorrectVariableThrowsArgument()
        {
            AssertExtensions.Throws<ArgumentException>("variable", () => Environment.SetEnvironmentVariable(string.Empty, "test"));
            AssertExtensions.Throws<ArgumentException>("variable", () => Environment.SetEnvironmentVariable(NullString, "test"));
            AssertExtensions.Throws<ArgumentException>("variable", null, () => Environment.SetEnvironmentVariable("Variable=Something", "test"));
        }

        [Fact]
        public void AllowAnyVariableLengths()
        {
            // longer than 32767
            string longVar = new string('c', 40000);
            string val = "test";

            try
            {
                Environment.SetEnvironmentVariable(longVar, val);
                Assert.Equal(val, Environment.GetEnvironmentVariable(longVar));
            }
            finally
            {
                Environment.SetEnvironmentVariable(longVar, null);
            }
        }

        [Fact]
        public void AllowAnyVariableValueLengths()
        {
            string var = "Test_SetEnvironmentVariable_AllowAnyVariableValueLengths";
            // longer than 32767
            string longVal = new string('c', 40000);

            try
            {
                Environment.SetEnvironmentVariable(var, longVal);
                Assert.Equal(longVal, Environment.GetEnvironmentVariable(var));
            }
            finally
            {
                Environment.SetEnvironmentVariable(var, null);
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void EnvironmentVariableTooLarge_Throws()
        {
            RemoteExecutor.Invoke(() =>
            {
                string longVar;
                string val = "Test_SetEnvironmentVariable_EnvironmentVariableTooLarge_Throws";

                try
                {
                    // string slightly less than 2 GiB (1 GiB for x86) so the constructor doesn't fail
                    var count = (Environment.Is64BitProcess ? 1024 * 1024 * 1024 : 512 * 1024 * 1024) - 64;
                    longVar = new string('c', count);
                }
                catch (OutOfMemoryException)
                {
                    // not enough memory to allocate a string at test time
                    return RemoteExecutor.SuccessExitCode;
                }

                try
                {
                    Environment.SetEnvironmentVariable(longVar, val);
                    // no exception is ok since we cannot construct an argument long enough to break the function
                    // in that particular environment
                }
                catch (OutOfMemoryException)
                {
                    // expected
                }
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void EnvironmentVariableValueTooLarge_Throws()
        {
            RemoteExecutor.Invoke(() =>
            {
                string var = "Test_SetEnvironmentVariable_EnvironmentVariableValueTooLarge_Throws";
                string longVal;

                try
                {
                    // string slightly less than 2 GiB (1 GiB for x86) so the constructor doesn't fail
                    var count = (Environment.Is64BitProcess ? 1024 * 1024 * 1024 : 512 * 1024 * 1024) - 64;
                    longVal = new string('c', count);
                }
                catch (OutOfMemoryException)
                {
                    // not enough memory to allocate a string at test time
                    return RemoteExecutor.SuccessExitCode;
                }

                try
                {
                    Environment.SetEnvironmentVariable(var, longVal);
                    // no exception is ok since we cannot construct an argument long enough to break the function
                    // in that particular environment
                }
                catch (OutOfMemoryException)
                {
                    // expected
                }
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        private static void ExecuteAgainstTarget(
            EnvironmentVariableTarget target,
            Action action,
            Action cleanUp = null)
        {
            bool shouldCleanUp = cleanUp != null;
            try
            {
                action();
            }
            catch (SecurityException)
            {
                shouldCleanUp = false;
                Assert.True(target == EnvironmentVariableTarget.Machine, "only machine target should have access issues");
                Assert.True(PlatformDetection.IsWindows, "and it should be Windows");
                Assert.False(PlatformDetection.IsPrivilegedProcess, "and we shouldn't be elevated");
            }
            finally
            {
                if (shouldCleanUp)
                    cleanUp();
            }
        }

        [Theory]
        [MemberData(nameof(EnvironmentTests.EnvironmentVariableTargets), MemberType = typeof(EnvironmentTests))]
        public void Default(EnvironmentVariableTarget target)
        {
            string varName = $"Test_SetEnvironmentVariable_Default ({target})";
            const string value = "true";

            ExecuteAgainstTarget(target,
            () =>
            {
                Environment.SetEnvironmentVariable(varName, value, target);
                Assert.Equal(IsSupportedTarget(target) ? value : null,
                    Environment.GetEnvironmentVariable(varName, target));
            },
            () =>
            {
                // Clear the test variable
                Environment.SetEnvironmentVariable(varName, null, target);
            });
        }


        [Theory]
        [MemberData(nameof(EnvironmentTests.EnvironmentVariableTargets), MemberType = typeof(EnvironmentTests))]
        public void ModifyEnvironmentVariable(EnvironmentVariableTarget target)
        {
            string varName = $"Test_ModifyEnvironmentVariable ({target})";
            const string value = "false";

            ExecuteAgainstTarget(target,
            () =>
            {
                // First set the value to something and then change it and ensure that it gets modified.
                Environment.SetEnvironmentVariable(varName, "true", target);

                Environment.SetEnvironmentVariable(varName, value, target);

                // Check whether the variable exists.
                Assert.Equal(IsSupportedTarget(target) ? value : null, Environment.GetEnvironmentVariable(varName, target));
            },
            () =>
            {
                // Clear the test variable
                Environment.SetEnvironmentVariable(varName, null, target);
            });
        }

        [Theory]
        [MemberData(nameof(EnvironmentTests.EnvironmentVariableTargets), MemberType = typeof(EnvironmentTests))]
        public void ModifyEnvironmentVariable_AndEnumerate(EnvironmentVariableTarget target)
        {
            string varName = $"Test_ModifyEnvironmentVariable_AndEnumerate ({target})";
            const string value = "false";

            ExecuteAgainstTarget(target,
            () =>
            {
                // First set the value to something and then change it and ensure that it gets modified.
                Environment.SetEnvironmentVariable(varName, "true", target);

                // Enumerate to validate our first value to ensure we can still set after enumerating
                IDictionary variables = Environment.GetEnvironmentVariables(target);
                if (IsSupportedTarget(target))
                {
                    Assert.True(variables.Contains(varName), "has the key we entered");
                    Assert.Equal("true", variables[varName]);
                }

                Environment.SetEnvironmentVariable(varName, value, target);

                // Check whether the variable exists.
                Assert.Equal(IsSupportedTarget(target) ? value : null, Environment.GetEnvironmentVariable(varName, target));
            },
            () =>
            {
                // Clear the test variable
                Environment.SetEnvironmentVariable(varName, null, target);
            });
        }

        [Theory]
        [MemberData(nameof(EnvironmentTests.EnvironmentVariableTargets), MemberType = typeof(EnvironmentTests))]
        public void DeleteEnvironmentVariable(EnvironmentVariableTarget target)
        {
            string varName = $"Test_DeleteEnvironmentVariable ({target})";
            const string value = "false";

            ExecuteAgainstTarget(target,
            () =>
            {
                // First set the value to something and then ensure that it can be deleted.
                Environment.SetEnvironmentVariable(varName, value);
                Environment.SetEnvironmentVariable(varName, string.Empty);
                Assert.Null(Environment.GetEnvironmentVariable(varName));

                Environment.SetEnvironmentVariable(varName, value);
                Environment.SetEnvironmentVariable(varName, null);
                Assert.Null(Environment.GetEnvironmentVariable(varName));

                Environment.SetEnvironmentVariable(varName, value);
                Environment.SetEnvironmentVariable(varName, NullString);
                Assert.Null(Environment.GetEnvironmentVariable(varName));
            });
        }

        [Fact]
        public void DeleteEnvironmentVariableNonInitialNullInName()
        {
            const string varNamePrefix = "Begin_DeleteEnvironmentVariableNonInitialNullInName";
            const string varNameSuffix = "End_DeleteEnvironmentVariableNonInitialNullInName";
            const string varName = varNamePrefix + NullString + varNameSuffix;
            const string value = "false";

            try
            {
                Environment.SetEnvironmentVariable(varName, value);
                Environment.SetEnvironmentVariable(varName, null);
                Assert.Null(Environment.GetEnvironmentVariable(varName));
                Assert.Null(Environment.GetEnvironmentVariable(varNamePrefix));
            }
            finally
            {
                // Clear the test variable
                Environment.SetEnvironmentVariable(varName, null);
            }
        }

        [Fact]
        public void DeleteEnvironmentVariableInitialNullInValue()
        {
            const string value = NullString + "test";
            const string varName = "DeleteEnvironmentVariableInitialNullInValue";

            try
            {
                Environment.SetEnvironmentVariable(varName, value);
                Assert.Null(Environment.GetEnvironmentVariable(varName));
            }
            finally
            {
                Environment.SetEnvironmentVariable(varName, string.Empty);
            }
        }

        [Fact]
        public void NonInitialNullCharacterInVariableName()
        {
            const string varNamePrefix = "NonInitialNullCharacterInVariableName_Begin";
            const string varNameSuffix = "NonInitialNullCharacterInVariableName_End";
            const string varName = varNamePrefix + NullString + varNameSuffix;
            const string value = "true";

            try
            {
                Environment.SetEnvironmentVariable(varName, value);
                Assert.Equal(value, Environment.GetEnvironmentVariable(varName));
                Assert.Equal(value, Environment.GetEnvironmentVariable(varNamePrefix));
            }
            finally
            {
                Environment.SetEnvironmentVariable(varName, string.Empty);
                Environment.SetEnvironmentVariable(varNamePrefix, string.Empty);
            }
        }

        [Fact]
        public void NonInitialNullCharacterInValue()
        {
            const string varName = "Test_TestNonInitialZeroCharacterInValue";
            const string valuePrefix = "Begin";
            const string valueSuffix = "End";
            const string value = valuePrefix + NullString + valueSuffix;

            try
            {
                Environment.SetEnvironmentVariable(varName, value);
                Assert.Equal(valuePrefix, Environment.GetEnvironmentVariable(varName));
            }
            finally
            {
                Environment.SetEnvironmentVariable(varName, string.Empty);
            }
        }

        [Fact]
        public void DeleteNonExistentEnvironmentVariable()
        {
            const string varName = "Test_TestDeletingNonExistingEnvironmentVariable";

            if (Environment.GetEnvironmentVariable(varName) != null)
            {
                Environment.SetEnvironmentVariable(varName, null);
            }

            Environment.SetEnvironmentVariable("TestDeletingNonExistingEnvironmentVariable", string.Empty);
        }
    }
}
