// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: 
**
**
===========================================================*/

using System;
using System.Collections;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace System.IO {
    [Serializable]
#if !FEATURE_CORECLR
    [FileIOPermissionAttribute(SecurityAction.InheritanceDemand,Unrestricted=true)]
#endif
    [ComVisible(true)]
#if FEATURE_REMOTING        
    public abstract class FileSystemInfo : MarshalByRefObject, ISerializable {
#else // FEATURE_REMOTING
    public abstract class FileSystemInfo : ISerializable {   
#endif  //FEATURE_REMOTING      
        
        [System.Security.SecurityCritical] // auto-generated
        internal Win32Native.WIN32_FILE_ATTRIBUTE_DATA _data; // Cache the file information
        internal int _dataInitialised = -1; // We use this field in conjunction with the Refresh methods, if we succeed
                                            // we store a zero, on failure we store the HResult in it so that we can
                                            // give back a generic error back.

        private const int ERROR_INVALID_PARAMETER = 87;
        internal const int ERROR_ACCESS_DENIED = 0x5;

        protected String FullPath;          // fully qualified path of the directory
        protected String OriginalPath;      // path passed in by the user
        private String _displayPath = "";   // path that can be displayed to the user

        #if FEATURE_CORECLR
#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#else
        [System.Security.SecuritySafeCritical]
#endif //FEATURE_CORESYSTEM
#endif
        protected FileSystemInfo()
        {
        }

        protected FileSystemInfo(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");
            Contract.EndContractBlock();
            
            // Must use V1 field names here, since V1 didn't implement 
            // ISerializable.
            FullPath = Path.GetFullPathInternal(info.GetString("FullPath"));
            OriginalPath = info.GetString("OriginalPath");

            // Lazily initialize the file attributes.
            _dataInitialised = -1;
        }

        [System.Security.SecurityCritical]
        internal void InitializeFrom(Win32Native.WIN32_FIND_DATA findData)
        {
            _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
            _data.PopulateFrom(findData);
            _dataInitialised = 0;
        }

        // Full path of the direcory/file
        public virtual String FullName {
            [System.Security.SecuritySafeCritical]
            get 
            {
                String demandDir;
                if (this is DirectoryInfo)
                    demandDir = Directory.GetDemandDir(FullPath, true);
                else
                    demandDir = FullPath;
#if FEATURE_CORECLR
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.PathDiscovery, String.Empty, demandDir);
                sourceState.EnsureState();
#else
                new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandDir).Demand();
#endif
                return FullPath;
            }
        }

        internal virtual String UnsafeGetFullName
        {
            [System.Security.SecurityCritical]
            get
            {
                String demandDir;
                if (this is DirectoryInfo)
                    demandDir = Directory.GetDemandDir(FullPath, true);
                else
                    demandDir = FullPath;
#if !FEATURE_CORECLR
                new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandDir).Demand();
#endif
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

            set {
                CreationTimeUtc = value.ToUniversalTime();
            }
        }

       [ComVisible(false)]
       public DateTime CreationTimeUtc {
           [System.Security.SecuritySafeCritical]
            get {
#if FEATURE_CORECLR
                // get_CreationTime also depends on this security check
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, FullPath);
                sourceState.EnsureState();
#endif
                if (_dataInitialised == -1) {
                    _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
                    Refresh();
                }

                if (_dataInitialised != 0) // Refresh was unable to initialise the data
                    __Error.WinIOError(_dataInitialised, DisplayPath);
                
                long fileTime = ((long)_data.ftCreationTimeHigh << 32) | _data.ftCreationTimeLow;
                return DateTime.FromFileTimeUtc(fileTime);
                
            }
        
            set {
                if (this is DirectoryInfo)
                    Directory.SetCreationTimeUtc(FullPath,value);
                else
                    File.SetCreationTimeUtc(FullPath,value);
                _dataInitialised = -1;
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
            [System.Security.SecuritySafeCritical]
            get {
#if FEATURE_CORECLR
                // get_LastAccessTime also depends on this security check
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, FullPath);
                sourceState.EnsureState();
#endif
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
                if (this is DirectoryInfo)
                    Directory.SetLastAccessTimeUtc(FullPath,value);
                else
                    File.SetLastAccessTimeUtc(FullPath,value);
                _dataInitialised = -1;
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
            [System.Security.SecuritySafeCritical]
            get {
#if FEATURE_CORECLR
                // get_LastWriteTime also depends on this security check
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, FullPath);
                sourceState.EnsureState();
#endif
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
                if (this is DirectoryInfo)
                    Directory.SetLastWriteTimeUtc(FullPath,value);
                else
                    File.SetLastWriteTimeUtc(FullPath,value);
                _dataInitialised = -1;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Refresh()
        {
            _dataInitialised = File.FillAttributeInfo(FullPath, ref _data, false, false);
        }

        public FileAttributes Attributes {
            [System.Security.SecuritySafeCritical]
            get
            {
#if FEATURE_CORECLR
                FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Read, String.Empty, FullPath);
                sourceState.EnsureState();
#endif
                if (_dataInitialised == -1) {
                    _data = new Win32Native.WIN32_FILE_ATTRIBUTE_DATA();
                    Refresh(); // Call refresh to intialise the data
                }

                if (_dataInitialised != 0) // Refresh was unable to initialise the data
                    __Error.WinIOError(_dataInitialised, DisplayPath);

                return (FileAttributes) _data.fileAttributes;
            }
            #if FEATURE_CORECLR
            [System.Security.SecurityCritical] // auto-generated
            #else
            [System.Security.SecuritySafeCritical]
            #endif
            set {
#if !FEATURE_CORECLR
                new FileIOPermission(FileIOPermissionAccess.Write, FullPath).Demand();
#endif
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

        [System.Security.SecurityCritical]  // auto-generated_required
        [ComVisible(false)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
#if !FEATURE_CORECLR
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, FullPath).Demand();
#endif

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
