// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;


internal class test
{
    public static int Main(String[] args)
    {
        ArrayList objList = new ArrayList();
        objList.Add("hey");
        objList.Add(null);

        IEnumerator ienum = objList.GetEnumerator();
        int iCounter = 0;
        while (ienum.MoveNext())
        {
            iCounter++;
            Console.WriteLine(iCounter.ToString());
        }
        if (iCounter == 2)
        {
            Console.WriteLine("Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Failed");
            return 1;
        }
    }
}
