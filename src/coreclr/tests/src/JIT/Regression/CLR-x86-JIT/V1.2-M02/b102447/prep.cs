// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

internal struct Gen<T>
{
    public static void Target<U>()
    { }
    public static void DelegateTest<U>()
    {
        ThreadStart d = new ThreadStart(Gen<T>.Target<U>);
        IAsyncResult ar = d.BeginInvoke(null, null);
        WaitHandle.WaitAll(new System.Threading.WaitHandle[] { ar.AsyncWaitHandle });
    }
}


public class Test
{
    public static int Main()
    {
        Gen<object>.DelegateTest<int>();
        return 100;
    }
}


