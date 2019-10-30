// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading.Tasks
{
    internal enum AsyncCausalityStatus
    {
        Started = 0,
        Completed = 1,
        Canceled = 2,
        Error = 3,
    }

    internal enum CausalityRelation
    {
        AssignDelegate = 0,
        Join = 1,
        Choice = 2,
        Cancel = 3,
        Error = 4,
    }

    internal enum CausalitySynchronousWork
    {
        CompletionNotification = 0,
        ProgressNotification = 1,
        Execution = 2,
    }
}
