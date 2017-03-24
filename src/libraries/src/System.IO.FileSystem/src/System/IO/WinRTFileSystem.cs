// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.FileProperties;
using Windows.UI.Core;
using WinRTFileAttributes = Windows.Storage.FileAttributes;

namespace System.IO
{
    internal sealed partial class WinRTFileSystem : FileSystem
    {
        public override int MaxPath { get { return Interop.Kernel32.MAX_PATH; } }
        public override int MaxDirectoryPath { get { return Interop.Kernel32.MAX_DIRECTORY_PATH; } }

        private static System.IO.FileAttributes ConvertFileAttributes(WinRTFileAttributes fileAttributes)
        {
            //namespace Windows.Storage
            //{
            //    [Flags]
            //    public enum FileAttributes
            //    {
            //        Normal = 0,
            //        ReadOnly = 1,
            //        Directory = 16,
            //        Archive = 32,
            //        Temporary = 256,
            //        LocallyIncomplete = 512,
            //    }
            //}

            //namespace System.IO
            //{
            //    [Flags]
            //    public enum FileAttributes
            //    {
            //        ReadOnly = 1,
            //        Hidden = 2,
            //        System = 4,
            //        Directory = 16,
            //        Archive = 32,
            //        Device = 64,
            //        Normal = 128,
            //        Temporary = 256,
            //        SparseFile = 512,
            //        ReparsePoint = 1024,
            //        Compressed = 2048,
            //        Offline = 4096,
            //        NotContentIndexed = 8192,
            //        Encrypted = 16384,
            //    }
            //}

            // Normal is a special case and happens to have different values in WinRT and Win32.
            // It's meant to indicate the absence of other flags.  On WinRT this logically is 0,
            // however on Win32 it is represented with a discrete value of 128.
            return (fileAttributes == WinRTFileAttributes.Normal) ?
                FileAttributes.Normal :
                (FileAttributes)fileAttributes;
        }

        private static WinRTFileAttributes ConvertFileAttributes(FileAttributes fileAttributes)
        {
            // see comment above
            // Here we make sure to remove the "normal" value since it is redundant
            // We do not mask unsupported values 
            return (fileAttributes == FileAttributes.Normal) ?
                WinRTFileAttributes.Normal :
                (WinRTFileAttributes)(fileAttributes & ~FileAttributes.Normal);
        }

        public override void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            EnsureBackgroundThread();
            SynchronousResultOf(CopyFileAsync(sourceFullPath, destFullPath, overwrite));
        }

        private async Task CopyFileAsync(string sourceFullPath, string destFullPath, bool overwrite)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(sourceFullPath).TranslateWinRTTask(sourceFullPath);

            string destDirectory, destFileName;
            PathHelpers.SplitDirectoryFile(destFullPath, out destDirectory, out destFileName);

            StorageFolder destFolder = await StorageFolder.GetFolderFromPathAsync(destDirectory).TranslateWinRTTask(destDirectory, isDirectory: true);

