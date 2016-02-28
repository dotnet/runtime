// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public class SetEnvironmentVariable
    {
        private const int MAX_VAR_LENGTH_ALLOWED = 32767;
        private const string NullString = "\u0000";

        [Fact]
        public void NullVariableThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => Environment.SetEnvironmentVariable(null, "test"));
        }

        [Fact]
        public void IncorrectVariableThrowsArgument()
        {
            Assert.Throws<ArgumentException>(() => Environment.SetEnvironmentVariable(String.Empty, "test"));
            Assert.Throws<ArgumentException>(() => Environment.SetEnvironmentVariable(NullString, "test"));
            Assert.Throws<ArgumentException>(() => Environment.SetEnvironmentVariable("Variable=Something", "test"));

            string varWithLenLongerThanAllowed = new string('c', MAX_VAR_LENGTH_ALLOWED + 1);
            Assert.Throws<ArgumentException>(() => Environment.SetEnvironmentVariable(varWithLenLongerThanAllowed, "test"));
        }

        [Fact]
        public void Default()
        {
            const string varName = "Test_SetEnvironmentVariable_Default";
            const string value = "true";

            try
            {
                Environment.SetEnvironmentVariable(varName, value);
                Assert.Equal(value, Environment.GetEnvironmentVariable(varName));
            }
            finally
            {
                // Clear the test variable
                Environment.SetEnvironmentVariable(varName, null);
            }
        }

        [Fact]
        public void ModifyEnvironmentVariable()
        {
            const string varName = "Test_ModifyEnvironmentVariable";
            const string value = "false";

            try
            {
                // First set the value to something and then change it and ensure that it gets modified.
                Environment.SetEnvironmentVariable(varName, "true");
                Environment.SetEnvironmentVariable(varName, value);

                // Check whether the variable exists.
                Assert.Equal(value, Environment.GetEnvironmentVariable(varName));
            }
            finally
            {
                // Clear the test variable
                Environment.SetEnvironmentVariable(varName, null);
            }
        }

        [Fact]
        public void DeleteEnvironmentVariable()
        {
            const string varName = "Test_DeleteEnvironmentVariable";
            const string value = "false";

            // First set the value to something and then ensure that it can be deleted.
            Environment.SetEnvironmentVariable(varName, value);
            Environment.SetEnvironmentVariable(varName, String.Empty);
            Assert.Equal(null, Environment.GetEnvironmentVariable(varName));

            Environment.SetEnvironmentVariable(varName, value);
            Environment.SetEnvironmentVariable(varName, null);
            Assert.Equal(null, Environment.GetEnvironmentVariable(varName));

            Environment.SetEnvironmentVariable(varName, value);
            Environment.SetEnvironmentVariable(varName, NullString);
            Assert.Equal(null, Environment.GetEnvironmentVariable(varName));
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
                Assert.Equal(Environment.GetEnvironmentVariable(varName), null);
                Assert.Equal(Environment.GetEnvironmentVariable(varNamePrefix), null);
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
                Assert.Equal(null, Environment.GetEnvironmentVariable(varName));
            }
            finally
            {
                Environment.SetEnvironmentVariable(varName, String.Empty);
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
                Assert.Equal(value, Environment.GetEnvironmentVariable(varNamePrefix));
            }
            finally
            {
                Environment.SetEnvironmentVariable(varName, String.Empty);
                Environment.SetEnvironmentVariable(varNamePrefix, String.Empty);
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
                Environment.SetEnvironmentVariable(varName, String.Empty);
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

            Environment.SetEnvironmentVariable("TestDeletingNonExistingEnvironmentVariable", String.Empty);
        }
    }
}
