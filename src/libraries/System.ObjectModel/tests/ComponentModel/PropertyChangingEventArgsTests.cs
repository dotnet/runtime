// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.ComponentModel;

public class PropertyChangingEventArgsTests
{
    [Fact]
    public static void Constructor_NullAsPropertyName_SetsPropertyNameToNull()
    {   
        // Null has special meaning; it means all properties have changed
        var e = new PropertyChangingEventArgs((string)null);

        Assert.Equal((string)null, e.PropertyName);
    }

    [Fact]
    public static void Constructor_NullAsPropertyName_SetsPropertyNameToEmpty()
    {   
        // Empty has special meaning; it means all properties have changed
        var e = new PropertyChangingEventArgs(string.Empty);

        Assert.Equal(string.Empty, e.PropertyName);
    }

    [Fact]
    public static void Constructor_ValueAsPropertyName_SetsPropertyNameToValue()
    {
        var inputs = new string[] { "PropertyName",
                                    "PROPERTYNAME",
                                    "propertyname" };

        foreach (string input in inputs)
        {
            var e = new PropertyChangingEventArgs(input);

            Assert.Equal(input, e.PropertyName);
        }
    }
}
