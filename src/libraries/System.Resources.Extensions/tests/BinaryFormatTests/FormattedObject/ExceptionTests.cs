// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Resources.Extensions.BinaryFormat;
using System.Formats.Nrbf;
using System.Resources.Extensions.Tests.Common;

namespace System.Resources.Extensions.Tests.FormattedObject;

public class ExceptionTests : SerializationTest<FormattedObjectSerializer>
{
    [Fact]
    public void NotSupportedException_Parse()
    {
        BinaryFormattedObject format = new(Serialize(new NotSupportedException()));
        var systemClass = (ClassRecord)format.RootRecord;
        systemClass.TypeName.FullName.Should().Be(typeof(NotSupportedException).FullName);
        systemClass.MemberNames.Should().BeEquivalentTo(
        [
            "ClassName",
            "Message",
            "Data",
            "InnerException",
            "HelpURL",
            "StackTraceString",
            "RemoteStackTraceString",
            "RemoteStackIndex",
            "ExceptionMethod",
            "HResult",
            "Source",
            "WatsonBuckets"
        ]);

        systemClass.GetString("ClassName").Should().Be("System.NotSupportedException");
        systemClass.GetString("Message").Should().Be("Specified method is not supported.");
        systemClass.GetRawValue("Data").Should().BeNull();
        systemClass.GetRawValue("InnerException").Should().BeNull();
        systemClass.GetRawValue("HelpURL").Should().BeNull();
        systemClass.GetRawValue("StackTraceString").Should().BeNull();
        systemClass.GetRawValue("RemoteStackTraceString").Should().BeNull();
        systemClass.GetInt32("RemoteStackIndex").Should().Be(0);
        systemClass.GetRawValue("ExceptionMethod").Should().BeNull();
        systemClass.GetInt32("HResult").Should().Be(unchecked((int)0x80131515));
        systemClass.GetRawValue("Source").Should().BeNull();
        systemClass.GetRawValue("WatsonBuckets").Should().BeNull();
    }
}
