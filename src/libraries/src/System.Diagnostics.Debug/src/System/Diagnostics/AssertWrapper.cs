// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;
using System.Threading;

namespace System.Diagnostics
{
    internal static class AssertWrapper
    {
        [SecuritySafeCritical]
        public static void ShowAssert(string stackTrace, string message, string detailMessage)
        {
            string fullMessage = message + Environment.NewLine + detailMessage + Environment.NewLine + stackTrace;

            int flags = Interop.User32.MB_OKCANCEL | Interop.User32.MB_ICONHAND | Interop.User32.MB_TOPMOST;                        

            if (IsRTLResources)
            {
                flags = flags | Interop.User32.MB_RIGHT | Interop.User32.MB_RTLREADING;
            }

            int rval = 0;

            // Run the message box on its own thread.
            rval = new MessageBoxPopup(fullMessage, Res.Strings.DebugAssertTitle, flags).ShowMessageBox();

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

        private static bool IsRTLResources
        {
            get
            {
                return Res.Strings.RTL != "RTL_False";
            }
        }
    }

    internal class MessageBoxPopup
    {
        public int ReturnValue { get; set; }
        private AutoResetEvent m_Event;
        private string m_Body;
        private string m_Title;
        private int m_Flags;

        [SecurityCritical]
        public MessageBoxPopup(string body, string title, int flags)
        {
            m_Event = new AutoResetEvent(false);
            m_Body = body;
            m_Title = title;
            m_Flags = flags;
        }

        public int ShowMessageBox()
        {
            Thread t = new Thread(DoPopup);
            t.Start();
            m_Event.WaitOne();
            return ReturnValue;
        }

        [SecuritySafeCritical]
        public void DoPopup()
        {
            ReturnValue = Interop.User32.MessageBox(IntPtr.Zero, m_Body, m_Title, m_Flags);
            m_Event.Set();
        }
    }
}
