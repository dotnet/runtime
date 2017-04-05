// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;

class GitHub_10714
{
    const int Passed = 100;
    const int Failed = 0;
    
    static int intToExchange = -1;
    static short innerShort = 2;
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test() => Interlocked.Exchange(ref intToExchange, innerShort);

    static int Main()
    {
        int oldValue = Test();
        return (oldValue == -1 && intToExchange == 2) ? Passed : Failed;
    }
}
