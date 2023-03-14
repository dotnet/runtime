// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class Program
{
    static int appVersion;

    [UnmanagedCallersOnly(EntryPoint = "InitializeMainAssembly", CallConvs = new Type[] { typeof(CallConvCdecl) })]
    static void InitializeMainAssembly(int version)
    {
        appVersion = version;
    }

    static int Main(string[] args)
    {
        Console.WriteLine($"MainAssembly version: {appVersion}");

        var referencedAssembly1 = new ReferencedAssembly1.Configuration();
        var referencedAssembly1Version = referencedAssembly1.GetAssemblyVersion();
        Console.WriteLine($"ReferencedAssembly1 version: {referencedAssembly1Version}");
        
        var referencedAssembly2 = new ReferencedAssembly2.Configuration();
        var referencedAssembly2Version = referencedAssembly2.GetAssemblyVersion();
        Console.WriteLine($"ReferencedAssembly2 version: {referencedAssembly2Version}");

        return appVersion + referencedAssembly1Version + (referencedAssembly2Version == 0 ? 50 : 0);
    }
}
