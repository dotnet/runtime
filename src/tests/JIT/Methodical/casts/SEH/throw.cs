// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace JitTest_throw_SEH_cs
{
    internal class Exception1 : Exception
    {
        override public String ToString() { return "Exception1"; }
    }

    internal class Exception2 : Exception
    {
        override public String ToString() { return "Exception2"; }
    }

    public class Test
    {
        [Fact]
        public static int TestEntryPoint()
        {
            object excep = new Exception1();

            try
            {
                throw (Exception)excep;
            }
            catch (Exception x)
            {
                Console.WriteLine(x.ToString());
                if (x is Exception1)
                    goto continue_1;
            }
            Console.WriteLine("castclass test failed.");
            return 101;

        continue_1:
            try
            {
                throw excep as Exception1;
            }
            catch (Exception x)
            {
                Console.WriteLine(x.ToString());
                if (x is Exception1)
                    goto continue_2;
            }
            Console.WriteLine("isinst test failed.");
            return 102;

        continue_2:
            try
            {
                throw (Exception2)excep;
            }
            catch (InvalidCastException x)
            {
                Console.WriteLine(x.ToString());
                goto continue_3;
            }
            catch (Exception x)
            {
                Console.WriteLine(x.ToString());
            }
            Console.WriteLine("negative castclass test failed.");
            return 103;

        continue_3:
            try
            {
                throw excep as Exception2;
            }
            catch (NullReferenceException x)
            {
                Console.WriteLine(x.ToString());
                goto continue_4;
            }
            catch (Exception x)
            {
                Console.WriteLine(x.ToString());
            }
            Console.WriteLine("negative isinst test failed.");
            return 104;

        continue_4:
            Console.WriteLine("all tests passed.");
            return 100;
        }
    }
}
