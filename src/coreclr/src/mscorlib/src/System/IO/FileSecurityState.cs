// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Enum:   FileSecurityState
** 
** 
**
**
** Purpose: Determines whether file system access is safe
**
**
===========================================================*/

using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security;
using System.Security.Permissions;

namespace System.IO
{
    [SecurityCritical]
    [System.Runtime.CompilerServices.FriendAccessAllowed]
    internal class FileSecurityState : SecurityState
    {
#if !PLATFORM_UNIX
        private static readonly char[] m_illegalCharacters = { '?', '*' };
#endif // !PLATFORM_UNIX

        private FileSecurityStateAccess m_access;
        private String m_userPath;
        private String m_canonicalizedPath;

        // default ctor needed for security rule consistency
        [SecurityCritical]
        private FileSecurityState()
        {
        }

        internal FileSecurityState(FileSecurityStateAccess access, String path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            VerifyAccess(access);
            m_access = access;
            m_userPath = path;
            if (path.Equals(String.Empty, StringComparison.OrdinalIgnoreCase))
            {
                m_canonicalizedPath = String.Empty;
            }
            else
            {
                VerifyPath(path);
                m_canonicalizedPath = System.IO.Path.GetFullPathInternal(path);
            }
        }

        // slight perf savings for trusted internal callers
        internal FileSecurityState(FileSecurityStateAccess access, String path, String canonicalizedPath)
        {
            VerifyAccess(access);
            VerifyPath(path);
            VerifyPath(canonicalizedPath);
   
            m_access = access;
            m_userPath = path;
            m_canonicalizedPath = canonicalizedPath;
        }

        internal FileSecurityStateAccess Access
        {
            get
            {
                return m_access;
            }
        }

        public String Path {
            [System.Runtime.CompilerServices.FriendAccessAllowed]
            get
            {
                return m_canonicalizedPath;
            }
        }

        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
        public override void EnsureState()
        {
            // this is the case for empty string machine name, etc
            if (String.Empty.Equals(m_canonicalizedPath))
                return;

            if (!IsStateAvailable())
            {
                throw new SecurityException(Environment.GetResourceString("FileSecurityState_OperationNotPermitted", (m_userPath == null) ? String.Empty : m_userPath));
            }
        }

        internal static FileSecurityStateAccess ToFileSecurityState(FileIOPermissionAccess access)
        {
            Contract.Requires((access & ~FileIOPermissionAccess.AllAccess) == 0);
            return (FileSecurityStateAccess)access; // flags are identical; just cast
        }

        private static void VerifyAccess(FileSecurityStateAccess access)
        {
            if ((access & ~FileSecurityStateAccess.AllAccess) != 0)
                throw new ArgumentOutOfRangeException("access", Environment.GetResourceString("Arg_EnumIllegalVal"));
        }

        private static void VerifyPath(String path)
        {
            if (path != null)
            {
                path = path.Trim();

#if !PLATFORM_UNIX
                if (path.Length > 2 && path.IndexOf( ':', 2 ) != -1)
                    throw new NotSupportedException( Environment.GetResourceString( "Argument_PathFormatNotSupported" ) );
#endif // !PLATFORM_UNIX

                System.IO.Path.CheckInvalidPathChars(path);

#if !PLATFORM_UNIX
                if (path.IndexOfAny( m_illegalCharacters ) != -1)
                    throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidPathChars" ) );
#endif // !PLATFORM_UNIX
            }
        }
    }
}
