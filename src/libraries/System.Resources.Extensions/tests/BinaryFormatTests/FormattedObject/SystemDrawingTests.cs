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
        classInfo.RecordType.Should().Be(SerializationRecordType.ClassWithMembersAndTypes);
        classInfo.Id.Should().NotBe(default);
        format[format.RootRecord.Id].Should().Be(classInfo);
        classInfo.TypeName.FullName.Should().Be("System.Drawing.PointF");
        classInfo.TypeName.AssemblyName!.FullName.Should().Be("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
        classInfo.MemberNames.Should().BeEquivalentTo(["x", "y"]);
        classInfo.GetSingle("x").Should().Be(input.X);
        classInfo.GetSingle("y").Should().Be(input.Y);
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsDrawingSupported))]
    public void RectangleF_Parse()
    {
        RectangleF input = new(x: 123.5f, y: 456.1f, width: 100.25f, height: 200.75f);
        BinaryFormattedObject format = new(Serialize(input));

        ClassRecord classInfo = (ClassRecord)format.RootRecord;
        classInfo.RecordType.Should().Be(SerializationRecordType.ClassWithMembersAndTypes);
        classInfo.Id.Should().NotBe(default);
        format[format.RootRecord.Id].Should().Be(classInfo);
        classInfo.TypeName.FullName.Should().Be("System.Drawing.RectangleF");
        classInfo.MemberNames.Should().BeEquivalentTo(["x", "y", "width", "height"]);
        classInfo.GetSingle("x").Should().Be(input.X);
        classInfo.GetSingle("y").Should().Be(input.Y);
        classInfo.GetSingle("width").Should().Be(input.Width);
        classInfo.GetSingle("height").Should().Be(input.Height);
    }

    public static TheoryData<object> SystemDrawing_TestData => new()
    {
        new PointF(),
        new RectangleF()
    };
}
