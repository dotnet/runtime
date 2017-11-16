// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

//
// Intentionally placed in the global namespace so that we don't have to see an annoying long type name
// in the xunit output.
//
internal sealed class TestException : Exception
{
    public TestException(string message)
       : base(message)
    {
        // This explicit exception type exists mostly so we can do the one useful thing that we can't do with downloaded xunit binaries:
        // put a BREAKPOINT to catch a test failure.
    }
}

