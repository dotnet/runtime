// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Security;
using Microsoft.Win32;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace System.IO
{
    public abstract partial class FileSystemInfo
    {
        protected String FullPath;          // fully qualified path of the file or directory
        protected String OriginalPath;      // path passed in by the user
        private String _displayPath = "";   // path that can be displayed to the user

        [System.Security.SecurityCritical]
        protected FileSystemInfo()
        {
        }

        // Full path of the directory/file
        public virtual String FullName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return FullPath;
            }
        }

        public String Extension
        {
            get
            {
                // GetFullPathInternal would have already stripped out the terminating "." if present.
                int length = FullPath.Length;
                for (int i = length; --i >= 0;)
                {
                    char ch = FullPath[i];
                    if (ch == '.')
                        return FullPath.Substring(i, length - i);
                    if (PathInternal.IsDirectorySeparator(ch) || ch == Path.VolumeSeparatorChar)
                        break;
                }
                return String.Empty;
            }
        }

        // For files name of the file is returned, for directories the last directory in hierarchy is returned if possible,
        // otherwise the fully qualified name s returned
        public abstract String Name
        {
            get;
        }

        // Whether a file/directory exists
        public abstract bool Exists
        {
            get;
        }

        // Delete a file/directory
        public abstract void Delete();

        public DateTime CreationTime
        {
            get
            {
                // depends on the security check in get_CreationTimeUtc
                return CreationTimeUtc.ToLocalTime();
            }
            set
            {
                CreationTimeUtc = value.ToUniversalTime();
            }
        }

        public DateTime CreationTimeUtc
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return FileSystemObject.CreationTime.UtcDateTime;
            }

            set
            {
                FileSystemObject.CreationTime = File.GetUtcDateTimeOffset(value);
            }
        }


        public DateTime LastAccessTime
        {
            get
            {
                // depends on the security check in get_LastAccessTimeUtc
                return LastAccessTimeUtc.ToLocalTime();
            }
            set
            {
                LastAccessTimeUtc = value.ToUniversalTime();
            }
        }

        public DateTime LastAccessTimeUtc
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return FileSystemObject.LastAccessTime.UtcDateTime;
            }

            set
            {
                FileSystemObject.LastAccessTime = File.GetUtcDateTimeOffset(value);
            }
        }

        public DateTime LastWriteTime
        {
            get
            {
                // depends on the security check in get_LastWriteTimeUtc
                return LastWriteTimeUtc.ToLocalTime();
            }

            set
            {
                LastWriteTimeUtc = value.ToUniversalTime();
            }
        }

        public DateTime LastWriteTimeUtc
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return FileSystemObject.LastWriteTime.UtcDateTime;
            }

            set
            {
                FileSystemObject.LastWriteTime = File.GetUtcDateTimeOffset(value);
            }
        }

        public void Refresh()
        {
            FileSystemObject.Refresh();
        }

        public FileAttributes Attributes
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return FileSystemObject.Attributes;
            }
            [System.Security.SecurityCritical] // auto-generated
            set
            {
                FileSystemObject.Attributes = value;
            }
        }

        internal String DisplayPath
        {
            get
            {
                return _displayPath;
            }
            set
            {
                _displayPath = value;
            }
        }
    }
}
