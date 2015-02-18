// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using Xunit;

public class GetEnvironmentVariable
{
    [Fact]
    public void NullVariableThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => Environment.GetEnvironmentVariable(null));
    }

    [Fact]
    public void EmptyVariableReturnsNull()
    {
        Assert.Equal(null, Environment.GetEnvironmentVariable(String.Empty));
    }

    [Fact]
    public void RandomLongVariableNameCanRoundTrip()
    {
        // NOTE: The limit of 32766 characters enforced by dekstop
        // SetEnvironmentVariable (not in the contract) is antiquated. I was
        // able to create ~1GB names and values on my Windows 8.1 box. On
        // desktop, GetEnvironmentVariable throws OOM during its attempt to
        // demand huge EnvironmentPermission well before that. Also, the old
        // test for long name case wasn't very good: it just checked that an
        // arbitrary long name > 32766 characters returned null (not found), but
        // that had nothing to do with the limit, the variable was simply not
        // found!

        string variable = "LongVariable_" + new string('@', 33000);
        const string value = "TestValue";

        try
        {
            Assert.Equal(true, SetEnvironmentVariable(variable, value));

            Assert.Equal(value, Environment.GetEnvironmentVariable(variable));
        }
        finally
        {
            Assert.Equal(true, SetEnvironmentVariable(variable, null));
        }
    }

    [Fact]
    public void RandomVariableThatDoesNotExistReturnsNull()
    {
        string variable = "TestVariable_SurelyThisDoesNotExist";
        Assert.Equal(null, Environment.GetEnvironmentVariable(variable));
    }

    [Fact]
    public void VariableNamesAreCaseInsensitive()
    {
        const string value = "TestValue";

        try
        {
            Assert.Equal(true, SetEnvironmentVariable("ThisIsATestEnvironmentVariable", value));

            Assert.Equal(value, Environment.GetEnvironmentVariable("ThisIsATestEnvironmentVariable"));
            Assert.Equal(value, Environment.GetEnvironmentVariable("thisisatestenvironmentvariable"));
            Assert.Equal(value, Environment.GetEnvironmentVariable("THISISATESTENVIRONMENTVARIABLE"));
            Assert.Equal(value, Environment.GetEnvironmentVariable("ThISISATeSTENVIRoNMEnTVaRIABLE"));
        }
        finally
        {
            Assert.Equal(true, SetEnvironmentVariable("ThisIsATestEnvironmentVariable", null));
        }
    }

    [Fact]
    public void CanGetAllVariablesIndividually()
    {
        bool atLeastOne = false;

        IDictionary envBlock = Environment.GetEnvironmentVariables();

        foreach (DictionaryEntry envEntry in envBlock)
        {
            string name = (string)envEntry.Key;
            string value = Environment.GetEnvironmentVariable(name);
            Assert.Equal(envEntry.Value, value);
            atLeastOne = true;
        }

        Assert.True(atLeastOne);
    }

    [DllImport("api-ms-win-core-processenvironment-l1-1-0.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetEnvironmentVariable(string lpName, string lpValue);
}
