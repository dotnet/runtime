// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;

namespace System.Diagnostics
{
    // Intentionally excluding visibility so it defaults to internal except for
    // the one public version in System.Diagnostics.Debug which defines
    // another version of this partial class with the public visibility 
    static partial class Debug
    {
        // internal and not read only so that the tests can swap this out.
        internal static IDebugLogger s_logger = new WindowsDebugLogger();

        // --------------
        // PAL ENDS HERE
        // --------------

        internal sealed class WindowsDebugLogger : IDebugLogger
        {
            [SecuritySafeCritical]
            public void ShowAssertDialog(string stackTrace, string message, string detailMessage)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                else
                {
                    // TODO: #3708 Determine if/how to put up a dialog instead.
                    throw new DebugAssertException(message, detailMessage, stackTrace);
                }
            }

            public void WriteCore(string message)
            {
                // really huge messages mess up both VS and dbmon, so we chop it up into 
                // reasonable chunks if it's too big. This is the number of characters 
                // that OutputDebugstring chunks at.
                const int WriteChunkLength = 4091;

                // We don't want output from multiple threads to be interleaved.
                lock (s_ForLock)
                {
                    if (message == null || message.Length <= WriteChunkLength)
                    {
                        WriteToDebugger(message);
                    }
                    else
                    {
                        int offset;
                        for (offset = 0; offset < message.Length - WriteChunkLength; offset += WriteChunkLength)
                        {
                            WriteToDebugger(message.Substring(offset, WriteChunkLength));
                        }
                        WriteToDebugger(message.Substring(offset));
                    }
                }
            }

            [System.Security.SecuritySafeCritical]
            private static void WriteToDebugger(string message)
            {
                if (Debugger.IsLogging())
                {
                    Debugger.Log(0, null, message);
                }
                else
                {
                    Interop.Kernel32.OutputDebugString(message ?? string.Empty);
                }
            }
        }
    }
}
