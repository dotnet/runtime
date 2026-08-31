// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;
using TestLibrary;

public class RootClass
{
    [ActiveIssue("These tests are not supposed to be run with mono.", TestRuntimes.Mono)]
    [ActiveIssue("needs triage", TestPlatforms.Android)]
    [ActiveIssue("System.IO.FileNotFoundException: Could not load file or assembly '/.../Library/Developer/CoreSimulator/Devices/941235AB-7563-4D79-AC28-946B7AD2304A/data/Containers/Bundle/Application/40176A30-D8F5-4497-958A-6514E5C684FC/readytorun_multifolder.app/testdir-multifolder/../FolderA/FolderA/FolderA.dll' or one of its dependencies.", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
    [Fact]
    public static int TestEntryPoint()
    {
        string currentFolder = Path.GetDirectoryName(typeof(RootClass).Assembly.Location);
        string folderAPath = Path.Combine(currentFolder, "FolderA.dll");
        Console.WriteLine("Loading FolderA: {0}", folderAPath);
        Assembly folderA = AssemblyLoadContext.Default.LoadFromAssemblyPath(folderAPath);
        Type classA = folderA.GetType("ClassA");
        object resultA = classA.GetProperty("Property").GetValue(null);
        if (resultA != "ClassA.Property")
        {
            Console.WriteLine("ClassA.Property returned: '{0}'", resultA);
            return 101;
        }
        
        string folderBPath = Path.Combine(currentFolder, "FolderB.dll");
        Console.WriteLine("Loading FolderB: {0}", folderBPath);
        Assembly folderB = AssemblyLoadContext.Default.LoadFromAssemblyPath(folderBPath);
        Type classB = folderB.GetType("ClassB");
        object resultB = classB.GetProperty("Property").GetValue(null);
        if (resultB != "ClassB.Property")
        {
            Console.WriteLine("ClassB.Property returned: '{0}'", resultB);
            return 102;
        }
        return 100;
    }
}
