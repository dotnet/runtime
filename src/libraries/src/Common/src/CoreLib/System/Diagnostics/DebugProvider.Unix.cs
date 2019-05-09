// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public partial class DebugProvider
    {
        private static readonly bool s_shouldWriteToStdErr = Environment.GetEnvironmentVariable("COMPlus_DebugWriteToStdErr") == "1";

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

        public static void WriteCore(string message)
        {
            if (s_WriteCore != null)
            {
                s_WriteCore(message); 
                return;
            }

            WriteToDebugger(message);

            if (s_shouldWriteToStdErr)
            {
                WriteToStderr(message);
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
                Interop.Sys.SysLog(Interop.Sys.SysLogPriority.LOG_USER | Interop.Sys.SysLogPriority.LOG_DEBUG, "%s", message);
            }
        }

        private static void WriteToStderr(string message)
        {
            // We don't want to write UTF-16 to a file like standard error.  Ideally we would transcode this
            // to UTF8, but the downside of that is it pulls in a bunch of stuff into what is ideally
            // a path with minimal dependencies (as to prevent re-entrency), so we'll take the strategy
            // of just throwing away any non ASCII characters from the message and writing the rest            

            const int BufferLength = 256;

            unsafe
            {
                byte* buf = stackalloc byte[BufferLength];
                int bufCount;
                int i = 0;

                while (i < message.Length)
                {
                    for (bufCount = 0; bufCount < BufferLength && i < message.Length; i++)
                    {
                        if (message[i] <= 0x7F)
                        {
                            buf[bufCount] = (byte)message[i];
                            bufCount++;
                        }
                    }

                    int totalBytesWritten = 0;
                    while (bufCount > 0)
                    {
                        int bytesWritten = Interop.Sys.Write(2 /* stderr */, buf + totalBytesWritten, bufCount);
                        if (bytesWritten < 0)
                        {
                            // On error, simply stop writing the debug output.  This could commonly happen if stderr
                            // was piped to a program that ended before this program did, resulting in EPIPE errors.
                            return;
                        }

                        bufCount -= bytesWritten;
                        totalBytesWritten += bytesWritten;
                    }
                }
            }
        }
    }
}
