// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;


/// <summary>
/// Tests that concurrent collections are not included in a trimmed app targeting browser.
/// The idea is that the runtime should not depend on these types when running in a browser.
/// Motivation: application size.
/// Except ConcurrentDictionary which is used by other parts of the runtime and many apps.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        var bagType = GetTypeByName("ConcurrentBag`1");
        if (bagType != null)
        {
            Console.WriteLine("Failed ConcurrentBag");
            return -2;
        }
        var stackType = GetTypeByName("ConcurrentStack`1");
        if (stackType != null)
        {
            Console.WriteLine("Failed ConcurrentStack");
            return -3;
        }
        var collectionType = GetTypeByName("BlockingCollection`1");
        if (collectionType != null)
        {
            Console.WriteLine("Failed BlockingCollection");
            return -4;
        }
        var queueType = GetTypeByName("ConcurrentQueue`1");
        if (queueType != null)
        {
            Console.WriteLine("Failed ConcurrentQueue");
            return -5;
        }
        if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "System.Collections.Concurrent"))
        {
            Console.WriteLine("Failed: System.Collections.Concurrent assembly is present");
            return -6;
        }
        return 100;
    }

    // make reflection trimmer incompatible
    static Type GetTypeByName(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType("System.Collections.Concurrent." + typeName);
            if (type != null)
            {
                return type;
            }
        }
        return null;
    }
}