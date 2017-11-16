// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceProcess
{
    public enum SessionChangeReason
    {
        /// <devdoc>
        ///    <para>A session was connected to the console session. </para>
        /// </devdoc>
        ConsoleConnect          = Interop.Advapi32.SessionStateChange.WTS_CONSOLE_CONNECT,
        /// <devdoc>
        ///    <para>A session was disconnected from the console session. </para>
        /// </devdoc>
        ConsoleDisconnect       = Interop.Advapi32.SessionStateChange.WTS_CONSOLE_DISCONNECT,
        /// <devdoc>
        ///    <para>A session was connected to the remote session. </para>
        /// </devdoc>
        RemoteConnect           = Interop.Advapi32.SessionStateChange.WTS_REMOTE_CONNECT,
        /// <devdoc>
        ///    <para>A session was disconnected from the remote session. </para>
        /// </devdoc>
        RemoteDisconnect        = Interop.Advapi32.SessionStateChange.WTS_REMOTE_DISCONNECT,
        /// <devdoc>
        ///    <para>A user has logged on to the session. </para>
        /// </devdoc>
        SessionLogon            = Interop.Advapi32.SessionStateChange.WTS_SESSION_LOGON,
        /// <devdoc>
        ///    <para>A user has logged off the session. </para>
        /// </devdoc>
        SessionLogoff           = Interop.Advapi32.SessionStateChange.WTS_SESSION_LOGOFF,
        /// <devdoc>
        ///    <para>A session has been locked. </para>
        /// </devdoc>
        SessionLock             = Interop.Advapi32.SessionStateChange.WTS_SESSION_LOCK,
        /// <devdoc>
        ///    <para>A session has been unlocked. </para>
        /// </devdoc>
        SessionUnlock           = Interop.Advapi32.SessionStateChange.WTS_SESSION_UNLOCK,
        /// <devdoc>
        ///    <para>A session has changed its remote controlled status. </para>
        /// </devdoc>
        SessionRemoteControl    = Interop.Advapi32.SessionStateChange.WTS_SESSION_REMOTE_CONTROL
    }
}

