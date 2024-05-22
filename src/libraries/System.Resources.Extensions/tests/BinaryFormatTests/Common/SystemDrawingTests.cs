// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Versioning;

namespace FormatTests.Common;

public abstract class SystemDrawingTests<T> : SerializationTest<T> where T : ISerializer
{
    [Theory]
    [MemberData(nameof(FormatterOptions))]
    [SupportedOSPlatform("windows")]
    public void Bitmap_RoundTrip(FormatterTypeStyle typeStyle, FormatterAssemblyStyle assemblyMatching)
    {
        using Bitmap bitmap = new(10, 10);
        using var deserialized = RoundTrip(bitmap, typeStyle: typeStyle, assemblyMatching: assemblyMatching);
        deserialized.Size.Should().Be(bitmap.Size);
    }

    [Theory]
    [MemberData(nameof(FormatterOptions))]
    [SupportedOSPlatform("windows")]
    public void Png_RoundTrip(FormatterTypeStyle typeStyle, FormatterAssemblyStyle assemblyMatching)
    {
        byte[] rawInlineImageBytes = Convert.FromBase64String(TestResources.TestPng);
        using Bitmap bitmap = new(new MemoryStream(rawInlineImageBytes));
        using var deserialized = RoundTrip(bitmap, typeStyle: typeStyle, assemblyMatching: assemblyMatching);
        deserialized.Size.Should().Be(bitmap.Size);
        deserialized.RawFormat.Should().Be(bitmap.RawFormat);
    }
}
