// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ServiceProcess
{
    /// <summary>
    /// Enum containing various session change reason
    /// </summary>
    public enum SessionChangeReason
    {
        ///<summary>
        /// A session was connected to the console session.
        ///</summary>
        ConsoleConnect = Interop.Advapi32.SessionStateChange.WTS_CONSOLE_CONNECT,

        ///<summary>
        ///A session was disconnected from the console session.
        ///</summary>
        ConsoleDisconnect = Interop.Advapi32.SessionStateChange.WTS_CONSOLE_DISCONNECT,

        ///<summary>
        /// A session was connected to the remote session.
        ///</summary>
        RemoteConnect = Interop.Advapi32.SessionStateChange.WTS_REMOTE_CONNECT,

        ///<summary>
        /// A session was disconnected from the remote session.
        ///</summary>
        RemoteDisconnect = Interop.Advapi32.SessionStateChange.WTS_REMOTE_DISCONNECT,

        ///<summary>
        /// A user has logged on to the session.
        ///</summary>
        SessionLogon = Interop.Advapi32.SessionStateChange.WTS_SESSION_LOGON,

        ///<summary>
        /// A user has logged off the session.
        ///</summary>
        SessionLogoff = Interop.Advapi32.SessionStateChange.WTS_SESSION_LOGOFF,

        ///<summary>
        /// A session has been locked.
        ///</summary>
        SessionLock = Interop.Advapi32.SessionStateChange.WTS_SESSION_LOCK,

        ///<summary>
        /// A session has been unlocked.
        ///</summary>
        SessionUnlock = Interop.Advapi32.SessionStateChange.WTS_SESSION_UNLOCK,

        ///<summary>
        /// A session has changed its remote controlled status.
        ///</summary>
        SessionRemoteControl = Interop.Advapi32.SessionStateChange.WTS_SESSION_REMOTE_CONTROL
    }
}
