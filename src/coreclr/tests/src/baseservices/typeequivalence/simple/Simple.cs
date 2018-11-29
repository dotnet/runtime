// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

using TestLibrary;
using TypeEquivalenceTypes;

public class Simple
{
    private class EmptyType2 : IEmptyType
    {
        /// <summary>
        /// Create an instance of <see cref="EmptyType" />
        /// </summary>
        public static object Create()
        {
            return new EmptyType2();
        }
    }

    private static void InterfaceTypesFromDifferentAssembliesAreEqual()
    {
        Console.WriteLine("Interfaces are the same");
        var inAsm = EmptyType.Create();
        DisplayType((IEmptyType)inAsm);

        var otherAsm = EmptyType2.Create();
        DisplayType((IEmptyType)otherAsm);

        void DisplayType(IEmptyType i)
        {
            Console.WriteLine(i.GetType());
        }
    }

    public static int Main(string[] noArgs)
    {
        try
        {
            InterfaceTypesFromDifferentAssembliesAreEqual();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }
}