// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.Versioning;

#if MS_IO_REDIST
namespace Microsoft.IO
#else
namespace System.IO
#endif
{
    // Class for creating FileStream objects, and some basic file management
    // routines such as Delete, etc.
    public sealed partial class FileInfo : FileSystemInfo
    {
        private FileInfo() { }

        public FileInfo(string fileName)
            : this(fileName, isNormalized: false)
        {
        }

        internal FileInfo(string originalPath, string? fullPath = null, string? fileName = null, bool isNormalized = false)
        {
            // Want to throw the original argument name
            OriginalPath = originalPath ?? throw new ArgumentNullException(nameof(fileName));

            fullPath = fullPath ?? originalPath;
            Debug.Assert(!isNormalized || !PathInternal.IsPartiallyQualified(fullPath.AsSpan()), "should be fully qualified if normalized");

            FullPath = isNormalized ? fullPath ?? originalPath : Path.GetFullPath(fullPath);
            _name = fileName ?? Path.GetFileName(originalPath);
        }

        public long Length
        {
            get
            {
                if ((Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, FullPath), FullPath);
                }
                return LengthCore;
            }
        }

        public string? DirectoryName => Path.GetDirectoryName(FullPath);

        public DirectoryInfo? Directory
        {
            get
            {
                string? dirName = DirectoryName;
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
            => new StreamReader(NormalizedPath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        public StreamWriter CreateText()
            => new StreamWriter(NormalizedPath, append: false);

        public StreamWriter AppendText()
            => new StreamWriter(NormalizedPath, append: true);

        public FileInfo CopyTo(string destFileName) => CopyTo(destFileName, overwrite: false);

        public FileInfo CopyTo(string destFileName, bool overwrite)
        {
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName), SR.ArgumentNull_FileName);
            if (destFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destFileName));

            string destinationPath = Path.GetFullPath(destFileName);
            FileSystem.CopyFile(FullPath, destinationPath, overwrite);
            return new FileInfo(destinationPath, isNormalized: true);
        }

        public FileStream Create()
        {
            FileStream fileStream = File.Create(NormalizedPath);
            Invalidate();
            return fileStream;
        }

        public override void Delete()
        {
            FileSystem.DeleteFile(FullPath);
            Invalidate();
        }

        public FileStream Open(FileMode mode)
            => Open(mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.None);

        public FileStream Open(FileMode mode, FileAccess access)
            => Open(mode, access, FileShare.None);

        public FileStream Open(FileMode mode, FileAccess access, FileShare share)
            => new FileStream(NormalizedPath, mode, access, share);

        public FileStream OpenRead()
            => new FileStream(NormalizedPath, FileMode.Open, FileAccess.Read, FileShare.Read, File.DefaultBufferSize, false);

        public FileStream OpenWrite()
            => new FileStream(NormalizedPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);

        // Moves a given file to a new location and potentially a new file name.
        // This method does work across volumes.
        public void MoveTo(string destFileName)
        {
            MoveTo(destFileName, false);
        }

        // Moves a given file to a new location and potentially a new file name.
        // Optionally overwrites existing file.
        // This method does work across volumes.
        public void MoveTo(string destFileName, bool overwrite)
        {
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName));
            if (destFileName.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyFileName, nameof(destFileName));

            string fullDestFileName = Path.GetFullPath(destFileName);

            // These checks are in place to ensure Unix error throwing happens the same way
            // as it does on Windows.These checks can be removed if a solution to
            // https://github.com/dotnet/runtime/issues/14885 is found that doesn't require
            // validity checks before making an API call.
            if (!new DirectoryInfo(Path.GetDirectoryName(FullName)!).Exists)
                throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, FullName));

            if (!Exists)
                throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, FullName), FullName);

            FileSystem.MoveFile(FullPath, fullDestFileName, overwrite);

            FullPath = fullDestFileName;
            OriginalPath = destFileName;
            _name = Path.GetFileName(fullDestFileName);

            // Flush any cached information about the file.
            Invalidate();
        }

        public FileInfo Replace(string destinationFileName, string? destinationBackupFileName)
            => Replace(destinationFileName, destinationBackupFileName, ignoreMetadataErrors: false);

        public FileInfo Replace(string destinationFileName, string? destinationBackupFileName, bool ignoreMetadataErrors)
        {
            if (destinationFileName == null)
                throw new ArgumentNullException(nameof(destinationFileName));

            FileSystem.ReplaceFile(
                FullPath,
                Path.GetFullPath(destinationFileName),
                destinationBackupFileName != null ? Path.GetFullPath(destinationBackupFileName) : null,
                ignoreMetadataErrors);

            return new FileInfo(destinationFileName);
        }

        [SupportedOSPlatform("windows")]
        public void Decrypt() => File.Decrypt(FullPath);

        [SupportedOSPlatform("windows")]
        public void Encrypt() => File.Encrypt(FullPath);
    }
}
