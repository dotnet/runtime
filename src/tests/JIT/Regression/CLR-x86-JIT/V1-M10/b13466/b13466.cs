// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace Default
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class q
    {
        static
        int func(int i, int updateAddr, byte[] newBytes, int[] m_fixupPos)
        {
            while (i > 10)
            {
                if (i == 3)
                {
                    if (updateAddr < 0)
                        newBytes[m_fixupPos[i]] = (byte)(256 + updateAddr);
                    else
                        newBytes[m_fixupPos[i]] = (byte)updateAddr;
                }
                else
                    i--;
            }

            return i;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            func(0, 0, null, null);
            return 100;
        }
    }
}
