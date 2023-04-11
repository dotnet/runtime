// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Test
{
    using System;

    public class AA
    {
        int[] m_anField3 = new int[100];

        static bool Static1(ref int[] param1) { return false; }

        [Fact]
        public static int TestEntryPoint()
        {
            AA local5 = new AA();
            while (AA.Static1(ref local5.m_anField3)) ;
            return 100;
        }
    }
}
