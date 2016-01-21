// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Emit
{
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum FlowControl
    {
        Branch = 0,
        Break = 1,
        Call = 2,
        Cond_Branch = 3,
        Meta = 4,
        Next = 5,
        Return = 7,
        Throw = 8,
    }
}
