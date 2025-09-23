// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    /// <summary>
    /// Represents a set of options for named synchronization objects that are wait handles and can be shared between processes,
    /// such as 'Mutex', 'Semaphore', and 'EventWaitHandle'.
    /// </summary>
    public struct NamedWaitHandleOptions
    {
        private bool _notCurrentUserOnly;
        private bool _notCurrentSessionOnly;

        /// <summary>
        /// Indicates whether the named synchronization object should be limited in access to the current user.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        ///
        /// If the option is true when creating a named synchronization object, the object is limited in access to the calling
        /// user. If the option is true when opening an existing named synchronization object, the object's access controls are
        /// verified for the calling user.
        ///
        /// If the option is false when creating a named synchronization object, the object is not limited in access to a user.
        ///
        /// On Unix-like operating systems, each user has namespaces for the object's name that are used when the option is
        /// true. These user-scoped namespaces are distinct from user-scoped namespaces for other users, and also distinct from
        /// namespaces used when the option is false.
        /// </remarks>
        public bool CurrentUserOnly
        {
            get => !_notCurrentUserOnly;
            set => _notCurrentUserOnly = !value;
        }

        /// <summary>
        /// Indicates whether the named synchronization object is intended to be used only within the current session.
        /// </summary>
        /// <remarks>
        /// The default value is true.
        ///
        /// Each session has namespaces for the object's name that are used when the option is true. These session-scoped
        /// namespaces are distinct from session-scoped namespaces for other sessions, and also distinct from namespaces used
        /// when the option is false.
        ///
        /// If the option is true when creating a named synchronization object, the object is limited in scope to the current
        /// session, and can't be opened by processes running in different sessions.
        ///
        /// On Windows, a session is a Terminal Services session. On Unix-like operating systems, a session is typically a shell
        /// session, where each shell gets its own session in which processes started from the shell run.
        /// </remarks>
        public bool CurrentSessionOnly
        {
            get => !_notCurrentSessionOnly;
            set => _notCurrentSessionOnly = !value;
        }
    }

    // This is an internal struct used by named wait handle helpers to also track whether the options were specified in APIs
    // that were used. Using the constructor indicates WasSpecified=true, and 'default' indicates WasSpecified=false.
    internal readonly struct NamedWaitHandleOptionsInternal
    {
        public const string CurrentSessionPrefix = @"Local\";
        public const string AllSessionsPrefix = @"Global\";

        private readonly NamedWaitHandleOptions _options;
        private readonly bool _wasSpecified;

        public NamedWaitHandleOptionsInternal(NamedWaitHandleOptions options)
        {
            _options = options;
            _wasSpecified = true;
        }

        public bool CurrentUserOnly
        {
            get
            {
                Debug.Assert(WasSpecified);
                return _options.CurrentUserOnly;
            }
        }

        public bool CurrentSessionOnly
        {
            get
            {
                Debug.Assert(WasSpecified);
                return _options.CurrentSessionOnly;
            }
        }

        public bool WasSpecified => _wasSpecified;

        public string GetNameWithSessionPrefix(string name)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert(WasSpecified);

            bool hasPrefix = name.Contains('\\');
            if (!hasPrefix)
            {
                if (CurrentSessionOnly)
                {
#if TARGET_WINDOWS
                    // Services use the global namespace by default, so always include a prefix on Windows
                    name = CurrentSessionPrefix + name;
#endif
                }
                else
                {
                    name = AllSessionsPrefix + name;
                }

                return name;
            }

            // Verify that the prefix is compatible with the CurrentSessionOnly option. On Windows, when CurrentSessionOnly is
            // false, any other prefix is permitted here, as a custom namespace can be created and used with a prefix.

            bool incompatible;
            if (CurrentSessionOnly)
            {
                incompatible = !name.StartsWith(CurrentSessionPrefix, StringComparison.Ordinal);
            }
            else
            {
#if TARGET_WINDOWS
                incompatible = name.StartsWith(CurrentSessionPrefix, StringComparison.Ordinal);
#else
                incompatible = !name.StartsWith(AllSessionsPrefix, StringComparison.Ordinal);
#endif
            }

            if (incompatible)
            {
                throw new ArgumentException(SR.Format(SR.NamedWaitHandles_IncompatibleNamePrefix, name), nameof(name));
            }

            return name;
        }
    }
}
