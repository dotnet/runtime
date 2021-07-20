// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using SkiaSharp;

public class Test
{
    public static int Main()
    {
        using SKFileStream skfs = new SKFileStream("mono.png");
        using SKImage img = SKImage.FromEncodedData(skfs);

        Console.WriteLine ($"Size: {skfs.Length} Height: {img.Height}, Width: {img.Width}");
        return 0;
    }
}
