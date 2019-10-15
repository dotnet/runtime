// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Collections.Generic;

public class Test
{
    public static int Main()
    {
        try
        {
            BodyDictionary obj = new BodyDictionary();

            Console.WriteLine("PASS");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("FAIL: Unexpected exception : " + e);
            return 101;
        }
    }
}

public class BodyDictionary : Dictionary<int, int>
{
    public BodyDictionary()
        : base()
    { }
}
