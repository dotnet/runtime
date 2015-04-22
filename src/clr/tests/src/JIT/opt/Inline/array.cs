// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
namespace JitInliningTest
{
    public class array
    {
        public static int Main()
        {

            String s = "";

            // Creates and initializes a new Array.
            Array myArray = Array.CreateInstance(typeof(String), 2, 4);
            myArray.SetValue("The", 0, 0);
            myArray.SetValue("quick", 0, 1);
            myArray.SetValue("brown", 0, 2);
            myArray.SetValue("fox", 0, 3);
            myArray.SetValue("jumped", 1, 0);
            myArray.SetValue("over", 1, 1);
            myArray.SetValue("the", 1, 2);
            myArray.SetValue("lazy", 1, 3);

            // Displays the values of the Array.
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

