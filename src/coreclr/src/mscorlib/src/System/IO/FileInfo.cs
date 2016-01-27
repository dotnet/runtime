// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: A collection of methods for manipulating Files.
**
**          April 09,2000 (some design refactorization)
**
===========================================================*/

using System;
#if FEATURE_MACL
using System.Security.AccessControl;
#endif
using System.Security.Permissions;
using PermissionSet = System.Security.PermissionSet;
using Win32Native = Microsoft.Win32.Win32Native;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.Serialization;
using System.Globalization;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;

namespace System.IO {    
    // Class for creating FileStream objects, and some basic file management
    // routines such as Delete, etc.
    [Serializable]
    [ComVisible(true)]
    public sealed class FileInfo: FileSystemInfo
    {
        private String _name;

#if FEATURE_CORECLR
        // Migrating InheritanceDemands requires this default ctor, so we can annotate it.
#if FEATURE_CORESYSTEM
        [System.Security.SecurityCritical]
#else
        [System.Security.SecuritySafeCritical]
#endif //FEATURE_CORESYSTEM
        private FileInfo(){}

        [System.Security.SecurityCritical]
        public static FileInfo UnsafeCreateFileInfo(String fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            Contract.EndContractBlock();

            FileInfo fi = new FileInfo();
            fi.Init(fileName, false);
            return fi;
        }
#endif

        [System.Security.SecuritySafeCritical]
        public FileInfo(String fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            Contract.EndContractBlock();

#if FEATURE_LEGACYNETCF
            if(CompatibilitySwitches.IsAppEarlierThanWindowsPhone8)
            {
                System.Reflection.Assembly callingAssembly = System.Reflection.Assembly.GetCallingAssembly();
                if(callingAssembly != null && !callingAssembly.IsProfileAssembly)
                {
                    string caller = new System.Diagnostics.StackFrame(1).GetMethod().FullName;
                    string callee = System.Reflection.MethodBase.GetCurrentMethod().FullName;
                    throw new MethodAccessException(String.Format(
                        CultureInfo.CurrentCulture,
                        Environment.GetResourceString("Arg_MethodAccessException_WithCaller"),
                        caller,
                        callee));
                }
            }
#endif // FEATURE_LEGACYNETCF

            Init(fileName, true);
        }

        [System.Security.SecurityCritical]
        private void Init(String fileName, bool checkHost)
        {
            OriginalPath = fileName;
            // Must fully qualify the path for the security check
            String fullPath = Path.GetFullPathInternal(fileName);
#if FEATURE_CORECLR
            if (checkHost)
            {
                FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, fileName, fullPath);
                state.EnsureState();
            }
#else
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Read, fullPath, false, false);
#endif

            _name = Path.GetFileName(fileName);
            FullPath = fullPath;
            DisplayPath = GetDisplayPath(fileName);
        }

        private String GetDisplayPath(String originalPath)
        {
#if FEATURE_CORECLR
            return Path.GetFileName(originalPath);
#else
            return originalPath;
#endif

        }

        [System.Security.SecurityCritical]  // auto-generated
        private FileInfo(SerializationInfo info, StreamingContext context) : base(info, context)
        {
#if !FEATURE_CORECLR
            new FileIOPermission(FileIOPermissionAccess.Read, new String[] { FullPath }, false, false).Demand();
#endif
            _name = Path.GetFileName(OriginalPath);
            DisplayPath = GetDisplayPath(OriginalPath);
        }

#if FEATURE_CORESYSTEM
        [System.Security.SecuritySafeCritical]
#endif //FEATURE_CORESYSTEM
        internal FileInfo(String fullPath, bool ignoreThis)
        {
            Contract.Assert(Path.GetRootLength(fullPath) > 0, "fullPath must be fully qualified!");
            _name = Path.GetFileName(fullPath);
            OriginalPath = _name;
            FullPath = fullPath;
            DisplayPath = _name;
        }

        public override String Name {
            get { return _name; }
        }
    
   
        public long Length {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                if (_dataInitialised == -1)
                    Refresh();
                
                if (_dataInitialised != 0) // Refresh was unable to initialise the data
                    __Error.WinIOError(_dataInitialised, DisplayPath);
        
                if ((_data.fileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY) != 0)
                    __Error.WinIOError(Win32Native.ERROR_FILE_NOT_FOUND, DisplayPath);
                
                return ((long)_data.fileSizeHigh) << 32 | ((long)_data.fileSizeLow & 0xFFFFFFFFL);
            }
        }

        /* Returns the name of the directory that the file is in */
        public String DirectoryName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                String directoryName = Path.GetDirectoryName(FullPath);
                if (directoryName != null)
                {
#if FEATURE_CORECLR
                    FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Read, DisplayPath, FullPath);
                    state.EnsureState();
