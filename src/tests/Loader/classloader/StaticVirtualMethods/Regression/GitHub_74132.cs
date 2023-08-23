// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// This regression test tracks the issue where GetInterfaceMap returns incorrect
// non-generic entries for generic static virtual methods.

public interface I<T> where T : I<T>
{
    public void Instance<M>(M m);
    public static abstract void Static<M>(M m);
}

public readonly struct C : I<C>
{
    public void Instance<M>(M m) { }
    public static void Static<M>(M m) { }
}

internal class Program
{
    static int Main(string[] args)
    {
        var interfaceMap = typeof(C).GetInterfaceMap(typeof(I<C>));
        bool allMethodsAreGeneric = true;
        for (int i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
        {
            var imethod = interfaceMap.InterfaceMethods[i];
            var tmethod = interfaceMap.TargetMethods[i];
            Console.WriteLine($"Interface.{imethod.Name} is generic method def: {imethod.IsGenericMethodDefinition}");
            Console.WriteLine($"Target.{tmethod.Name} is generic method def: {tmethod.IsGenericMethodDefinition}");
            if (!imethod.IsGenericMethodDefinition || !tmethod.IsGenericMethodDefinition)
            {
                allMethodsAreGeneric = false;
            }
        }
        
        if (!allMethodsAreGeneric)
        {
            throw new Exception("Test failed, all above methods should be reported as generic!");
        }
        
        return 100;
    }
}
