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

        public override string GetCurrentDirectory()
        {
            throw new PlatformNotSupportedException(); // https://github.com/dotnet/corefx/issues/17470;
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
            if (string.Equals(sourceParent, destParent, StringComparison.OrdinalIgnoreCase))
            {
                // not the same subfolder
                if (!string.Equals(sourceFolderName, destFolderName, StringComparison.OrdinalIgnoreCase))
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

        public override void SetCurrentDirectory(string fullPath)
        {
            throw new PlatformNotSupportedException(); // https://github.com/dotnet/corefx/issues/17470
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
