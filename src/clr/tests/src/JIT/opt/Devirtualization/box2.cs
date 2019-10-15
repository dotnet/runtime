// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main()
    {
        for (int i = 0; i < 10; i++)
        {
            // In the associated AwaitUnsafeOnCompleted, the 
            // jit should devirtualize, remove the box,
            // and change to call unboxed entry, passing
            // extra context argument.
            await new ValueTask<string>(Task.Delay(1).ContinueWith(_ => default(string))).ConfigureAwait(false);
        }

        return 100;
    }
}
