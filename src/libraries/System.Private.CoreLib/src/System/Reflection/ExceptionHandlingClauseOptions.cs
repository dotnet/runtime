// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    [Flags]
    public enum ExceptionHandlingClauseOptions : int
    {
        Clause = 0x0,
        Filter = 0x1,
        Finally = 0x2,
        Fault = 0x4,
    }
}
