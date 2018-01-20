// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Security;
using Microsoft.Win32;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.IO
{
    public abstract partial class FileSystemInfo : MarshalByRefObject, ISerializable
    {
        protected string FullPath;          // fully qualified path of the file or directory
        protected string OriginalPath;      // path passed in by the user
        private string _displayPath = "";   // path that can be displayed to the user

        protected FileSystemInfo()
        {
        }

        protected FileSystemInfo(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        // Full path of the directory/file
        public virtual string FullName
        {
            get
            {
                return FullPath;
            }
        }

        public string Extension
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
                return string.Empty;
            }
        }

        // For files name of the file is returned, for directories the last directory in hierarchy is returned if possible,
        // otherwise the fully qualified name s returned
        public abstract string Name
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
            get
            {
                return CreationTimeCore.UtcDateTime;
            }

            set
            {
                CreationTimeCore = File.GetUtcDateTimeOffset(value);
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
            get
            {
                return LastAccessTimeCore.UtcDateTime;
            }

            set
            {
                LastAccessTimeCore = File.GetUtcDateTimeOffset(value);
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
            get
            {
                return LastWriteTimeCore.UtcDateTime;
            }

            set
            {
                LastWriteTimeCore = File.GetUtcDateTimeOffset(value);
            }
        }

        internal string DisplayPath
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
