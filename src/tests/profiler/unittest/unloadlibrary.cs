// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace UnloadLibrary
{
    public class TestClass : MarshalByRefObject
    {
        static string staticString = "A static string";
        string[] instanceStrings;
        static int s_count;

        static List<TestClass> instances = new List<TestClass>();

        public TestClass()
        {
            instanceStrings = new string[100];
            for (int i = 0; i < instanceStrings.Length; i++)
            {
                instanceStrings[i] = staticString + (++s_count);
            }
            Console.WriteLine("Class1 constructed");

            instances.Add(this);
        }
    }
}
