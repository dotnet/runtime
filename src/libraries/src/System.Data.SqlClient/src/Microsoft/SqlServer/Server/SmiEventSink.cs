// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Data;


namespace Microsoft.SqlServer.Server
{
    // SqlEventSink is implemented by calling code.  In all methods that accept
    // a SqlEventSink directly the sink must be able to handle multiple callbacks 
    // without control returning from the original call.

    // Methods that do not accept SmiEventSync are (generally) ProcessEvent on
    // the SmiEventStream methods returning a SmiEventStream and methods that 
    // are certain to never call to the server (most will, for in-proc back end). 

    // Methods are commented with their corresponding TDS token

    // NOTE: Throwing from these methods will not usually produce the desired
    //       effect -- the managed to native boundary will eat any exceptions,
    //       and will cause a simple "Something bad happened" exception to be
    //       thrown in the native to managed boundary...
    internal abstract class SmiEventSink
    {
    }
}

