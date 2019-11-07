// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.ConstrainedExecution
{
    public enum Cer : int
    {
        None = 0,
        MayFail = 1,  // Might fail, but the method will say it failed
        Success = 2,
    }
}
