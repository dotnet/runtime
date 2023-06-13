// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Annotations used by debugger
    /// </summary>
    public static class DebugAnnotations
    {
        /// <summary>
        /// Informs debugger that previous line contains code that debugger needs to dive deeper inside.
        /// </summary>
        public static void PreviousCallContainsDebuggerStepInCode()
        {
            // This is a marker method and has no code in method body
        }
    }
}
