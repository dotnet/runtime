// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;
using System.Text;
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
                if (Debugger.IsAttached)
                {
                    string fullMessage = message + Environment.NewLine + detailMessage + Environment.NewLine + stackTrace;
                    Debug.WriteLine(fullMessage);
                    Debugger.Break();
                }
                else
                {
                    string fullMessage = FormatAssert(stackTrace, message, detailMessage) + Environment.NewLine;
                    WriteToStdErr(fullMessage); // ignore return value indicating whether or not write was successful
                    throw new InvalidOperationException(fullMessage);
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

            private static unsafe bool WriteToStdErr(string message)
            {
                IntPtr handle = Interop.mincore.GetStdHandle(Interop.mincore.HandleTypes.STD_ERROR_HANDLE);
                if (handle != IntPtr.Zero && handle != new IntPtr(-1))
                {
                    byte[] messageBytes = Encoding.ASCII.GetBytes(message);
                    fixed (byte* messageBytesPtr = messageBytes)
                    {
                        int numBytesWritten;
                        if (Interop.mincore.WriteFile(handle, messageBytesPtr, messageBytes.Length, out numBytesWritten, IntPtr.Zero) != 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
