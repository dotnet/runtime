// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Diagnostics
{
    public partial class TraceEventCache
    {
        internal static int GetProcessId()
        {
            // Whereas the Win32 implementation caches the GetProcessId result, the Unix 
            // implementation doesn't so as to avoid problems with fork'd child processes 
            // ending up returning the same id as the parent.
            return Interop.Sys.GetPid();
        }
    }
}
