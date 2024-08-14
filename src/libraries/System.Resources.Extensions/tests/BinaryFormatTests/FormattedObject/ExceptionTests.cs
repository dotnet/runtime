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
        Assert.Equal(typeof(NotSupportedException).FullName, systemClass.TypeName.FullName);
        Assert.Equal(
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
        ], systemClass.MemberNames);

        Assert.Equal("System.NotSupportedException", systemClass.GetString("ClassName"));
        Assert.Equal("Specified method is not supported.", systemClass.GetString("Message"));
        Assert.Null(systemClass.GetRawValue("Data"));
        Assert.Null(systemClass.GetRawValue("InnerException"));
        Assert.Null(systemClass.GetRawValue("HelpURL"));
        Assert.Null(systemClass.GetRawValue("StackTraceString"));
        Assert.Null(systemClass.GetRawValue("RemoteStackTraceString"));
        Assert.Equal(0, systemClass.GetInt32("RemoteStackIndex"));
        Assert.Null(systemClass.GetRawValue("ExceptionMethod"));
        Assert.Equal(unchecked((int)0x80131515), systemClass.GetInt32("HResult"));
        Assert.Null(systemClass.GetRawValue("Source"));
        Assert.Null(systemClass.GetRawValue("WatsonBuckets"));
    }
}
