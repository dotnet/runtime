// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;
using System.Threading;

namespace System.Diagnostics
{
    public static partial class Debug
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

                int flags = Interop.User32.MB_OKCANCEL | Interop.User32.MB_ICONHAND | Interop.User32.MB_TOPMOST;

                if (IsRTLResources)
                {
                    flags = flags | Interop.User32.MB_RIGHT | Interop.User32.MB_RTLREADING;
                }

                int rval = 0;

                // Run the message box on its own thread.
                rval = new MessageBoxPopup(fullMessage, SR.DebugAssertTitle, flags).ShowMessageBox();

                switch (rval)
                {
                    case Interop.User32.IDCANCEL:
                        if (!System.Diagnostics.Debugger.IsAttached)
                        {
                            System.Diagnostics.Debugger.Launch();
                        }
                        System.Diagnostics.Debugger.Break();
                        break;
                    default:
                        Debug.Assert(rval == Interop.User32.IDOK);
                        break;
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
                Interop.mincore.OutputDebugString(message ?? string.Empty);
            }

            private static bool IsRTLResources
            {
                get
                {
                    return SR.RTL != "RTL_False";
                }
            }

            internal class MessageBoxPopup
            {
                private readonly string _body;
                private readonly string _title;
                private readonly int _flags;
                private int _returnValue;


                [SecurityCritical]
                public MessageBoxPopup(string body, string title, int flags)
                {
                    _body = body;
                    _title = title;
                    _flags = flags;
                }

                public int ShowMessageBox()
                {
                    Thread t = new Thread(DoPopup);
                    t.Start();
                    t.Join();
                    return _returnValue;
                }

                [SecuritySafeCritical]
                public void DoPopup()
                {
                    _returnValue = Interop.User32.MessageBox(IntPtr.Zero, _body, _title, _flags);
                }
            }
        }
    }
}
