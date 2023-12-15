// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections;
using Xunit;


namespace Test
{
    public struct AA
    {
        public static int Main1()
        {
            AA[] local1 = new AA[10];
            try
            {
                goto EOM;
            }
            finally
            {
                throw new Exception();
            }
        EOM:
            if (((Array)new Object()).Clone() == null)
                return 1;
            return 0;
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
                return 101;
            }
            catch (Exception)
            {
                return 100;
            }
        }
    }
}
