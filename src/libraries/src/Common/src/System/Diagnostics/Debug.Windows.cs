// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;
using System.Threading;

namespace System.Diagnostics
{
#if PUBLIC_DEBUG
    public static partial class Debug
#else
    internal static partial class Debug
#endif
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
                string fullMessage = message + Environment.NewLine + detailMessage + Environment.NewLine + stackTrace;

                Debug.WriteLine(fullMessage);
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                else
                {
                    Environment.FailFast(fullMessage);
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
                    Interop.mincore.OutputDebugString(message ?? string.Empty);
                }
            }
        }
    }
}
