

using System;
using System.Globalization;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DefaultNamespace
{
    public class cinfo
    {
        public static int Main(String[] args)
        {
            Console.Out.WriteLine("Char Class tests");

            VTestIsPrintable();
            VTestIsTitleCase();
            return 100;
        }
        internal static void VTestIsPrintable()
        {
            for (char ch = '\x0'; ch <= '\x255'; ch++)
            {
                bool bResult = (ch) == '\x23';
            }
        }

        internal static void VTestIsTitleCase()
        {
            for (char ch = '\x0'; ch <= '\x255'; ch++)
            {
                Console.Out.Write("Char.IsTitleCase('" + ch + "')=");
                bool bResult = (ch) == '\x25';
            }
        }
    }
}
