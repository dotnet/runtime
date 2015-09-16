// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
#if PUBLIC_DEBUG
    public static partial class Debug
#else
    internal static partial class Debug
#endif
    {
        // internal and not read only so that the tests can swap this out.
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
                // TODO: Determine whether there's anything better we can do here once
                //       Debugger.* is available on Unix.

                // When an assert fails, it calls WriteCore with the assert message, followed
                // by calling ShowAssertDialog.  If s_shouldWriteToStdError is true,
                // then the assert will have already been written to the console.  But if it's
                // false (the default), then it's easy for the important failure information
                // to be missed.  As such, if if it's false, we still output the error information to
                // stderr for lack of any better place to display it to the user; this will also
                // help ensure it shows up in continuous integration logs.
                string assertMessage = FormatAssert(stackTrace, message, detailMessage) + Environment.NewLine;
                if (!s_shouldWriteToStdErr)
                {
                    WriteToFile(Interop.Devices.stderr, assertMessage);
                }
            }

            public void WriteCore(string message)
            {
                Assert(message != null);

                WriteToSyslog(message);

                if (s_shouldWriteToStdErr)
                {
                    WriteToFile(Interop.Devices.stderr, message);
                }
            }

            private static void WriteToSyslog(string message)
            {
                Interop.Sys.SysLog(Interop.Sys.SysLogPriority.LOG_USER | Interop.Sys.SysLogPriority.LOG_DEBUG, "%s", message);
            }

            private static void WriteToFile(string filePath, string message)
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

                    using (SafeFileHandle fileHandle = SafeFileHandle.Open(filePath, Interop.Sys.OpenFlags.O_WRONLY, 0))
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
                                int bytesWritten;
                                while (Interop.CheckIo(bytesWritten = Interop.Sys.Write((int)fileHandle.DangerousGetHandle(), buf + totalBytesWritten, bufCount))) ;
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

