// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Generated;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.DataGeneratorTests;

/// <summary>
/// Direct unit tests for <see cref="LayoutPair"/>'s name-cascade behavior.
/// These don't need a full <see cref="Target"/>; they exercise the field
/// selection logic through <c>HasField</c> / <c>GetFieldAddress</c>, which
/// is the same code path used by every Read/Write overload.
/// </summary>
public class LayoutPairTests
{
    private static Target.TypeInfo MakeType(uint? size, params (string Name, int Offset)[] fields)
    {
        var dict = new Dictionary<string, Target.FieldInfo>();
        foreach ((string name, int offset) in fields)
        {
            dict[name] = new Target.FieldInfo { Offset = offset };
        }
        return new Target.TypeInfo { Size = size, Fields = dict };
    }

    [Fact]
    public void NativeOnly_FindsField()
    {
        var native = MakeType(size: 16, ("State", 4));
        var layouts = new LayoutPair(native, managedType: null, managedDataOffset: 0);

        Assert.True(layouts.HasField("State"));
        Assert.Equal((TargetPointer)0x1004UL, layouts.GetFieldAddress(0x1000, "State"));
    }

    [Fact]
    public void ManagedOnly_FindsField_WithObjectHeaderOffset()
    {
        var managed = MakeType(size: 16, ("_state", 4));
        var layouts = new LayoutPair(nativeType: null, managed, managedDataOffset: 8);

        Assert.True(layouts.HasField("_state"));
        Assert.Equal((TargetPointer)(0x1000UL + 8UL + 4UL), layouts.GetFieldAddress(0x1000, "_state"));
    }

    [Fact]
    public void CrossSource_PrefersNative_WhenBothHaveField()
    {
        var native = MakeType(size: 16, ("State", 4));
        var managed = MakeType(size: 24, ("State", 12));
        var layouts = new LayoutPair(native, managed, managedDataOffset: 8);

        // Native at +4 wins over managed at +8+12.
        Assert.Equal((TargetPointer)0x1004UL, layouts.GetFieldAddress(0x1000, "State"));
    }

    [Fact]
    public void Cascade_TriesAllNamesAgainstNativeBeforeManaged()
    {
        // Native has "B" only; managed has "A". The cascade should:
        //   - native: try "A" (no), try "B" (no for A field's lookup -- but this test is just about iteration)
        //   - managed: try "A" (yes)
        // So a field declared with names ["A"] resolves via managed at addr+Header+0.
        var native = MakeType(size: 16, ("B", 8));
        var managed = MakeType(size: 8, ("A", 0));
        var layouts = new LayoutPair(native, managed, managedDataOffset: 8);

        Assert.Equal((TargetPointer)(0x1000UL + 8UL + 0UL), layouts.GetFieldAddress(0x1000, "A"));
    }

    [Fact]
    public void NameAlias_FallsBackToSecondaryNameOnSameSource()
    {
        // The runtime renamed "State" to "m_state" in some build.
        var native = MakeType(size: 16, ("m_state", 8));
        var layouts = new LayoutPair(native, managedType: null, managedDataOffset: 0);

        Assert.True(layouts.HasField(new[] { "State", "m_state" }));
        Assert.Equal((TargetPointer)(0x1000UL + 8UL), layouts.GetFieldAddress(0x1000, new[] { "State", "m_state" }));
    }

    [Fact]
    public void NameList_CascadesAllNativeFirstThenAllManaged()
    {
        // Native has "A_old"; managed has "_a". With names ["A", "A_old", "_a"]:
        // native cascade tries "A" (no), "A_old" (yes) -> resolves natively, NOT managed.
        var native = MakeType(size: 16, ("A_old", 4));
        var managed = MakeType(size: 8, ("_a", 0));
        var layouts = new LayoutPair(native, managed, managedDataOffset: 8);

        Assert.Equal((TargetPointer)(0x1000UL + 4UL),
            layouts.GetFieldAddress(0x1000, new[] { "A", "A_old", "_a" }));
    }

    [Fact]
    public void NoMatch_ThrowsOnRead()
    {
        var native = MakeType(size: 16, ("Other", 0));
        var managed = MakeType(size: 24, ("_other", 0));
        var layouts = new LayoutPair(native, managed, managedDataOffset: 8);

        Assert.False(layouts.HasField("State"));
        Assert.Throws<System.InvalidOperationException>(
            () => layouts.GetFieldAddress(0x1000, "State"));
    }

    [Fact]
    public void InstanceSize_PrefersNative()
    {
        var native = MakeType(size: 16);
        var managed = MakeType(size: 24);
        var layouts = new LayoutPair(native, managed, managedDataOffset: 8);

        Assert.Equal(16UL, layouts.InstanceSize);
    }

    [Fact]
    public void InstanceSize_FallsBackToManaged_WhenNativeAbsent()
    {
        var managed = MakeType(size: 24);
        var layouts = new LayoutPair(nativeType: null, managed, managedDataOffset: 8);

        Assert.Equal(24UL, layouts.InstanceSize);
    }

    [Fact]
    public void ValueType_HasZeroManagedDataOffset()
    {
        var managed = MakeType(size: 8, ("HashCode", 0), ("Next", 4));
        var layouts = new LayoutPair(nativeType: null, managed, managedDataOffset: 0);

        Assert.Equal((TargetPointer)0x1000UL, layouts.GetFieldAddress(0x1000, "HashCode"));
        Assert.Equal((TargetPointer)0x1004UL, layouts.GetFieldAddress(0x1000, "Next"));
    }
}
