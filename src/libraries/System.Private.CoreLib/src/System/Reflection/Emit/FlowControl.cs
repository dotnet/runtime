// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    /// <summary>
    /// Describes how an instruction alters the flow of control.
    /// </summary>
    public enum FlowControl
    {
        Branch = 0,
        Break = 1,
        Call = 2,
        Cond_Branch = 3,
        Meta = 4,
        Next = 5,
        [Obsolete("FlowControl.Phi has been deprecated and is not supported.")]
        Phi = 6,
        Return = 7,
        Throw = 8,
    }
}
