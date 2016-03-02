// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

public class Test
{

    public static int Main()
    {
        try
        {
            Monitor.Enter(null);
            Console.WriteLine("Failed to throw exception on Monitor.Enter");
            return 1;
        }
        catch(ArgumentNullException)
        {
            //Expected            
        }
        return 100;
    }
}

