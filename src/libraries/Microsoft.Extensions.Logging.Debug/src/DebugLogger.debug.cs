// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// We need to define the DEBUG symbol because we want the logger
// to work even when this package is compiled on release. Otherwise,
// the call to Debug.WriteLine will not be in the release binary
#define DEBUG

namespace Microsoft.Extensions.Logging.Debug
{
    internal sealed partial class DebugLogger
    {
        private void DebugWriteLine(string message, string name)
        {
            System.Diagnostics.Debug.WriteLine(message, category: name);
        }
    }
}
