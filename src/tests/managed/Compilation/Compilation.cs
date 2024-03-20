// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine("Starting the test");
        string codeFile = @"HelloWorld.cs";

        var sourceTree = new List<SyntaxTree>()
        {
            SyntaxFactory.ParseSyntaxTree(File.ReadAllText(codeFile))
        };

        string mscorlibFile = Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"),
                                           "System.Private.CoreLib.dll");

        Console.WriteLine("Using reference to: {0}", mscorlibFile);
        var reference = new List<MetadataReference>()
        {
            MetadataReference.CreateFromFile(mscorlibFile)
        };

        var compilation = CSharpCompilation.Create("helloworld", sourceTree, reference);
        Console.WriteLine("Test compiled");
        var result = compilation.Emit(new FileStream("helloworld.exe", FileMode.Create));

        if (!result.Success)
        {
            return -1;
        }

        return 100;
    }
}
