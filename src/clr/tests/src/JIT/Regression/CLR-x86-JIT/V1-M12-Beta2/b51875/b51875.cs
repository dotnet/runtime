// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections;


namespace Test
{
    internal struct AA
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
        public static int Main()
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
