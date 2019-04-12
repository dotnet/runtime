// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// We need to define the DEBUG symbol because we want the logger
// to work even when this package is compiled on release. Otherwise,
// the call to Debug.WriteLine will not be in the release binary
#define DEBUG

namespace Microsoft.Extensions.Logging.Debug
{
    internal partial class DebugLogger
    {
        private void DebugWriteLine(string message, string name)
        {
            System.Diagnostics.Debug.WriteLine(message, category: name);
        }
    }
}
