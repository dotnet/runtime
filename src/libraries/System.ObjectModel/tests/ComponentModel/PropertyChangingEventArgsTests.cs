// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
