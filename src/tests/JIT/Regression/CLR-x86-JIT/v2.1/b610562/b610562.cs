// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//This bug exists in whidbey rtm (2.0.50727.42) and has been fixed in Orcas and PUCLR
//When the test fails, the function call test(ref sometype) causes a
//bad image format exception to be thrown.  This is due to a problem when a generic
//static member is passed by ref to an interlocked method.

using System;
using Xunit;
namespace VTest
{
    public class TestMain : refTest<TestMain>
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int ret = 100;
            try
            {
                new TestMain();
                Console.WriteLine("PASS");
            }
            catch (System.Exception e)
            {
                Console.WriteLine("FAIL: exception thrown: " + e.Message);
                ret = 666;
            }
            return ret;
        }

    }

    public class refTest<type> where type : refTest<type>
    {
        public refTest()
        {

            test(ref sometype);

        }

        public void test(ref type r)
        {
            System.Threading.Interlocked.CompareExchange(ref r, this as type, null);
        }

        public static type sometype;
    }

}
