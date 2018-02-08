// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.IO.Pipelines
{
    [Flags]
    internal enum ResultFlags : byte
    {
        None = 0,
        Canceled = 1,
        Completed = 2
    }
}
