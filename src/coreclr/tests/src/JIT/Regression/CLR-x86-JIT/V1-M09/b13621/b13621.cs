// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
namespace DefaultNamespace
{
    internal class RootMem
    {
        internal long[] l;
        internal static GCHandle[] root;
        internal static int n;

        public static int Main(String[] args)
        {
            int iSize = 1000;
            root = new GCHandle[iSize];
            RootMem rm_obj;
            rm_obj = new RootMem(10);
            for (n = 0; n < iSize; n++)
            {
                root[n] = GCHandle.Alloc(rm_obj);
                if (n % 50 == 0)
                    Console.Out.WriteLine(n);
            }
            return 100;
        }

        public RootMem(int i)
        {
            if (i > 0)
            {
                l = new long[i];
                l[0] = 0;
                l[i - 1] = i;
            }
        }

        ~RootMem()
        {
            root[n - 1].Free();
        }
    }
}
