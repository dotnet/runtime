// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

