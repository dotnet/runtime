// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace FormatTests.FormattedObject;

public class CorruptedTests : Common.CorruptedTests<FormattedObjectSerializer>
{
    public override void ValueTypeReferencesSelf2()
    {
        Action action = base.ValueTypeReferencesSelf2;
        action.Should().Throw<SerializationException>();
    }

    public override void ValueTypeReferencesSelf3()
    {
        Action action = base.ValueTypeReferencesSelf3;
        action.Should().Throw<SerializationException>();
    }
}
