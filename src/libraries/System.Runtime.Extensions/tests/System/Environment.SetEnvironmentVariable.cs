// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
public class SetEnvironmentVariable
{
    private const int MAX_VAR_LENGTH_ALLOWED = 32767;

    [Fact]
    public void NullVariableThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => Environment.SetEnvironmentVariable(null, "foo"));
    }

    [Fact]
    public void IncorrectVariableThrowsArgument()
    {
        Assert.Throws<ArgumentException>(() => Environment.SetEnvironmentVariable(String.Empty, "foo"));
        Assert.Throws<ArgumentException>(() => Environment.SetEnvironmentVariable('\x00'.ToString(), "foo"));
        Assert.Throws<ArgumentException>(() => Environment.SetEnvironmentVariable("Variable=Something", "foo"));

        string varWithLenLongerThanAllowed = new string('c', MAX_VAR_LENGTH_ALLOWED + 1);
        Assert.Throws<ArgumentException>(() => Environment.SetEnvironmentVariable(varWithLenLongerThanAllowed, "foo"));
    }

    [Fact]
    public void SetEnvironmentVariable_Default()
    {
        const string varName = "Test_SetEnvironmentVariable_Default";
        string value = "true";
        Environment.SetEnvironmentVariable(varName, value);

        // Check whether the variable exists.
        Assert.Equal(Environment.GetEnvironmentVariable(varName), value);

        // Clean the value.
        Environment.SetEnvironmentVariable(varName, null);
    }

    [Fact]
    public void ModifyEnvironmentVariable()
    {
        string varName = "Test_ModifyEnvironmentVariable";
        string value = "false";

        // First set the value to something and then change it and ensure that it gets modified.
        Environment.SetEnvironmentVariable(varName, "true");
        Environment.SetEnvironmentVariable(varName, value);

        // Check whether the variable exists.
        Assert.Equal(Environment.GetEnvironmentVariable(varName), value);

        // Clean the value.
        Environment.SetEnvironmentVariable(varName, null);
    }

    [Fact]
    public void DeleteEnvironmentVariable()
    {
        string varName = "Test_DeleteEnvironmentVariable";
        string value = "false";

        // First set the value to something and then change it and ensure that it gets modified.
        Environment.SetEnvironmentVariable(varName, value);
        Environment.SetEnvironmentVariable(varName, String.Empty);

        // Check whether the variable exists.
        Assert.Equal(Environment.GetEnvironmentVariable(varName), null);

        Environment.SetEnvironmentVariable(varName, value);
        Environment.SetEnvironmentVariable(varName, null);
        Assert.Equal(Environment.GetEnvironmentVariable(varName), null);

        Environment.SetEnvironmentVariable(varName, value);
        Environment.SetEnvironmentVariable(varName, '\u0000'.ToString());
        Assert.Equal(Environment.GetEnvironmentVariable(varName), null);

        // Check that the varName with non-initial zero characters work during deleting.
        string varName_initial = "Begin_DeleteEnvironmentVariable";
        string varName_end = "End_DeleteEnvironmentVariable";
        string hexDecimal = '\u0000'.ToString();

        varName = varName_initial + hexDecimal + varName_end;
        Environment.SetEnvironmentVariable(varName, "true");
        Environment.SetEnvironmentVariable(varName, String.Empty);
        Assert.Equal(Environment.GetEnvironmentVariable(varName), null);
        Assert.Equal(Environment.GetEnvironmentVariable(varName_initial), null);

        //Make sure we remove the environmentVariables.
        Environment.SetEnvironmentVariable(varName, String.Empty);

        //Check that the varValue with non-initial zero characters also work during deleting.
        hexDecimal = '\u0000'.ToString();
        value = hexDecimal + "Foo";
        varName = "Test_DeleteEnvironmentVariable1";
        Environment.SetEnvironmentVariable(varName, value);
        Assert.Equal(Environment.GetEnvironmentVariable(varName), null);
        Environment.SetEnvironmentVariable(varName, String.Empty);
    }

    [Fact]
    public void TestNonInitialZeroCharacterInVariableName()
    {
        string varName_initial = "Begin";
        string varName_end = "End";
        string hexDecimal = '\u0000'.ToString();

        string varName = varName_initial + hexDecimal + varName_end;
        string value = "true";

        Environment.SetEnvironmentVariable(varName, value);
        Assert.Equal(Environment.GetEnvironmentVariable(varName_initial), "true");

        Environment.SetEnvironmentVariable(varName, String.Empty);
        Environment.SetEnvironmentVariable(varName_initial, String.Empty);
    }

    [Fact]
    public void TestNonInitialZeroCharacterInValue()
    {
        string varName = "Test_TestNonInitialZeroCharacterInValue";
        string value_initial = "Begin";
        string value_end = "End";
        string hexDecimal = '\u0000'.ToString();

        string value = value_initial + hexDecimal + value_end;
        Environment.SetEnvironmentVariable(varName, value);
        Assert.Equal(Environment.GetEnvironmentVariable(varName), value_initial);

        Environment.SetEnvironmentVariable(varName, String.Empty);
    }

    [Fact]
    public void TestDeletingNonExistingEnvironmentVariable()
    {
        string varName = "Test_TestDeletingNonExistingEnvironmentVariable";

        if (Environment.GetEnvironmentVariable(varName) != null)
        {
            Environment.SetEnvironmentVariable(varName, null);
        }

        try
        {
            Environment.SetEnvironmentVariable("TestDeletingNonExistingEnvironmentVariable", String.Empty);
        }
        catch (Exception ex)
        {
            Assert.True(false, "TestDeletingNonExistingEnvironmentVariable failed: " + ex);
        }
    }
}