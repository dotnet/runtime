// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class TupleElementNamesAttributeTests
{
    [Fact]
    public static void DefaultConstructor()
    {
        var attribute = new TupleElementNamesAttribute();
        Assert.NotNull(attribute.TransformNames);
        Assert.Equal(0, attribute.TransformNames.Count);
    }

    [Fact]
    public static void Constructor()
    {
        var attribute = new TupleElementNamesAttribute(new string[] { "name1", "name2" });
        Assert.NotNull(attribute.TransformNames);
        Assert.Equal(new string[] { "name1", "name2" }, attribute.TransformNames);

        Assert.Throws<ArgumentNullException>(() => new TupleElementNamesAttribute(null));
    }

    [TupleElementNames]
    public object appliedToField = null;

    [TupleElementNames(new string[] { null, "name1", "name2" })]
    public object appliedToField2 = null;

    public static void AppliedToParameter([TupleElementNames] object parameter, [TupleElementNames(new string[] { "name1", null })] object parameter2) { }

    [TupleElementNames]
    public static object AppliedToProperty { get; set; }

    [TupleElementNames(new string[] { null, "name1", "name2" })]
    public static object AppliedToProperty2 { get; set; }

    [return: TupleElementNames]
    public static void AppliedToReturn()
    {
    }

    [TupleElementNames]
    public class AppliedToClass { }
    
    [TupleElementNames(new string[] { null, "name1", "name2" })]
    public class AppliedToClass2 { }

    [TupleElementNames]
    public struct AppliedToStruct { }
    
    [TupleElementNames(new string[] { null, "name1", "name2" })]
    public class AppliedToStruct2 { }
}
