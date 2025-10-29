// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        public static void HandleAsyncEntryPoint(Task task)
        {
            task.GetAwaiter().GetResult();
        }

        public static int HandleAsyncEntryPoint(Task<int> task)
        {
            return task.GetAwaiter().GetResult();
        }
    }
}
