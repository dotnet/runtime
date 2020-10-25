// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

public class Test
{
    public static void Main (String[] args) {
        double _location = 0;
        double _newValue = 1;
        //Volatile.Read(ref _location);
        Console.WriteLine("The original result is: " + _location);
        Volatile.Write(ref _location, _newValue);
        Console.WriteLine("The result is: " + _location);
    }
}
