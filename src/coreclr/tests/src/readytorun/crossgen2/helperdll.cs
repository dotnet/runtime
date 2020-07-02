// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public class HelperClass
{
    // This method is used to test whether or not a method from a separate module 
    // referenced via Delegate is handled correctly. Do not call this method directly.
    public static void DelegateReferencedMethod()
    {
        Console.WriteLine("In helper method");
    }

    // This method is used to test whether or not a method from a separate module 
    // referenced as a function pointer is handled correctly. Do not call this method directly
    public static void FunctionPointerReferencedMethod()
    {
        Console.WriteLine("In function pointer method");
    }
}