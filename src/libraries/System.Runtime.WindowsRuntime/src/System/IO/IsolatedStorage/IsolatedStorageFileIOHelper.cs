// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;
using Windows.Storage;

namespace System.IO.IsolatedStorage
{
    [FriendAccessAllowed]
    internal class IsolatedStorageFileIOHelper : IsolatedStorageFileIOHelperBase
    {
        internal override void UnsafeMoveFile(string sourceFileFullPath, string destinationFileFullPath)
        {
            if (Directory.Exists(destinationFileFullPath))
            {
                string destinationFileName = Path.GetFileName(sourceFileFullPath);
                Task.Run(() => { UnsafeMoveFileAsync(sourceFileFullPath, destinationFileFullPath, destinationFileName).Wait(); }).Wait();
            }
            else
            {
                string destinationFileName = Path.GetFileName(destinationFileFullPath);
                string destinationDirectoryFullPath = Path.GetDirectoryName(destinationFileFullPath);
                Task.Run(() => { UnsafeMoveFileAsync(sourceFileFullPath, destinationDirectoryFullPath, destinationFileName).Wait(); }).Wait();
            }
        }

        private static async Task UnsafeMoveFileAsync(string sourceFileFullPath, string destinationFolderFullPath, string destinationFileName)
        {
            StorageFile sourceStorageFile = await StorageFile.GetFileFromPathAsync(sourceFileFullPath).ConfigureAwait(false);
            StorageFolder destinationStorageFolder = await StorageFolder.GetFolderFromPathAsync(destinationFolderFullPath).ConfigureAwait(false);
            await sourceStorageFile.MoveAsync(destinationStorageFolder, destinationFileName, NameCollisionOption.FailIfExists).ConfigureAwait(false);
        }
    }
}
