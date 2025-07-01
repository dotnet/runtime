// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Resources.Extensions.BinaryFormat;
using System.Formats.Nrbf;

namespace System.Resources.Extensions.Tests.FormattedObject;

public class SystemDrawingTests : Common.SystemDrawingTests<FormattedObjectSerializer>
{
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDrawingSupported))]
    public void PointF_Parse()
    {
        PointF input = new() { X = 123.5f, Y = 456.1f };
        BinaryFormattedObject format = new(Serialize(input));

        ClassRecord classInfo = (ClassRecord)format.RootRecord;
        Assert.Equal(SerializationRecordType.ClassWithMembersAndTypes, classInfo.RecordType);
        Assert.NotEqual(default, classInfo.Id);
        Assert.Same(classInfo, format[format.RootRecord.Id]);
        Assert.Equal("System.Drawing.PointF", classInfo.TypeName.FullName);
        Assert.Equal("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", classInfo.TypeName.AssemblyName!.FullName);
        Assert.Equal(["x", "y"], classInfo.MemberNames);
        Assert.Equal(input.X, classInfo.GetSingle("x"));
        Assert.Equal(input.Y, classInfo.GetSingle("y"));
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDrawingSupported))]
    public void RectangleF_Parse()
    {
        RectangleF input = new(x: 123.5f, y: 456.1f, width: 100.25f, height: 200.75f);
        BinaryFormattedObject format = new(Serialize(input));

        ClassRecord classInfo = (ClassRecord)format.RootRecord;
        Assert.Equal(SerializationRecordType.ClassWithMembersAndTypes, classInfo.RecordType);
        Assert.NotEqual(default, classInfo.Id);
        Assert.Same(classInfo, format[format.RootRecord.Id]);
        Assert.Equal("System.Drawing.RectangleF", classInfo.TypeName.FullName);
        Assert.Equal(["x", "y", "width", "height"], classInfo.MemberNames);
        Assert.Equal(input.X, classInfo.GetSingle("x"));
        Assert.Equal(input.Y, classInfo.GetSingle("y"));
        Assert.Equal(input.Width, classInfo.GetSingle("width"));
        Assert.Equal(input.Height, classInfo.GetSingle("height"));
    }

    public static TheoryData<object> SystemDrawing_TestData => new()
    {
        new PointF(),
        new RectangleF()
    };
}
