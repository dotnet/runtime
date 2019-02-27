// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading
{
    /// <summary>
    /// declaring a local var of this enum type and passing it by ref into a function that needs to do a
    /// stack crawl will both prevent inlining of the callee and pass an ESP point to stack crawl to
    /// Declaring these in EH clauses is illegal; they must declared in the main method body
    /// </summary>
    internal enum StackCrawlMark
    {
        LookForMe = 0,
        LookForMyCaller = 1,
        LookForMyCallersCaller = 2,
        LookForThread = 3
    }
}
