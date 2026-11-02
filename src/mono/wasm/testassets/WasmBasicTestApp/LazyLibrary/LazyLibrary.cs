// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices.JavaScript;

[assembly:System.Runtime.Versioning.SupportedOSPlatform("browser")]

namespace LazyLibrary;

public partial class Foo
{
    [JSExport]
    public static string ReadResource(string culture)
    {
        Assembly assembly = Assembly.Load("ResourceLibrary");
        ResourceManager resources = new("ResourceLibrary.words", assembly);
        return resources.GetString("hello", new CultureInfo(culture))!;
    }

    [JSExport]
    public static int Bar()
    {
        Console.WriteLine("Hello from Foo.Bar!");
        Console.Out.Flush();
        return 42;
    }
}
