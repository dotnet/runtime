
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Runtime_55107
{
    class Program
    {
        class G
        {
        }

        static int Main(string[] args)
        {
            G g = new G();

            ref G iprnull = ref Unsafe.NullRef<G>();
            ref G ipr1 = ref g;

            if(Unsafe.AreSame(ref ipr1, ref iprnull))
            {
                // Failure case 1
                return -101;
            }
            else if(Unsafe.AreSame(ref ipr1, ref Unsafe.NullRef<G>()))
            {
                // Failure case 2
                return -102;
            }
            else
            {
               // Successful exit
               return 100;
            }
        }
    }
}
