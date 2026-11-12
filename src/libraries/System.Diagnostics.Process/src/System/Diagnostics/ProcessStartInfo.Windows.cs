// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics
{
    public sealed partial class ProcessStartInfo
    {
        private string? _domain;

        [SupportedOSPlatform("windows")]
        public string? PasswordInClearText { get; set; }

        [SupportedOSPlatform("windows")]
        [AllowNull]
        public string Domain
        {
            get => _domain ?? string.Empty;
            set
            {
                ThrowIfEnvironmentWasAccessed(
                    nameof(Domain),
                    !string.IsNullOrEmpty(UserName) &&
                    !UseCredentialsForNetworkingOnly &&
                    !string.Equals(value ?? string.Empty, _domain ?? string.Empty, StringComparison.Ordinal));
                _domain = value;
            }
        }

        [SupportedOSPlatform("windows")]
        public bool LoadUserProfile { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the user credentials
        /// are only to be used for network resources.
        /// </summary>
        /// <value><c>true</c> if the user credentials are only to be used for
        /// network resources.</value>
        /// <remarks>
        /// <para>This property is referenced if the process is being started
        /// by using the user name, password, and domain.</para>
        /// <para>If the value is <c>true</c>, the process is started with the
        /// caller's identity. The system creates a new logon session with
        /// the given credentials, which is used on the network only.</para>
        /// <para>The system does not validate the specified credentials. Therefore,
        /// the process can start, but it may not have access to network resources.</para>
        /// </remarks>
        [SupportedOSPlatform("windows")]
        public bool UseCredentialsForNetworkingOnly
        {
            get;
            set
            {
                ThrowIfEnvironmentWasAccessed(
                    nameof(UseCredentialsForNetworkingOnly),
                    !string.IsNullOrEmpty(UserName) && field != value);
                field = value;
            }
        }

        [CLSCompliant(false)]
        [SupportedOSPlatform("windows")]
        public SecureString? Password { get; set; }

        // Assigned only by the UserName setter, so that apps which never set UserName
        // can trim the logon and environment block code (and its native dependencies).
        private static Func<ProcessStartInfo, IDictionary>? s_getUserEnvironmentVariables;

        private void ThrowIfEnvironmentWasAccessed(string propertyName, bool changesEnvironment)
        {
            if (_environmentVariables is not null && changesEnvironment)
            {
                throw new InvalidOperationException(SR.Format(SR.EnvironmentAccessedCannotSet, propertyName));
            }
        }

        /// <summary>
        /// Returns the environment variables the started process gets by default.
        /// When starting the process as a different user, these are the variables of that user rather than the current process.
        /// </summary>
        private IDictionary GetDefaultEnvironmentVariables() =>
            s_getUserEnvironmentVariables is not null && !string.IsNullOrEmpty(UserName) && !UseCredentialsForNetworkingOnly
                ? s_getUserEnvironmentVariables(this)
                : System.Environment.GetEnvironmentVariables();

        private unsafe IDictionary GetUserEnvironmentVariables()
        {
            if (Password != null && PasswordInClearText != null)
            {
                throw new ArgumentException(SR.CantSetDuplicatePassword);
            }

            SafeTokenHandle token;
            fixed (char* passwordInClearTextPtr = PasswordInClearText ?? string.Empty)
            {
                IntPtr passwordPtr = Password != null ? Marshal.SecureStringToGlobalAllocUnicode(Password) : IntPtr.Zero;
                try
                {
                    if (!Interop.Advapi32.LogonUser(
                        UserName,
                        Domain,
                        passwordPtr != IntPtr.Zero ? passwordPtr : (IntPtr)passwordInClearTextPtr,
                        Interop.Advapi32.LOGON32_LOGON_INTERACTIVE,
                        Interop.Advapi32.LOGON32_PROVIDER_DEFAULT,
                        out token))
                    {
                        int errorCode = Marshal.GetLastPInvokeError();
                        token.Dispose();
                        throw new Win32Exception(errorCode);
                    }
                }
                finally
                {
                    if (passwordPtr != IntPtr.Zero)
                    {
                        Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
                    }
                }
            }

            using (token)
            {
                // inherit: false, to match what CreateProcessWithLogonW does when no environment block is provided:
                // the child gets an environment created from the target user's profile, without the variables of the current process.
                // inherit: true would copy the current process' variables (possibly secrets) into the child running as another user.
                if (!Interop.Userenv.CreateEnvironmentBlock(out char* block, token, inherit: false))
                {
                    throw new Win32Exception();
                }

                try
                {
                    return EnvironmentBlockParser.Parse(block);
                }
                finally
                {
                    Interop.Userenv.DestroyEnvironmentBlock(block);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to start the process in a new process group.
        /// </summary>
        /// <value><c>true</c> if the process should be started in a new process group; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        /// <remarks>
        /// <para>When a process is created in a new process group, it becomes the root of a new process group.</para>
        /// <para>An implicit call to <c>SetConsoleCtrlHandler(NULL,TRUE)</c> is made on behalf of the new process, this means that the new process has CTRL+C disabled.</para>
        /// <para>This property is useful for preventing console control events sent to the child process from affecting the parent process.</para>
        /// </remarks>
        [SupportedOSPlatform("windows")]
        public bool CreateNewProcessGroup { get; set; }

        public string[] Verbs
        {
            get
            {
                string extension = Path.GetExtension(FileName);
                if (string.IsNullOrEmpty(extension))
                    return Array.Empty<string>();

                using (RegistryKey? key = Registry.ClassesRoot.OpenSubKey(extension))
                {
                    if (key == null)
                        return Array.Empty<string>();

                    string? value = key.GetValue(string.Empty) as string;
                    if (string.IsNullOrEmpty(value))
                        return Array.Empty<string>();

                    using (RegistryKey? subKey = Registry.ClassesRoot.OpenSubKey(value + "\\shell"))
                    {
                        if (subKey == null)
                            return Array.Empty<string>();

                        string[] names = subKey.GetSubKeyNames();
                        ArrayBuilder<string> verbs = default;
                        foreach (string name in names)
                        {
                            if (!string.Equals(name, "new", StringComparison.OrdinalIgnoreCase))
                            {
                                verbs.Add(name);
                            }
                        }
                        return verbs.ToArray();
                    }
                }
            }
        }
    }
}
