// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b36470
{
    using System;

    public class AA
    {
        int[] m_anField3 = new int[100];

        static bool Static1(ref int[] param1) { return false; }

        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            AA local5 = new AA();
            while (AA.Static1(ref local5.m_anField3)) ;
        }
    }
}
