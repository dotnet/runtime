// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Runtime.CompilerServices;

public class ClassWithCctor<T>
{
    static TimeSpan span1;

    static ClassWithCctor()
    {
        span1 = TimeSpan.Parse("00:01:00");
    }
}

public class Repro
{
    public static int Main()
    {
        try
        {
            RuntimeHelpers.RunClassConstructor(typeof(ClassWithCctor<object>).TypeHandle);
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 101;
        }
    }
}
