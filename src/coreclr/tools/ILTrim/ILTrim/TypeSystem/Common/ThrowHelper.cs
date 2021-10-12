// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    public static partial class ThrowHelper
    {
        [System.Diagnostics.DebuggerHidden]
        public static void ThrowInvalidProgramException()
        {
            throw new System.Exception("Invalid program detected.");
        }
    }
}
