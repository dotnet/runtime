// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TraceHelpers.cs
//
//
// Common routines used to trace information about execution, the state of things, etc.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;

namespace System.Linq.Parallel
{
    internal static class TraceHelpers
    {
        [Conditional("PFXTRACE")]
        internal static void TraceInfo(string msg, params object[] args)
        {
            Debug.WriteLine(string.Format(msg, args));
        }
    }
}
