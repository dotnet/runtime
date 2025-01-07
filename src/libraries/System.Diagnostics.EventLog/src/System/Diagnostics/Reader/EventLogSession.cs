// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Security;
using Microsoft.Win32;

namespace System.Diagnostics.Eventing.Reader
{
    /// <summary>
    /// Session Login Type
    /// </summary>
    public enum SessionAuthentication
    {
        Default = 0,
        Negotiate = 1,
        Kerberos = 2,
        Ntlm = 3
    }

    /// <summary>
    /// The type: log / external log file to query
    /// </summary>
    public enum PathType
    {
        LogName = 1,
        FilePath = 2
    }

    /// <summary>
    /// Defines a session for Event Log operations. The session can
    /// be configured for a remote machine and can use specific
    /// user credentials.
    /// </summary>
    public class EventLogSession : IDisposable
    {
        //
        // the two context handles for rendering (for EventLogRecord).
        // the system and user context handles. They are both common for all the event instances and can be created only once.
        // access to the data member references is safe, while
        // invoking methods on it is marked SecurityCritical as appropriate.
        //
        internal EventLogHandle renderContextHandleSystem = EventLogHandle.Zero;
        internal EventLogHandle renderContextHandleUser = EventLogHandle.Zero;

        // The dummy sync object for the two contexts.
        private readonly object _syncObject;

        private readonly string _server;
        private readonly string _user;
        private readonly string _domain;
        private readonly SessionAuthentication _logOnType;

        // Setup the System Context, once for all the EventRecords.
        internal void SetupSystemContext()
        {
            if (!this.renderContextHandleSystem.IsInvalid)
                return;
            lock (_syncObject)
            {
                if (this.renderContextHandleSystem.IsInvalid)
                {
                    // Create the SYSTEM render context
                    // Call the EvtCreateRenderContext to get the renderContextHandleSystem, so that we can get the system/values/user properties.
                    this.renderContextHandleSystem = NativeWrapper.EvtCreateRenderContext(0, null, UnsafeNativeMethods.EvtRenderContextFlags.EvtRenderContextSystem);
                }
            }
        }

        internal void SetupUserContext()
        {
            lock (_syncObject)
            {
                if (this.renderContextHandleUser.IsInvalid)
                {
                    // Create the USER render context
                    this.renderContextHandleUser = NativeWrapper.EvtCreateRenderContext(0, null, UnsafeNativeMethods.EvtRenderContextFlags.EvtRenderContextUser);
                }
            }
        }

        public EventLogSession()
        {
            // handle = EventLogHandle.Zero;
            _syncObject = new object();
        }

        public EventLogSession(string server)
            :
            this(server, null, null, (SecureString)null, SessionAuthentication.Default)
        {
        }

        public EventLogSession(string server, string domain, string user, SecureString password, SessionAuthentication logOnType)
        {
            server ??= "localhost";

            _syncObject = new object();

            _server = server;
            _domain = domain;
            _user = user;
            _logOnType = logOnType;

            UnsafeNativeMethods.EvtRpcLogin erLogin = default;
            erLogin.Server = _server;
            erLogin.User = _user;
            erLogin.Domain = _domain;
            erLogin.Flags = (int)_logOnType;
            erLogin.Password = CoTaskMemUnicodeSafeHandle.Zero;

            try
            {
                if (password != null)
                    erLogin.Password.SetMemory(SecureStringMarshal.SecureStringToCoTaskMemUnicode(password));
                // Open a session using the erLogin structure.
                Handle = NativeWrapper.EvtOpenSession(UnsafeNativeMethods.EvtLoginClass.EvtRpcLogin, ref erLogin, 0, 0);
            }
            finally
            {
                erLogin.Password.Dispose();
            }
        }

