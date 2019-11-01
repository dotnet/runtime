// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace System.IO
{
    internal class StorageFolderMock : IStorageFolder
    {
        public IAsyncOperation<StorageFile> CreateFileAsync(string desiredName)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<StorageFile> CreateFileAsync(string desiredName, CreationCollisionOption options)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<StorageFolder> CreateFolderAsync(string desiredName)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<StorageFolder> CreateFolderAsync(string desiredName, CreationCollisionOption options)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<StorageFile> GetFileAsync(string name)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<StorageFolder> GetFolderAsync(string name)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<IStorageItem> GetItemAsync(string name)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<IReadOnlyList<StorageFile>> GetFilesAsync()
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<IReadOnlyList<StorageFolder>> GetFoldersAsync()
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<IReadOnlyList<IStorageItem>> GetItemsAsync()
        {
            throw new NotImplementedException();
        }

        public IAsyncAction RenameAsync(string desiredName)
        {
            throw new NotImplementedException();
        }

        public IAsyncAction RenameAsync(string desiredName, NameCollisionOption option)
        {
            throw new NotImplementedException();
        }

        public IAsyncAction DeleteAsync()
        {
            throw new NotImplementedException();
        }

        public IAsyncAction DeleteAsync(StorageDeleteOption option)
        {
            throw new NotImplementedException();
        }

        public IAsyncOperation<BasicProperties> GetBasicPropertiesAsync()
        {
            throw new NotImplementedException();
        }

        public bool IsOfType(StorageItemTypes type)
        {
            throw new NotImplementedException();
        }

        public global::Windows.Storage.FileAttributes Attributes => throw new NotImplementedException();

        public DateTimeOffset DateCreated => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public string Path => throw new NotImplementedException();
    }
}
