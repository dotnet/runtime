// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

class Program
{
    static int Main(string[] args)
    {
        Console.WriteLine("Starting the test");
        string codeFile = @"HelloWorld.cs";

        var sourceTree = new List<SyntaxTree>(){SyntaxFactory.ParseSyntaxTree(File.ReadAllText(codeFile))};

        string mscorlibFile = Path.Combine(Environment.GetEnvironmentVariable("CORE_ROOT"), "mscorlib.dll");
        Console.WriteLine("Using reference to: {0}", mscorlibFile);
        var reference = new List<MetadataReference>(){ MetadataReference.CreateFromFile(mscorlibFile)};

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
