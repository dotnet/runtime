// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Versioning;

namespace System.Resources.Extensions.Tests.Common;

// This type can not be annotated with [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsDrawingSupported))]
// because the base type is annotated with [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
public abstract class SystemDrawingTests<T> : SerializationTest<T> where T : ISerializer
{
    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsDrawingSupported), nameof(PlatformDetection.SupportsComInterop))]
    [MemberData(nameof(FormatterOptions))]
    [SupportedOSPlatform("windows")]
    public void Bitmap_RoundTrip(FormatterTypeStyle typeStyle, FormatterAssemblyStyle assemblyMatching)
    {
        using Bitmap bitmap = new(10, 10);
        using var deserialized = RoundTrip(bitmap, typeStyle: typeStyle, assemblyMatching: assemblyMatching);
        Assert.Equal(bitmap.Size, deserialized.Size);
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsDrawingSupported), nameof(PlatformDetection.SupportsComInterop))]
    [MemberData(nameof(FormatterOptions))]
    [SupportedOSPlatform("windows")]
    public void Png_RoundTrip(FormatterTypeStyle typeStyle, FormatterAssemblyStyle assemblyMatching)
    {
        byte[] rawInlineImageBytes = Convert.FromBase64String(TestResources.TestPng);
        using Bitmap bitmap = new(new MemoryStream(rawInlineImageBytes));
        using var deserialized = RoundTrip(bitmap, typeStyle: typeStyle, assemblyMatching: assemblyMatching);
        Assert.Equal(bitmap.Size, deserialized.Size);
        Assert.Equal(bitmap.RawFormat, deserialized.RawFormat);
    }
}
