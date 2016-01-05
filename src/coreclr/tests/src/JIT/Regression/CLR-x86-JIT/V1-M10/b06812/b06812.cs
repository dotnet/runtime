// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Def
{
    using System;

    public class jitassert2
    {
        public static int Main(String[] args)
        {
            int i = -1;
            Object[] array = new Object[3];
            try
            {
                Object o = array[i];
                Console.WriteLine("Should have thrown!");
                return 1;
            }
            catch (System.Exception)
            {
                Console.WriteLine("Yup");
                return 100;
            }
        }
    }
}