            await file.CopyAsync(destFolder, destFileName, overwrite ? NameCollisionOption.ReplaceExisting : NameCollisionOption.FailIfExists).TranslateWinRTTask(sourceFullPath);
        }

        public override void ReplaceFile(string sourceFullPath, string destFullPath, string destBackupFullPath, bool ignoreMetadataErrors)
        {
            EnsureBackgroundThread();
            SynchronousResultOf(ReplaceFileAsync(sourceFullPath, destFullPath, destBackupFullPath, ignoreMetadataErrors));
        }

        private async Task ReplaceFileAsync(string sourceFullPath, string destFullPath, string destBackupFullPath, bool ignoreMetadataErrors)
        {
            // Copy the destination file to a backup.
            if (destBackupFullPath != null)
            {
                await CopyFileAsync(destFullPath, destBackupFullPath, overwrite: true).ConfigureAwait(false);
            }

            // Then copy the contents of the source file to the destination file.
            await CopyFileAsync(sourceFullPath, destFullPath, overwrite: true).ConfigureAwait(false);

            // Finally, delete the source file.
            await DeleteFileAsync(sourceFullPath).ConfigureAwait(false);
        }

        public override void CreateDirectory(string fullPath)
        {
            EnsureBackgroundThread();
            SynchronousResultOf(CreateDirectoryAsync(fullPath, failIfExists: false));
        }

        private async Task<StorageFolder> CreateDirectoryAsync(string fullPath, bool failIfExists)
        {
            if (fullPath.Length >= Interop.Kernel32.MAX_DIRECTORY_PATH)
                throw new PathTooLongException(SR.IO_PathTooLong);

            Stack<string> stackDir = new Stack<string>();
            StorageFolder workingFolder = null;
            string workingPath = fullPath;

            // walk up the path until we can get a directory
            while (workingFolder == null)
            {
                try
                {
                    workingFolder = await StorageFolder.GetFolderFromPathAsync(workingPath).TranslateWinRTTask(workingPath, isDirectory: true);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }

                if (workingFolder == null)
                {
                    // we couldn't get the directory, we'll need to create it
                    string folderName = null;
                    PathHelpers.SplitDirectoryFile(workingPath, out workingPath, out folderName);

                    if (String.IsNullOrEmpty(folderName))
                    {
                        // we reached the root and it did not exist.  we can't create roots.
                        throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_PATH_NOT_FOUND, workingPath);
                    }

                    stackDir.Push(folderName);
                    Debug.Assert(!String.IsNullOrEmpty(workingPath));
                }
            }

            Debug.Assert(workingFolder != null);

            if (failIfExists && (stackDir.Count == 0))
                throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_ALREADY_EXISTS, fullPath);

            // we have work to do.  if stackDir is empty it means we were passed a path to an existing directory.
            while (stackDir.Count > 0)
            {
                // use CreationCollisionOption.OpenIfExists to address a race conditions when creating directories
                workingFolder = await workingFolder.CreateFolderAsync(stackDir.Pop(), CreationCollisionOption.OpenIfExists).TranslateWinRTTask(fullPath, isDirectory: true);
            }

            return workingFolder;
        }

        public override void DeleteFile(string fullPath)
        {
            EnsureBackgroundThread();
            SynchronousResultOf(DeleteFileAsync(fullPath));
        }

        private async Task DeleteFileAsync(string fullPath)
        {
            try
            {
                // Note the absence of TranslateWinRTTask, we translate below in the catch block.
                StorageFile file = await StorageFile.GetFileFromPathAsync(fullPath).AsTask().ConfigureAwait(false);
                await file.DeleteAsync(StorageDeleteOption.PermanentDelete).AsTask().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                // For consistency with Win32 we ignore missing files
                if (exception.HResult != HResults.ERROR_FILE_NOT_FOUND)
                    throw exception.TranslateWinRTException(fullPath);
            }
        }

        public override bool DirectoryExists(string fullPath)
        {
            EnsureBackgroundThread();
            return SynchronousResultOf(DirectoryExistsAsync(fullPath));
        }

        private async Task<bool> DirectoryExistsAsync(string fullPath)
        {
            string directoryPath = null, fileName = null;
            PathHelpers.SplitDirectoryFile(fullPath, out directoryPath, out fileName);

            // Rather than call await StorageFolder.GetFolderFromPathAsync(fullPath); and catch FileNotFoundException
            // we try to avoid the exception by calling TryGetItemAsync.
            // We can still hit an exception if the parent directory doesn't exist but it does provide better performance
            // for the existing parent/non-existing directory case and avoids a first chance exception which is a developer
            // pain point.

            StorageFolder parent = null;
            try
            {
                parent = await StorageFolder.GetFolderFromPathAsync(directoryPath).TranslateWinRTTask(directoryPath, isDirectory: true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            if (String.IsNullOrEmpty(fileName))
            {
                // we were given a root
                return parent != null;
            }

            if (parent != null)
            {
                StorageFolder folder = await parent.TryGetItemAsync(fileName).TranslateWinRTTask(fullPath, isDirectory: true) as StorageFolder;
                return folder != null;
            }
            else
            {
                // it's possible we don't have access to the parent but do have access to this folder
                try
                {
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(fullPath).TranslateWinRTTask(fullPath, isDirectory: true);
                    return folder != null;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            return false;
        }


        public override IEnumerable<string> EnumeratePaths(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            IReadOnlyList<IStorageItem> storageFiles = SynchronousResultOf(EnumerateFileQuery(fullPath, searchPattern, searchOption, searchTarget));
            return IteratePathsFromStorageItems(storageFiles);
        }

        public override IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            IReadOnlyList<IStorageItem> storageFiles = SynchronousResultOf(EnumerateFileQuery(fullPath, searchPattern, searchOption, searchTarget));
            return IterateFileSystemInfosFromStorageItems(storageFiles);
        }

        /// <summary>
        /// Translates IStorageItems into FileSystemInfos and yields the results.
        /// </summary>
        private static IEnumerable<FileSystemInfo> IterateFileSystemInfosFromStorageItems(IReadOnlyList<IStorageItem> storageFiles)
        {
            int count = storageFiles.Count;
            for (int i = 0; i < count; i++)
            {
                if (storageFiles[i].IsOfType(StorageItemTypes.Folder))
                {
                    yield return new DirectoryInfo(storageFiles[i].Path);
                }
                else // If it is neither a File nor folder then we treat it as a File.
                {
                    yield return new FileInfo(storageFiles[i].Path);
                }
            }
        }

        /// <summary>
        /// Translates IStorageItems into string paths and yields the results.
        /// </summary>
        private static IEnumerable<string> IteratePathsFromStorageItems(IReadOnlyList<IStorageItem> storageFiles)
        {
            int count = storageFiles.Count;
            for (int i = 0; i < count; i++)
            {
                yield return storageFiles[i].Path;
            }
        }

        private async static Task<IReadOnlyList<IStorageItem>> EnumerateFileQuery(string path, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            // Get a StorageFolder for "path"
            string fullPath = Path.GetFullPath(path);
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(fullPath).TranslateWinRTTask(fullPath, isDirectory: true);

            // Construct a query for the search.
            QueryOptions query = new QueryOptions();

            // Translate SearchOption into FolderDepth
            query.FolderDepth = searchOption == SearchOption.AllDirectories ? FolderDepth.Deep : FolderDepth.Shallow;

            // Construct an AQS filter
            string normalizedSearchPattern = PathHelpers.NormalizeSearchPattern(searchPattern);
            if (normalizedSearchPattern.Length == 0)
            {
                // An empty searchPattern will return no results and requires no AQS parsing.
                return new IStorageItem[0];
            }
            else
            {
                // Parse the query as an ItemPathDisplay filter.
                string searchPath = PathHelpers.GetFullSearchString(fullPath, normalizedSearchPattern);
                string aqs = "System.ItemPathDisplay:~\"" + searchPath + "\"";
                query.ApplicationSearchFilter = aqs;

                // If the filtered path is deeper than the given user path, we need to get a new folder for it.
                // This occurs when someone does something like Enumerate("C:\first\second\", "C:\first\second\third\*").
                // When AllDirectories is set this isn't an issue, but for TopDirectoryOnly we have to do some special work
                // to make sure something is actually returned when the searchPattern is a subdirectory of the path.
                // To do this, we attempt to get a new StorageFolder for the subdirectory and return an empty enumerable
                // if we can't.
                string searchPatternDirName = Path.GetDirectoryName(normalizedSearchPattern);
                string userPath = string.IsNullOrEmpty(searchPatternDirName) ? fullPath : Path.Combine(fullPath, searchPatternDirName);
                if (userPath != folder.Path)
                {
                    folder = await StorageFolder.GetFolderFromPathAsync(userPath).TranslateWinRTTask(userPath, isDirectory: true);
                }
            }

            // Execute our built query
            if (searchTarget == SearchTarget.Files)
            {
                StorageFileQueryResult queryResult = folder.CreateFileQueryWithOptions(query);
                return await queryResult.GetFilesAsync().TranslateWinRTTask(folder.Path, isDirectory: true);
            }
            else if (searchTarget == SearchTarget.Directories)
            {
                StorageFolderQueryResult queryResult = folder.CreateFolderQueryWithOptions(query);
                return await queryResult.GetFoldersAsync().TranslateWinRTTask(folder.Path, isDirectory: true);
            }
            else
            {
                StorageItemQueryResult queryResult = folder.CreateItemQueryWithOptions(query);
                return await queryResult.GetItemsAsync().TranslateWinRTTask(folder.Path, isDirectory: true);
            }
        }

        public override bool FileExists(string fullPath)
        {
            EnsureBackgroundThread();
            return SynchronousResultOf(FileExistsAsync(fullPath));
        }

        private async Task<bool> FileExistsAsync(string fullPath)
        {
            string directoryPath = null, fileName = null;
            PathHelpers.SplitDirectoryFile(fullPath, out directoryPath, out fileName);

            if (String.IsNullOrEmpty(fileName))
            {
                // No filename was provided
                return false;
            }

            // Rather than call await StorageFile.GetFileFromPathAsync(fullPath); and catch FileNotFoundException
            // we try to avoid the exception by calling TryGetItemAsync.
            // We can still hit an exception if the directory doesn't exist but it does provide better performance
            // for the existing folder/non-existing file case and avoids a first chance exception which is a developer
            // pain point.

            StorageFolder parent = null;
            try
            {
                parent = await StorageFolder.GetFolderFromPathAsync(directoryPath).TranslateWinRTTask(directoryPath);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            StorageFile file = null;

            if (parent != null)
            {
                // The expectation is that this API will never throw, thus it is missing TranslateWinRTTask
                file = await parent.TryGetItemAsync(fileName).TranslateWinRTTask(fullPath) as StorageFile;
            }
            else
            {
                // it's possible we don't have access to the parent but do have access to this file
                try
                {
                    file = await StorageFile.GetFileFromPathAsync(fullPath).TranslateWinRTTask(fullPath);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }

            return (file != null) ? file.IsAvailable : false;
        }

        public override FileAttributes GetAttributes(string fullPath)
        {
            EnsureBackgroundThread();
            return SynchronousResultOf(GetAttributesAsync(fullPath));
        }

        private async Task<FileAttributes> GetAttributesAsync(string fullPath)
        {
            IStorageItem item = await GetStorageItemAsync(fullPath).ConfigureAwait(false);

            return ConvertFileAttributes(item.Attributes);
        }

        public override DateTimeOffset GetCreationTime(string fullPath)
        {
            EnsureBackgroundThread();
            return SynchronousResultOf(GetCreationTimeAsync(fullPath));
        }

        private async Task<DateTimeOffset> GetCreationTimeAsync(string fullPath)
        {
            IStorageItem item = await GetStorageItemAsync(fullPath).ConfigureAwait(false);

            return item.DateCreated;
        }

        public override string GetCurrentDirectory()
        {
            throw new PlatformNotSupportedException();
        }

        public override IFileSystemObject GetFileSystemInfo(string fullPath, bool asDirectory)
        {
            return new WinRTFileSystemObject(fullPath, asDirectory);
        }

        public override DateTimeOffset GetLastAccessTime(string fullPath)
        {
            EnsureBackgroundThread();
            return SynchronousResultOf(GetLastAccessTimeAsync(fullPath));
        }

        private async Task<DateTimeOffset> GetLastAccessTimeAsync(string fullPath)
        {
            IStorageItem item = await GetStorageItemAsync(fullPath).ConfigureAwait(false);

            return await GetLastAccessTimeAsync(item).ConfigureAwait(false);
        }

        // declare a static to avoid unnecessary heap allocations
        private static readonly string[] s_dateAccessedKey = { "System.DateAccessed" };

        private static async Task<DateTimeOffset> GetLastAccessTimeAsync(IStorageItem item)
        {
            BasicProperties properties = await item.GetBasicPropertiesAsync().TranslateWinRTTask(item.Path);

            var propertyMap = await properties.RetrievePropertiesAsync(s_dateAccessedKey).TranslateWinRTTask(item.Path);

            // shell doesn't expose this metadata on all item types
            if (propertyMap.ContainsKey(s_dateAccessedKey[0]))
            {
                return (DateTimeOffset)propertyMap[s_dateAccessedKey[0]];
            }

            // fallback to modified date
            return properties.DateModified;
        }

        public override DateTimeOffset GetLastWriteTime(string fullPath)
        {
            EnsureBackgroundThread();
            return SynchronousResultOf(GetLastWriteTimeAsync(fullPath));
        }

        private async Task<DateTimeOffset> GetLastWriteTimeAsync(string fullPath)
        {
            IStorageItem item = await GetStorageItemAsync(fullPath).ConfigureAwait(false);

            return await GetLastWriteTimeAsync(item).ConfigureAwait(false);
        }

        private static async Task<DateTimeOffset> GetLastWriteTimeAsync(IStorageItem item)
        {
            BasicProperties properties = await item.GetBasicPropertiesAsync().TranslateWinRTTask(item.Path);

            return properties.DateModified;
        }

        private static async Task<IStorageItem> GetStorageItemAsync(string fullPath)
        {
            string directoryPath, itemName;
            PathHelpers.SplitDirectoryFile(fullPath, out directoryPath, out itemName);

            StorageFolder parent = await StorageFolder.GetFolderFromPathAsync(directoryPath).TranslateWinRTTask(directoryPath, isDirectory: true);

            if (String.IsNullOrEmpty(itemName))
                return parent;

            return await parent.GetItemAsync(itemName).TranslateWinRTTask(fullPath);
        }

        public override void MoveDirectory(string sourceFullPath, string destFullPath)
        {
            EnsureBackgroundThread();
            SynchronousResultOf(MoveDirectoryAsync(sourceFullPath, destFullPath));
        }

        private async Task MoveDirectoryAsync(string sourceFullPath, string destFullPath)
        {
            StorageFolder sourceFolder = await StorageFolder.GetFolderFromPathAsync(sourceFullPath).TranslateWinRTTask(sourceFullPath, isDirectory: true);

            // WinRT doesn't support move, only rename
            // If parents are the same, just rename.
            string sourceParent, sourceFolderName, destParent, destFolderName;
            PathHelpers.SplitDirectoryFile(sourceFullPath, out sourceParent, out sourceFolderName);
            PathHelpers.SplitDirectoryFile(destFullPath, out destParent, out destFolderName);

            // same parent folder
            if (String.Equals(sourceParent, destParent, StringComparison.OrdinalIgnoreCase))
            {
                // not the same subfolder
                if (!String.Equals(sourceFolderName, destFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    await sourceFolder.RenameAsync(destFolderName).TranslateWinRTTask(destFullPath, isDirectory: true);
                }
                // else : nothing to do
            }
            else
            {
                // Otherwise, create the destination and move each item recursively.
                // We could do a copy, which would be safer in case of a failure
                // We do a move because it has the perf characteristics that match the win32 move
                StorageFolder destFolder = await CreateDirectoryAsync(destFullPath, failIfExists: true).ConfigureAwait(false);

                await MoveDirectoryAsync(sourceFolder, destFolder).ConfigureAwait(false);
            }
        }

        private async Task MoveDirectoryAsync(StorageFolder sourceFolder, StorageFolder destFolder)
        {
            foreach (var sourceFile in await sourceFolder.GetFilesAsync().TranslateWinRTTask(sourceFolder.Path, isDirectory: true))
            {
                await sourceFile.MoveAsync(destFolder).TranslateWinRTTask(sourceFile.Path);
            }

            foreach (var sourceSubFolder in await sourceFolder.GetFoldersAsync().TranslateWinRTTask(sourceFolder.Path, isDirectory: true))
            {
                StorageFolder destSubFolder = await destFolder.CreateFolderAsync(sourceSubFolder.Name).TranslateWinRTTask(destFolder.Path, isDirectory: true);

                // Recursively move sub-directory
                await MoveDirectoryAsync(sourceSubFolder, destSubFolder).ConfigureAwait(false);
            }

            // sourceFolder should now be empty
            await sourceFolder.DeleteAsync(StorageDeleteOption.PermanentDelete).TranslateWinRTTask(sourceFolder.Path, isDirectory: true);
        }

        public override void MoveFile(string sourceFullPath, string destFullPath)
        {
            EnsureBackgroundThread();
            SynchronousResultOf(MoveFileAsync(sourceFullPath, destFullPath));
        }

        private async Task MoveFileAsync(string sourceFullPath, string destFullPath)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(sourceFullPath).TranslateWinRTTask(sourceFullPath);

            string destDirectory, destFileName;
            PathHelpers.SplitDirectoryFile(destFullPath, out destDirectory, out destFileName);

            // Win32 MoveFileEx will return success if source and destination are the same.
            // Comparison is safe here as the caller has normalized both paths.
            if (!sourceFullPath.Equals(destFullPath, StringComparison.OrdinalIgnoreCase))
            {
                StorageFolder destFolder = await StorageFolder.GetFolderFromPathAsync(destDirectory).TranslateWinRTTask(destDirectory, isDirectory: true);

                await file.MoveAsync(destFolder, destFileName, NameCollisionOption.FailIfExists).TranslateWinRTTask(sourceFullPath);
            }
        }

        public override FileStreamBase Open(string fullPath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, FileStream parent)
        {
            EnsureBackgroundThread();
            return SynchronousResultOf(OpenAsync(fullPath, mode, access, share, bufferSize, options, parent));
        }

        private async Task<FileStreamBase> OpenAsync(string fullPath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, FileStream parent)
        {
            // When trying to open the root directory, we need to throw an Access Denied
            if (PathInternal.GetRootLength(fullPath) == fullPath.Length)
                throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_ACCESS_DENIED, fullPath);

            // Win32 CreateFile returns ERROR_PATH_NOT_FOUND when given a path that ends with '\'
            if (PathHelpers.EndsInDirectorySeparator(fullPath))
                throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_PATH_NOT_FOUND, fullPath);

            StorageFile file = null;

            // FileMode
            if (mode == FileMode.Open || mode == FileMode.Truncate)
            {
                file = await StorageFile.GetFileFromPathAsync(fullPath).TranslateWinRTTask(fullPath);
            }
            else
            {
                CreationCollisionOption collisionOptions;

                switch (mode)
                {
                    case FileMode.Create:
                        collisionOptions = CreationCollisionOption.ReplaceExisting;
                        break;
                    case FileMode.CreateNew:
                        collisionOptions = CreationCollisionOption.FailIfExists;
                        break;
                    case FileMode.Append:
                    case FileMode.OpenOrCreate:
                    default:
                        collisionOptions = CreationCollisionOption.OpenIfExists;
                        break;
                }

                string directoryPath, fileName;
                PathHelpers.SplitDirectoryFile(fullPath, out directoryPath, out fileName);

                StorageFolder directory = await StorageFolder.GetFolderFromPathAsync(directoryPath).TranslateWinRTTask(directoryPath, isDirectory: true);

                file = await directory.CreateFileAsync(fileName, collisionOptions).TranslateWinRTTask(fullPath);
            }

            // FileAccess: WinRT doesn't support FileAccessMode.Write so we upgrade to ReadWrite
            FileAccessMode accessMode = ((access & FileAccess.Write) != 0) ? FileAccessMode.ReadWrite : FileAccessMode.Read;

            // FileShare: cannot translate StorageFile uses a different sharing model (oplocks) that is controlled via FileAccessMode

            // FileOptions: ignore most values of FileOptions as they are hints and are not supported by WinRT.
            // FileOptions.Encrypted is not a hint, and not supported by WinRT, but there is precedent for ignoring this (FAT).
            // FileOptions.DeleteOnClose should result in an UnauthorizedAccessException when
            //   opening a file that can only be read, but we cannot safely reproduce that behavior
            //   in WinRT without actually deleting the file.  
            //   Instead the failure will occur in the finalizer for WinRTFileStream and be ignored.

            // open our stream
            Stream stream = (await file.OpenAsync(accessMode).TranslateWinRTTask(fullPath)).AsStream(bufferSize);

            if (mode == FileMode.Append)
            {
                // seek to end.
                stream.Seek(0, SeekOrigin.End);
            }
            else if (mode == FileMode.Truncate)
            {
                // truncate stream to 0
                stream.SetLength(0);
            }

            return new WinRTFileStream(stream, file, access, options, parent);
        }

        public override void RemoveDirectory(string fullPath, bool recursive)
        {
            EnsureBackgroundThread();
            SynchronousResultOf(RemoveDirectoryAsync(fullPath, recursive));
        }

        private async Task RemoveDirectoryAsync(string fullPath, bool recursive)
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(fullPath).TranslateWinRTTask(fullPath, isDirectory: true);

            // StorageFolder.DeleteAsync will always be recursive.  Detect a non-empty folder up front and throw.
            if (!recursive && (await folder.GetItemsAsync()).Count != 0)
                throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_DIR_NOT_EMPTY, fullPath);

            // StorageFolder.Delete ignores readonly attribute.  Detect and throw.
            if ((folder.Attributes & WinRTFileAttributes.ReadOnly) == WinRTFileAttributes.ReadOnly)
                throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, fullPath));

            StorageFolder parentFolder = await folder.GetParentAsync().TranslateWinRTTask(fullPath, isDirectory: true);

            // StorageFolder.Delete throws on hidden directories but we cannot prevent it.
            await folder.DeleteAsync(StorageDeleteOption.PermanentDelete).TranslateWinRTTask(fullPath, isDirectory: true);

            // WinRT will ignore failures to delete in cases where files are in use.
            // Throw if the folder still remains after successful DeleteAsync
            if (null != await (parentFolder.TryGetItemAsync(folder.Name)))
                throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_DIR_NOT_EMPTY, fullPath);
        }

        public override void SetAttributes(string fullPath, FileAttributes attributes)
        {
            EnsureBackgroundThread();
            SynchronousResultOf(SetAttributesAsync(fullPath, attributes));
        }

        private async Task SetAttributesAsync(string fullPath, FileAttributes attributes)
        {
            IStorageItem item = await GetStorageItemAsync(fullPath).ConfigureAwait(false);

            await SetAttributesAsync(item, attributes).ConfigureAwait(false);
        }

        private static async Task SetAttributesAsync(IStorageItem item, FileAttributes attributes)
        {
            BasicProperties basicProperties = await item.GetBasicPropertiesAsync().TranslateWinRTTask(item.Path);

            // This works for only a subset of attributes, unsupported attributes are ignored.
            // We don't mask the attributes since WinRT just ignores the unsupported ones and flowing
            // them enables possible lightup in the future.
            var property = new KeyValuePair<string, object>("System.FileAttributes", (UInt32)ConvertFileAttributes(attributes));
            try
            {
                await basicProperties.SavePropertiesAsync(new[] { property }).AsTask().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                if (exception.HResult != HResults.ERROR_INVALID_PARAMETER)
                    throw new ArgumentException(SR.Arg_InvalidFileAttrs);

                throw exception.TranslateWinRTException(item.Path);
            }
        }

        public override void SetCreationTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            // intentionally noop : not supported
            // "System.DateCreated" property is readonly
        }

        public override void SetCurrentDirectory(string fullPath)
        {
            throw new PlatformNotSupportedException();
        }

        public override void SetLastAccessTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            // intentionally noop 
            // "System.DateAccessed" property is readonly
        }

        public override void SetLastWriteTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            // intentionally noop : not supported
            // "System.DateModified" property is readonly
        }

        public override string[] GetLogicalDrives()
        {
            return DriveInfoInternal.GetLogicalDrives();
        }

        #region Task Utility
        private static void EnsureBackgroundThread()
        {
            // WinRT async operations on brokered files require posting a message back to the UI thread.
            // If we were to run a sync method on the UI thread we'd deadlock.  Throw instead.

            if (IsUIThread())
                throw new InvalidOperationException(SR.IO_SyncOpOnUIThread);
        }

        private static bool IsUIThread()
        {
            CoreWindow window = CoreWindow.GetForCurrentThread();

            return window != null && window.Dispatcher != null && window.Dispatcher.HasThreadAccess;
        }

        private static void SynchronousResultOf(Task task)
        {
            WaitForTask(task);
        }

        private static TResult SynchronousResultOf<TResult>(Task<TResult> task)
        {
            WaitForTask(task);
            return task.Result;
        }

        // this needs to be separate from SynchronousResultOf so that SynchronousResultOf<T> can call it.
        private static void WaitForTask(Task task)
        {
            // This should never be called from the UI thread since it can deadlock
            // Throwing here, however, is too late since work has already been started
            // Instead we assert here so that our tests can catch cases where we forgot to
            // call EnsureBackgroundThread before starting the task.
            Debug.Assert(!IsUIThread());

            task.GetAwaiter().GetResult();
        }
        #endregion Task Utility
    }
}
