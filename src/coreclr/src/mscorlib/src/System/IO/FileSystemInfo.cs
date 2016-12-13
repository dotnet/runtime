// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Diagnostics.Contracts;

namespace System.IO
{
#if FEATURE_SERIALIZATION
    [Serializable]
#endif
    [ComVisible(true)]
    public abstract class FileSystemInfo : MarshalByRefObject, ISerializable {
        
        internal Win32Native.WIN32_FILE_ATTRIBUTE_DATA _data; // Cache the file information
        internal int _dataInitialised = -1; // We use this field in conjunction with the Refresh methods, if we succeed
                                            // we store a zero, on failure we store the HResult in it so that we can
                                            // give back a generic error back.

        private const int ERROR_INVALID_PARAMETER = 87;
        internal const int ERROR_ACCESS_DENIED = 0x5;

        protected String FullPath;          // fully qualified path of the directory
        protected String OriginalPath;      // path passed in by the user
        private String _displayPath = "";   // path that can be displayed to the user

        protected FileSystemInfo()
        {
        }

        protected FileSystemInfo(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            Contract.EndContractBlock();

            // Must use V1 field names here, since V1 didn't implement 
            // ISerializable.
            FullPath = Path.GetFullPath(info.GetString("FullPath"));
            OriginalPath = info.GetString("OriginalPath");

            // Lazily initialize the file attributes.
            _dataInitialised = -1;
        }

        internal void InitializeFrom(Win32Native.WIN32_FIND_DATA findData)
        {
            _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            _data.PopulateFrom(findData);
            _dataInitialised = 0;
        }

        // Full path of the direcory/file
        public virtual String FullName {
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
                for (int i = length; --i >= 0;) {
                    char ch = FullPath[i];
                    if (ch == '.')
                        return FullPath.Substring(i, length - i);
                    if (ch == Path.DirectorySeparatorChar || ch == Path.AltDirectorySeparatorChar || ch == Path.VolumeSeparatorChar)
                        break;
                }
                return String.Empty;
            }
        }

        // For files name of the file is returned, for directories the last directory in hierarchy is returned if possible,
        // otherwise the fully qualified name s returned
        public abstract String Name {
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
            get {
                    // depends on the security check in get_CreationTimeUtc
                    return CreationTimeUtc.ToLocalTime();
            }
        }

       [ComVisible(false)]
       public DateTime CreationTimeUtc {
            get {
                if (_dataInitialised == -1) {
                    _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
                    Refresh();
                }

                if (_dataInitialised != 0) // Refresh was unable to initialise the data
                    __Error.WinIOError(_dataInitialised, DisplayPath);
                
                long fileTime = ((long)_data.ftCreationTimeHigh << 32) | _data.ftCreationTimeLow;
                return DateTime.FromFileTimeUtc(fileTime);
                
            }
        }

        public DateTime LastAccessTime
        {
           get {
                // depends on the security check in get_LastAccessTimeUtc
                return LastAccessTimeUtc.ToLocalTime();
           }
           set {
                LastAccessTimeUtc = value.ToUniversalTime();
            }
        }

        [ComVisible(false)]
        public DateTime LastAccessTimeUtc {
            get {
                if (_dataInitialised == -1) {
                    _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
                    Refresh();
                }

                if (_dataInitialised != 0) // Refresh was unable to initialise the data
                    __Error.WinIOError(_dataInitialised, DisplayPath);
                    
                long fileTime = ((long)_data.ftLastAccessTimeHigh << 32) | _data.ftLastAccessTimeLow;
                return DateTime.FromFileTimeUtc(fileTime);
            }

            set {
            }
        }

        public DateTime LastWriteTime
        {
            get {
                // depends on the security check in get_LastWriteTimeUtc
                return LastWriteTimeUtc.ToLocalTime();
            }

            set {
                LastWriteTimeUtc = value.ToUniversalTime();
            }
        }

        [ComVisible(false)]
        public DateTime LastWriteTimeUtc {
            get {
                if (_dataInitialised == -1) {
                    _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
                    Refresh();
                }

                if (_dataInitialised != 0) // Refresh was unable to initialise the data
                    __Error.WinIOError(_dataInitialised, DisplayPath);
        
            
                long fileTime = ((long)_data.ftLastWriteTimeHigh << 32) | _data.ftLastWriteTimeLow;
                return DateTime.FromFileTimeUtc(fileTime);
            }

            set {
            }
        }

        public void Refresh()
        {
            _dataInitialised = File.FillAttributeInfo(FullPath, ref _data, false, false);
        }

        public FileAttributes Attributes {
            get
            {
                if (_dataInitialised == -1) {
                    _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
                    Refresh(); // Call refresh to intialise the data
                }

                if (_dataInitialised != 0) // Refresh was unable to initialise the data
                    __Error.WinIOError(_dataInitialised, DisplayPath);

                return (FileAttributes) _data.fileAttributes;
            }

            set {
                bool r = Win32Native.SetFileAttributes(FullPath, (int) value);
                if (!r) {
                    int hr = Marshal.GetLastWin32Error();
                    
                    if (hr==ERROR_INVALID_PARAMETER)
                        throw new ArgumentException(Environment.GetResourceString("Arg_InvalidFileAttrs"));
                    
                    // For whatever reason we are turning ERROR_ACCESS_DENIED into 
                    // ArgumentException here (probably done for some 9x code path).
                    // We can't change this now but special casing the error message instead.
                    if (hr == ERROR_ACCESS_DENIED)
                        throw new ArgumentException(Environment.GetResourceString("UnauthorizedAccess_IODenied_NoPathName"));
                    __Error.WinIOError(hr, DisplayPath);
                }
                _dataInitialised = -1;
            }
        }

        [ComVisible(false)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("OriginalPath", OriginalPath, typeof(String));
            info.AddValue("FullPath", FullPath, typeof(String));
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
