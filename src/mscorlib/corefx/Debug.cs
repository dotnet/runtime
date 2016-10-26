// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    internal static class Debug
    {
        [Conditional("_DEBUG")]
        static public void Assert(bool condition)
        {
            BCLDebug.Assert(condition);
        }

        [Conditional("_DEBUG")]
        static public void Assert(bool condition, string message)
        {
            BCLDebug.Assert(condition, message);
        }

        [Conditional("_DEBUG")]
        static public void Fail(string message)
        {
            BCLDebug.Assert(false, message);
        }
    }
}