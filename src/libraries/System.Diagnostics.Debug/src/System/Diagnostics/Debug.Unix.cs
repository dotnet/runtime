// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public static partial class Debug
    {
        // internal and not read only so that the tests can swap this out.
        internal static IDebugLogger s_logger = new UnixDebugLogger();

        // --------------
        // PAL ENDS HERE
        // --------------

        internal sealed class UnixDebugLogger : IDebugLogger
        {
            public void ShowAssertDialog(string stackTrace, string message, string detailMessage)
            {
                // TODO: Implement this
                throw new NotImplementedException();
            }

            public void WriteCore(string message)
            {
                WriteToSyslog(message);
                WriteToFile(Interop.Devices.stderr, message);
            }

            private static void WriteToSyslog(string message)
            {
                Interop.libc.syslog(Interop.libc.LOG_USER | Interop.libc.LOG_DEBUG, "%s", message);
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

                    using (SafeFileHandle fileHandle = SafeFileHandle.Open(filePath, Interop.libc.OpenFlags.O_WRONLY, 0))
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

                            while (bufCount > 0)
                            {
                                int bytesWritten;
                                while (Interop.CheckIo(bytesWritten = (int)Interop.libc.write((int)fileHandle.DangerousGetHandle(), buf, new IntPtr(bufCount)))) ;
                                bufCount -= bytesWritten;
                                buf += bytesWritten;
                            }
                        }
                    }
                }
            }
        }
    }
}

