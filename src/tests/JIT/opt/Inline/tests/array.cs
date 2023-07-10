// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitInliningTest
{
    public class array
    {
        [Fact]
        public static int TestEntryPoint()
        {
            String s = "";

            Array myArray = Array.CreateInstance(typeof(String), 2, 4);
            myArray.SetValue("The", 0, 0);
            myArray.SetValue("quick", 0, 1);
            myArray.SetValue("brown", 0, 2);
            myArray.SetValue("fox", 0, 3);
            myArray.SetValue("jumped", 1, 0);
            myArray.SetValue("over", 1, 1);
            myArray.SetValue("the", 1, 2);
            myArray.SetValue("lazy", 1, 3);

            for (int i = myArray.GetLowerBound(0); i <= myArray.GetUpperBound(0); i++)
                for (int j = myArray.GetLowerBound(1); j <= myArray.GetUpperBound(1); j++)
                    s += myArray.GetValue(i, j);

            if (s == "Thequickbrownfoxjumpedoverthelazy")
                return 100;
            else
                return 1;
        }
    }
}

