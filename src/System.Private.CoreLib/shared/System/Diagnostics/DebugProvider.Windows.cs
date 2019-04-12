// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace System.Diagnostics
{
    public partial class DebugProvider
    {
        public static void FailCore(string stackTrace, string? message, string? detailMessage, string errorSource)
        {
            if (s_FailCore != null)
            {
                s_FailCore(stackTrace, message, detailMessage, errorSource); 
                return;
            }

            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
            else
            {
                // In Core, we do not show a dialog.
                // Fail in order to avoid anyone catching an exception and masking
                // an assert failure.
                DebugAssertException ex;
                if (message == string.Empty) 
                {
                    ex = new DebugAssertException(stackTrace);
                }
                else if (detailMessage == string.Empty) 
                {
                    ex = new DebugAssertException(message, stackTrace);
                }
                else
                {
                    ex = new DebugAssertException(message, detailMessage, stackTrace);
                }
                Environment.FailFast(ex.Message, ex, errorSource);
            }
        }

        private static readonly object s_ForLock = new object();

        public static void WriteCore(string message)
        {
            if (s_WriteCore != null)
            {
                s_WriteCore(message); 
                return;
            }

            // really huge messages mess up both VS and dbmon, so we chop it up into 
            // reasonable chunks if it's too big. This is the number of characters 
            // that OutputDebugstring chunks at.
            const int WriteChunkLength = 4091;

            // We don't want output from multiple threads to be interleaved.
            lock (s_ForLock)
            {
                if (message.Length <= WriteChunkLength)
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
