// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is regression test for VSW 583649
// We were AVing due to checking whether the struct ValueType147 is marshalable.
// The struct contains a literal field and we should ignore marshaling data attached to literals and statics

using System;

public class Type_Class42_Driver
{

    public static int Main()
    {
        try
        {
            Console.WriteLine("Instantiating: Class42_0...");
            Class42 Class42_0 = new Class42();
            
            Console.WriteLine("PASS");
            return 100;
        } 
        catch (Exception e)
        {
            Console.WriteLine("FAIL: Caught unexpected exception: " + e.Message);
            return 101;
        }
    } 
} 
