// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
public class SamplesArray
{
    public static int Main()
    {
        int[] myLens = new int[1] { 5 };
        int[] myLows = new int[1] { -2 };

        Array myArr = Array.CreateInstance(typeof(String), myLens, myLows);
        for (int i = myArr.GetLowerBound(0); i <= myArr.GetUpperBound(0); i++)
            myArr.SetValue(i.ToString(), i);

        Object[] objSZArray = myArr as Object[];
        if (objSZArray != null)
            Console.Error.WriteLine("Ack!  JIT casting bug!  This is not an SZArray!");

        try
        {
            Array.Reverse(myArr, -1, 3);
        }
        catch (Exception myException)
        {
            Console.WriteLine("Exception: " + myException.ToString());
        }
        return 100;
    }
}
