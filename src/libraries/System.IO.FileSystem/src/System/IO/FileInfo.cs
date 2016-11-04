// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Text;

namespace System.IO
{
    // Class for creating FileStream objects, and some basic file management
    // routines such as Delete, etc.
    [Serializable]
    public sealed partial class FileInfo : FileSystemInfo
    {
        private String _name;

        [System.Security.SecurityCritical]
        private FileInfo() { }

        [System.Security.SecuritySafeCritical]
        public FileInfo(String fileName)
        {
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            Contract.EndContractBlock();

            Init(fileName);
        }

        private FileInfo(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            _name = Path.GetFileName(OriginalPath);
            DisplayPath = GetDisplayPath(OriginalPath);
        }

        [System.Security.SecurityCritical]
        private void Init(String fileName)
        {
            OriginalPath = fileName;
            // Must fully qualify the path for the security check
            String fullPath = Path.GetFullPath(fileName);

            _name = Path.GetFileName(fileName);
            FullPath = fullPath;
            DisplayPath = GetDisplayPath(fileName);
        }

        private String GetDisplayPath(String originalPath)
        {
            return originalPath;
        }

        [System.Security.SecuritySafeCritical]
        internal FileInfo(String fullPath, String originalPath)
        {
            Debug.Assert(Path.IsPathRooted(fullPath), "fullPath must be fully qualified!");
            _name = originalPath ?? Path.GetFileName(fullPath);
            OriginalPath = _name;
            FullPath = fullPath;
            DisplayPath = _name;
        }

        public override String Name
        {
            get { return _name; }
        }


        public long Length
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if ((FileSystemObject.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, DisplayPath), DisplayPath);
                }
                return FileSystemObject.Length;
            }
        }

        /* Returns the name of the directory that the file is in */
        public String DirectoryName
        {
            [System.Security.SecuritySafeCritical]
            get
            {
                return Path.GetDirectoryName(FullPath);
            }
        }

        /* Creates an instance of the parent directory */
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

        public bool IsReadOnly
        {
            get
            {
                return (Attributes & FileAttributes.ReadOnly) != 0;
            }
            set
            {
                if (value)
                    Attributes |= FileAttributes.ReadOnly;
                else
                    Attributes &= ~FileAttributes.ReadOnly;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public StreamReader OpenText()
        {
            Stream stream = FileStream.InternalOpen(FullPath);
            return new StreamReader(stream, Encoding.UTF8, true);
        }

        public StreamWriter CreateText()
        {
            Stream stream = FileStream.InternalCreate(FullPath);
            return new StreamWriter(stream);
        }

        public StreamWriter AppendText()
        {
            Stream stream = FileStream.InternalAppend(FullPath);
            return new StreamWriter(stream);
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
        public FileInfo CopyTo(String destFileName)
        {
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName), SR.ArgumentNull_FileName);
            if (destFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destFileName));
            Contract.EndContractBlock();

            destFileName = File.InternalCopy(FullPath, destFileName, false);
            return new FileInfo(destFileName, null);
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
        public FileInfo CopyTo(String destFileName, bool overwrite)
        {
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName), SR.ArgumentNull_FileName);
            if (destFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destFileName));
            Contract.EndContractBlock();

            destFileName = File.InternalCopy(FullPath, destFileName, overwrite);
            return new FileInfo(destFileName, null);
        }

        public FileStream Create()
        {
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
            FileSystem.Current.DeleteFile(FullPath);
        }

        // Tests if the given file exists. The result is true if the file
        // given by the specified path exists; otherwise, the result is
        // false.  
        //
        // Your application must have Read permission for the target directory.
        public override bool Exists
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                try
                {
                    return FileSystemObject.Exists;
                }
                catch
                {
                    return false;
                }
            }
        }

        // User must explicitly specify opening a new file or appending to one.
        public FileStream Open(FileMode mode)
        {
            return Open(mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None);
        }

        public FileStream Open(FileMode mode, FileAccess access)
        {
            return Open(mode, access, FileShare.None);
        }

        public FileStream Open(FileMode mode, FileAccess access, FileShare share)
        {
            return new FileStream(FullPath, mode, access, share);
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        public FileStream OpenRead()
        {
            return new FileStream(FullPath, FileMode.Open, FileAccess.Read,
                                  FileShare.Read, 4096, false);
        }


        public FileStream OpenWrite()
        {
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
        public void MoveTo(String destFileName)
        {
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName));
            if (destFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destFileName));
            Contract.EndContractBlock();

            String fullDestFileName = Path.GetFullPath(destFileName);

            // These checks are in place to ensure Unix error throwing happens the same way
            // as it does on Windows.These checks can be removed if a solution to #2460 is
            // found that doesn't require validity checks before making an API call.
            if (!new DirectoryInfo(Path.GetDirectoryName(FullName)).Exists)
            {
                throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, FullName));
            }
            if (!Exists)
            {
                throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, FullName), FullName);
            }

            FileSystem.Current.MoveFile(FullPath, fullDestFileName);

            FullPath = fullDestFileName;
            OriginalPath = destFileName;
            _name = Path.GetFileName(fullDestFileName);
            DisplayPath = GetDisplayPath(destFileName);
            // Flush any cached information about the file.
            Invalidate();
        }

        // Returns the display path
        public override String ToString()
        {
            return DisplayPath;
        }

        public void Decrypt()
        {
            File.Decrypt(FullPath);
        }

        public void Encrypt()
        {
            File.Encrypt(FullPath);
        }

    }
}
