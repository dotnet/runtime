// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        internal static void Main1()
        {
            bool local2 = true;
            while (local2) { throw new Exception(); }
            while (local2)
            {
                bool[] local3 = (new bool[119]);
                //for (; local2; new AA[]{  }) - not a valid statement, see VS7 #244656.
                for (; local2; new AA())
                {
                }
            }
        }
        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                Main1();
            }
            catch (Exception)
            {
                return 100;
            }
            return 101;
        }
    }

}
