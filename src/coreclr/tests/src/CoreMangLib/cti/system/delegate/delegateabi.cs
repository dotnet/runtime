// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace ClassLibrary
{
    public class test
    {
        public delegate float FloatTest(float a);

        static float testfloat(float a)
        {
            if(a != -1.1f)
            {
                throw new Exception();
            }
            return -2.2f;
        }

        public delegate int IntTest(int a);

        static int testint(int a)
        {
           if(a != -1)
           {
                throw new Exception();
           }
           return -2;
        }

       static int Main(string[] args)
       {
            try
            {
                IntTest inttest = testint;
                int r = inttest(-1);
                if (r != -2)
                {
                    Console.WriteLine("FAIL - int return value mismatch");
                    return 101;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("FAIL - int argument value mismatch");
                return 102;
            }
            try
            {
                FloatTest floattest = testfloat;
                float r = floattest(-1.1f);
                if (r != -2.2f)
                {
                    Console.WriteLine("FAIL - float return value mismatch");
                    return 103;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("FAIL - float argument value mismatch");
                return 104;
            }
            return 100;
       }
   }
}
