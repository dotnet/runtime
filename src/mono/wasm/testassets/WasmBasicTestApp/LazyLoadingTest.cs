// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;

public partial class LazyLoadingTest
{
    [JSExport]
    public static void Run()
    {
        // System.Text.Json is marked as lazy loaded in the csproj ("BlazorWebAssemblyLazyLoad"), this method can be called only after the assembly is lazy loaded
        // In the test case it is done in the JS before call to this method
        var text = JsonSerializer.Serialize(new Person("John", "Doe"));
        TestOutput.WriteLine(text);
    }

    public record Person(string FirstName, string LastName);
}
