// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Threading;
using System.Runtime.CompilerServices;
internal class Foo
{
    private static int s_taskIdCounter;
    private int _taskId = 0;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Function()
    {
        if (_taskId == 0)
        {
            int newId = Interlocked.Increment(ref s_taskIdCounter);
            Interlocked.CompareExchange(ref _taskId, newId, 0);
        }
        return _taskId;
    }
    public static int Main()
    {
        if (new Foo().Function() == 1) return 100; else return 101;
    }
}
