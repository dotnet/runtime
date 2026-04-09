// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.Win32
{
    /// <devdoc>
    ///    <para> Specifies the reason for the session switch</para>
    /// </devdoc>
    public enum SessionSwitchReason
    {
        /// <devdoc>
        ///      A session was connected to the console session.
        /// </devdoc>
        ConsoleConnect = Interop.Wtsapi32.WTS_CONSOLE_CONNECT,

        /// <devdoc>
        ///      A session was disconnected from the console session.
        /// </devdoc>
        ConsoleDisconnect = Interop.Wtsapi32.WTS_CONSOLE_DISCONNECT,

        /// <devdoc>
        ///      A session was connected to the remote session.
        /// </devdoc>
        RemoteConnect = Interop.Wtsapi32.WTS_REMOTE_CONNECT,

        /// <devdoc>
        ///      A session was disconnected from the remote session.
        /// </devdoc>
        RemoteDisconnect = Interop.Wtsapi32.WTS_REMOTE_DISCONNECT,

        /// <devdoc>
        ///      A user has logged on to the session.
        /// </devdoc>
        SessionLogon = Interop.Wtsapi32.WTS_SESSION_LOGON,

        /// <devdoc>
        ///      A user has logged off the session.
        /// </devdoc>
        SessionLogoff = Interop.Wtsapi32.WTS_SESSION_LOGOFF,

        /// <devdoc>
        ///      A session has been locked.
        /// </devdoc>
        SessionLock = Interop.Wtsapi32.WTS_SESSION_LOCK,

        /// <devdoc>
        ///      A session has been unlocked.
        /// </devdoc>
        SessionUnlock = Interop.Wtsapi32.WTS_SESSION_UNLOCK,

        /// <devdoc>
        ///      A session has changed its remote controlled status.
        /// </devdoc>
        SessionRemoteControl = Interop.Wtsapi32.WTS_SESSION_REMOTE_CONTROL
    }
}