        internal EventLogHandle Handle { get; } = EventLogHandle.Zero;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this == s_globalSession)
                    throw new InvalidOperationException();
            }

            if (this.renderContextHandleSystem != null &&
                !this.renderContextHandleSystem.IsInvalid)
                this.renderContextHandleSystem.Dispose();

            if (this.renderContextHandleUser != null &&
                !this.renderContextHandleUser.IsInvalid)
                this.renderContextHandleUser.Dispose();

            if (Handle != null && !Handle.IsInvalid)
                Handle.Dispose();
        }

        public void CancelCurrentOperations()
        {
            NativeWrapper.EvtCancel(Handle);
        }

        private static readonly EventLogSession s_globalSession = new EventLogSession();
        public static EventLogSession GlobalSession
        {
            get { return s_globalSession; }
        }

        public IEnumerable<string> GetProviderNames()
        {
            List<string> namesList = new List<string>(100);

            using (EventLogHandle ProviderEnum = NativeWrapper.EvtOpenProviderEnum(this.Handle, 0))
            {
                bool finish = false;

                do
                {
                    string s = NativeWrapper.EvtNextPublisherId(ProviderEnum, ref finish);
                    if (finish == false)
                        namesList.Add(s);
                }
                while (finish == false);

                return namesList;
            }
        }

        public IEnumerable<string> GetLogNames()
        {
            List<string> namesList = new List<string>(100);

            using (EventLogHandle channelEnum = NativeWrapper.EvtOpenChannelEnum(this.Handle, 0))
            {
                bool finish = false;

                do
                {
                    string s = NativeWrapper.EvtNextChannelPath(channelEnum, ref finish);
                    if (finish == false)
                        namesList.Add(s);
                }
                while (finish == false);

                return namesList;
            }
        }

        public EventLogInformation GetLogInformation(string logName, PathType pathType)
        {
            ArgumentNullException.ThrowIfNull(logName);

            return new EventLogInformation(this, logName, pathType);
        }

        public void ExportLog(string path, PathType pathType, string query, string targetFilePath)
        {
            this.ExportLog(path, pathType, query, targetFilePath, false);
        }

        public void ExportLog(string path, PathType pathType, string query, string targetFilePath, bool tolerateQueryErrors)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(targetFilePath);

            UnsafeNativeMethods.EvtExportLogFlags flag = pathType switch
            {
                PathType.LogName => UnsafeNativeMethods.EvtExportLogFlags.EvtExportLogChannelPath,
                PathType.FilePath => UnsafeNativeMethods.EvtExportLogFlags.EvtExportLogFilePath,
                _ => throw new ArgumentOutOfRangeException(nameof(pathType)),
            };
            if (tolerateQueryErrors == false)
                NativeWrapper.EvtExportLog(this.Handle, path, query, targetFilePath, (int)flag);
            else
                NativeWrapper.EvtExportLog(this.Handle, path, query, targetFilePath, (int)flag | (int)UnsafeNativeMethods.EvtExportLogFlags.EvtExportLogTolerateQueryErrors);
        }

        public void ExportLogAndMessages(string path, PathType pathType, string query, string targetFilePath)
        {
            this.ExportLogAndMessages(path, pathType, query, targetFilePath, false, CultureInfo.CurrentCulture);
        }

        public void ExportLogAndMessages(string path, PathType pathType, string query, string targetFilePath, bool tolerateQueryErrors, CultureInfo targetCultureInfo)
        {
            ExportLog(path, pathType, query, targetFilePath, tolerateQueryErrors);
            // Ignore the CultureInfo, pass 0 to use the calling thread's locale
            NativeWrapper.EvtArchiveExportedLog(this.Handle, targetFilePath, 0, 0);
        }

        public void ClearLog(string logName)
        {
            this.ClearLog(logName, null);
        }

        public void ClearLog(string logName, string backupPath)
        {
            ArgumentNullException.ThrowIfNull(logName);

            NativeWrapper.EvtClearLog(this.Handle, logName, backupPath, 0);
        }
    }
}
