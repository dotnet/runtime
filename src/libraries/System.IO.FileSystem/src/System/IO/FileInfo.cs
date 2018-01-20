// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;

namespace System.IO
{
    // Class for creating FileStream objects, and some basic file management
    // routines such as Delete, etc.
    public sealed partial class FileInfo : FileSystemInfo
    {
        private string _name;

        private FileInfo() { }

        public FileInfo(string fileName)
            : this(fileName, isNormalized: false)
        {
        }

        internal FileInfo(string originalPath, string fullPath = null, string fileName = null, bool isNormalized = false)
        {
            // Want to throw the original argument name
            OriginalPath = originalPath ?? throw new ArgumentNullException("fileName");

            fullPath = fullPath ?? originalPath;
            Debug.Assert(!isNormalized || !PathInternal.IsPartiallyQualified(fullPath), "should be fully qualified if normalized");

            FullPath = isNormalized ? fullPath ?? originalPath : Path.GetFullPath(fullPath);
            _name = fileName ?? Path.GetFileName(originalPath);
            DisplayPath = originalPath;
        }

        public override string Name
        {
            get { return _name; }
        }

        public long Length
        {
            get
            {
                if ((Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, DisplayPath), DisplayPath);
                }
                return LengthCore;
            }
        }

        /* Returns the name of the directory that the file is in */
        public string DirectoryName
        {
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
                string dirName = DirectoryName;
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

        public StreamReader OpenText()
        {
            return new StreamReader(FullPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        }

        public StreamWriter CreateText()
        {
            return new StreamWriter(FullPath, append: false);
        }

        public StreamWriter AppendText()
        {
            return new StreamWriter(FullPath, append: true);
        }


        // Copies an existing file to a new file. An exception is raised if the
        // destination file already exists. Use the 
        // Copy(string, string, boolean) method to allow 
        // overwriting an existing file.
        //
        // The caller must have certain FileIOPermissions.  The caller must have
        // Read permission to sourceFileName 
        // and Write permissions to destFileName.
        // 
        public FileInfo CopyTo(string destFileName)
        {
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName), SR.ArgumentNull_FileName);
            if (destFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destFileName));

            return new FileInfo(File.InternalCopy(FullPath, destFileName, false), isNormalized: true);
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
        public FileInfo CopyTo(string destFileName, bool overwrite)
        {
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName), SR.ArgumentNull_FileName);
            if (destFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destFileName));

            return new FileInfo(File.InternalCopy(FullPath, destFileName, overwrite), isNormalized: true);
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
        public override void Delete()
        {
            FileSystem.DeleteFile(FullPath);
        }

        // Tests if the given file exists. The result is true if the file
        // given by the specified path exists; otherwise, the result is
        // false.  
        //
        // Your application must have Read permission for the target directory.
        public override bool Exists
        {
            get
            {
                try
                {
                    return ExistsCore;
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
        public void MoveTo(string destFileName)
        {
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName));
            if (destFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destFileName));

            string fullDestFileName = Path.GetFullPath(destFileName);

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

            FileSystem.MoveFile(FullPath, fullDestFileName);

            FullPath = fullDestFileName;
            OriginalPath = destFileName;
            _name = Path.GetFileName(fullDestFileName);
            DisplayPath = destFileName;
            // Flush any cached information about the file.
            Invalidate();
        }

        public FileInfo Replace(string destinationFileName, string destinationBackupFileName)
        {
            return Replace(destinationFileName, destinationBackupFileName, ignoreMetadataErrors: false);
        }

        public FileInfo Replace(string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors)
        {
            File.Replace(FullPath, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);
            return new FileInfo(destinationFileName);
        }

        // Returns the display path
        public override string ToString()
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
