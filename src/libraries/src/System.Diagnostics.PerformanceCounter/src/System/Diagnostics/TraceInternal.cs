// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Diagnostics
{
    internal static class TraceInternal
    {
        // this is internal so TraceSource can use it.  We want to lock on the same object because both TraceInternal and 
        // TraceSource could be writing to the same listeners at the same time. 
        internal static readonly object critSec = new object();
    }
}
