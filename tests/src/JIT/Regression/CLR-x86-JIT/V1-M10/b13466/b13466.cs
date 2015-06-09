// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

namespace Default
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    class q
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

        public
        static
        int Main(String[] args)
        {
            func(0, 0, null, null);
            return 100;
        }
    }
}
