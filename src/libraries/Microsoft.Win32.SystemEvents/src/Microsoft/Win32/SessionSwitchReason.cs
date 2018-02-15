//------------------------------------------------------------------------------
// <copyright file="SessionSwitchReason.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace Microsoft.Win32 {
    using System.Diagnostics;
    using System;

    /// <devdoc>
    ///    <para> Specifies the reason for the session switch</para>
    /// </devdoc>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum SessionSwitchReason {

        /// <devdoc>
        ///      A session was connected to the console session.
        /// </devdoc>
        ConsoleConnect = NativeMethods.WTS_CONSOLE_CONNECT,

        /// <devdoc>
        ///      A session was disconnected from the console session.
        /// </devdoc>
        ConsoleDisconnect = NativeMethods.WTS_CONSOLE_DISCONNECT,

        /// <devdoc>
        ///      A session was connected to the remote session.
        /// </devdoc>
        RemoteConnect = NativeMethods.WTS_REMOTE_CONNECT,

        /// <devdoc>
        ///      A session was disconnected from the remote session.
        /// </devdoc>
        RemoteDisconnect = NativeMethods.WTS_REMOTE_DISCONNECT,

        /// <devdoc>
        ///      A user has logged on to the session.
        /// </devdoc>
        SessionLogon = NativeMethods.WTS_SESSION_LOGON,

        /// <devdoc>
        ///      A user has logged off the session.
        /// </devdoc>
        SessionLogoff = NativeMethods.WTS_SESSION_LOGOFF,

        /// <devdoc>
        ///      A session has been locked.
        /// </devdoc>
        SessionLock = NativeMethods.WTS_SESSION_LOCK,

        /// <devdoc>
        ///      A session has been unlocked.
        /// </devdoc>
        SessionUnlock = NativeMethods.WTS_SESSION_UNLOCK,

        /// <devdoc>
        ///      A session has changed its remote controlled status.
        /// </devdoc>
        SessionRemoteControl = NativeMethods.WTS_SESSION_REMOTE_CONTROL
    }
}

