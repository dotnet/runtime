// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

internal struct Gen
{
    public void Target<U>()
    {
    }
    public static void DelegateTest<U>()
    {
        ThreadStart d = new ThreadStart(new Gen().Target<U>);
        IAsyncResult ar = d.BeginInvoke(null, null);
        WaitHandle.WaitAll(new System.Threading.WaitHandle[] { ar.AsyncWaitHandle });
    }
}

public class Test
{
    public static int Main()
    {
        Gen.DelegateTest<object>();
        return 100;
    }
}