#else
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, new String[] { directoryName }, false, false).Demand();
#endif
                }
                return directoryName;
            }
        }

        /* Creates an instance of the the parent directory */
        public DirectoryInfo Directory
        {
            get
            {
                String dirName = DirectoryName;
                if (dirName == null)
                    return null;
                return new DirectoryInfo(dirName);    
            }
        } 

        public bool IsReadOnly {
            get {
                return (Attributes & FileAttributes.ReadOnly) != 0;
            }
            set {
                if (value)
                    Attributes |= FileAttributes.ReadOnly;
                else
                    Attributes &= ~FileAttributes.ReadOnly;
            }
        }

#if FEATURE_MACL
        public FileSecurity GetAccessControl()
        {
            return File.GetAccessControl(FullPath, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        public FileSecurity GetAccessControl(AccessControlSections includeSections)
        {
            return File.GetAccessControl(FullPath, includeSections);
        }

        public void SetAccessControl(FileSecurity fileSecurity)
        {
            File.SetAccessControl(FullPath, fileSecurity);
        }
#endif

        [System.Security.SecuritySafeCritical]  // auto-generated
        public StreamReader OpenText()
        {
            return new StreamReader(FullPath, Encoding.UTF8, true, StreamReader.DefaultBufferSize, false);
        }

        public StreamWriter CreateText()
        {
            return new StreamWriter(FullPath,false);
        }

        public StreamWriter AppendText()
        {
            return new StreamWriter(FullPath,true);
        }

        
        // Copies an existing file to a new file. An exception is raised if the
        // destination file already exists. Use the 
        // Copy(String, String, boolean) method to allow 
        // overwriting an existing file.
        //
        // The caller must have certain FileIOPermissions.  The caller must have
        // Read permission to sourceFileName 
        // and Write permissions to destFileName.
        // 
        public FileInfo CopyTo(String destFileName) {
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
            Contract.EndContractBlock();

            destFileName = File.InternalCopy(FullPath, destFileName, false, true);
            return new FileInfo(destFileName, false);
        }


        // Copies an existing file to a new file. If overwrite is 
        // false, then an IOException is thrown if the destination file 
        // already exists.  If overwrite is true, the file is 
        // overwritten.
        //
        // The caller must have certain FileIOPermissions.  The caller must have
        // Read permission to sourceFileName and Create
        // and Write permissions to destFileName.
        // 
        public FileInfo CopyTo(String destFileName, bool overwrite) {
            if (destFileName == null)
                throw new ArgumentNullException("destFileName", Environment.GetResourceString("ArgumentNull_FileName"));
            if (destFileName.Length == 0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
            Contract.EndContractBlock();

            destFileName = File.InternalCopy(FullPath, destFileName, overwrite, true);
            return new FileInfo(destFileName, false);
        }

        public FileStream Create() {
            return File.Create(FullPath);
        }

        // Deletes a file. The file specified by the designated path is deleted. 
        // If the file does not exist, Delete succeeds without throwing
        // an exception.
        // 
        // On NT, Delete will fail for a file that is open for normal I/O
        // or a file that is memory mapped.  On Win95, the file will be 
        // deleted irregardless of whether the file is being used.
        // 
        // Your application must have Delete permission to the target file.
        // 
        [System.Security.SecuritySafeCritical]
        public override void Delete()
        {
#if FEATURE_CORECLR
            FileSecurityState state = new FileSecurityState(FileSecurityStateAccess.Write, DisplayPath, FullPath);
            state.EnsureState();
#else
            // For security check, path should be resolved to an absolute path.
            new FileIOPermission(FileIOPermissionAccess.Write, new String[] { FullPath }, false, false).Demand();
#endif

            bool r = Win32Native.DeleteFile(FullPath);
            if (!r) {
                int hr = Marshal.GetLastWin32Error();
                if (hr==Win32Native.ERROR_FILE_NOT_FOUND)
                    return;
                else
                    __Error.WinIOError(hr, DisplayPath);
            }
        }

        [ComVisible(false)]
        public void Decrypt()
        {
            File.Decrypt(FullPath);
        }

        [ComVisible(false)]
        public void Encrypt()
        {
            File.Encrypt(FullPath);
        }

        // Tests if the given file exists. The result is true if the file
        // given by the specified path exists; otherwise, the result is
        // false.  
        //
        // Your application must have Read permission for the target directory.
        public override bool Exists {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                try {
                    if (_dataInitialised == -1)
                        Refresh();
                    if (_dataInitialised != 0) {
                        // Refresh was unable to initialise the data.
                        // We should normally be throwing an exception here, 
                        // but Exists is supposed to return true or false.
                        return false;
                    }
                    return (_data.fileAttributes & Win32Native.FILE_ATTRIBUTE_DIRECTORY) == 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        
      
      
        // User must explicitly specify opening a new file or appending to one.
        public FileStream Open(FileMode mode) {
            return Open(mode, FileAccess.ReadWrite, FileShare.None);
        }

        public FileStream Open(FileMode mode, FileAccess access) {
            return Open(mode, access, FileShare.None);
        }

        public FileStream Open(FileMode mode, FileAccess access, FileShare share) {
            return new FileStream(FullPath, mode, access, share);
        }

        
#if FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]  // auto-generated
#endif
        public FileStream OpenRead()
        {
            return new FileStream(FullPath, FileMode.Open, FileAccess.Read,
                                  FileShare.Read, 4096, false);
        }


        public FileStream OpenWrite() {
            return new FileStream(FullPath, FileMode.OpenOrCreate, 
                                  FileAccess.Write, FileShare.None);
        }

      

       
        

        // Moves a given file to a new location and potentially a new file name.
        // This method does work across volumes.
        //
        // The caller must have certain FileIOPermissions.  The caller must
        // have Read and Write permission to 
        // sourceFileName and Write 
        // permissions to destFileName.
        // 
        [System.Security.SecuritySafeCritical]
        public void MoveTo(String destFileName) {
            if (destFileName==null)
                throw new ArgumentNullException("destFileName");
            if (destFileName.Length==0)
                throw new ArgumentException(Environment.GetResourceString("Argument_EmptyFileName"), "destFileName");
            Contract.EndContractBlock();

            String fullDestFileName = Path.GetFullPathInternal(destFileName);
#if FEATURE_CORECLR
            FileSecurityState sourceState = new FileSecurityState(FileSecurityStateAccess.Write | FileSecurityStateAccess.Read, DisplayPath, FullPath);
            FileSecurityState destState = new FileSecurityState(FileSecurityStateAccess.Write, destFileName, fullDestFileName);
            sourceState.EnsureState();
            destState.EnsureState();
#else
            new FileIOPermission(FileIOPermissionAccess.Write | FileIOPermissionAccess.Read, new String[] { FullPath }, false, false).Demand();
            FileIOPermission.QuickDemand(FileIOPermissionAccess.Write, fullDestFileName, false, false);
#endif
       
            if (!Win32Native.MoveFile(FullPath, fullDestFileName))
                __Error.WinIOError();
            FullPath = fullDestFileName;
            OriginalPath = destFileName;
            _name = Path.GetFileName(fullDestFileName);
            DisplayPath = GetDisplayPath(destFileName);
            // Flush any cached information about the file.
            _dataInitialised = -1;
        }

        [ComVisible(false)]
        public FileInfo Replace(String destinationFileName, String destinationBackupFileName)
        {
            return Replace(destinationFileName, destinationBackupFileName, false);
        }

        [ComVisible(false)]
        public FileInfo Replace(String destinationFileName, String destinationBackupFileName, bool ignoreMetadataErrors)
        {
            File.Replace(FullPath, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
            return new FileInfo(destinationFileName);
        }

        // Returns the display path
        public override String ToString()
        {
            return DisplayPath;
        }
    }
}
