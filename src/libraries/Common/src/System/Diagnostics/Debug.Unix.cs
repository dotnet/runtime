// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    // Intentionally excluding visibility so it defaults to internal except for
    // the one public version in System.Diagnostics.Debug which defines
    // another version of this partial class with the public visibility 
    static partial class Debug
    {
        // internal and not readonly so that the tests can swap this out.
        internal static IDebugLogger s_logger = new UnixDebugLogger();

        // --------------
        // PAL ENDS HERE
        // --------------

        internal sealed class UnixDebugLogger : IDebugLogger
        {
            private const string EnvVar_DebugWriteToStdErr = "COMPlus_DebugWriteToStdErr";
            private static readonly bool s_shouldWriteToStdErr = 
                Environment.GetEnvironmentVariable(EnvVar_DebugWriteToStdErr) == "1";

            public void ShowAssertDialog(string stackTrace, string message, string detailMessage)
            {
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
                else
                {
                    // TODO: #3708 Determine if/how to put up a dialog instead.
                    var exc = new DebugAssertException(message, detailMessage, stackTrace);
                    if (!s_shouldWriteToStdErr) 
                    {
                        // We always want to print out Debug.Assert failures to stderr, even if
                        // !s_shouldWriteToStdErr, so if it wouldn't have been printed in
                        // WriteCore (only when s_shouldWriteToStdErr), print it here.
                        WriteToStderr(exc.Message);
                    }
                    throw exc;
                }
            }

            public void WriteCore(string message)
            {
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

                    using (SafeFileHandle fileHandle = SafeFileHandleHelper.Open(() => Interop.Sys.Dup(Interop.Sys.FileDescriptors.STDERR_FILENO)))
                    {
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
                                int bytesWritten = Interop.Sys.Write(fileHandle, buf + totalBytesWritten, bufCount);
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
    }
}
