// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

namespace DefaultNamespace
{
    using System;
    using System.Collections;

    internal class test
    {

        public static void ccc(byte[] bytes)
        {
            int[] m_array;
            int m_length;

            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            m_array = new int[(bytes.Length + 3) / 4];
            m_length = bytes.Length * 8;

            int i = 0;
            int j = 0;
            while (bytes.Length - j >= 4)
            {
                m_array[i++] = (bytes[j] & 0xff) |
                              ((bytes[j + 1] & 0xff) << 8) |
                              ((bytes[j + 2] & 0xff) << 16) |
                              ((bytes[j + 3] & 0xff) << 24);
                j += 4;
            }
            if (bytes.Length - j >= 0)
            {
                Console.WriteLine("hhhh");
            }
        }

        public static int Main(String[] args)
        {
            byte[] ub = new byte[0];
            ccc(ub);
            return 100;
        }
    }
}
