// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace System.IO
{
    partial class WinRTFileSystem
    {
        internal sealed class WinRTFileSystemObject : IFileSystemObject
        {
            private readonly bool _asDirectory;

            // Cache the file/directory information
            private readonly string _fullPath;

            // Cache the file information
            private IStorageItem _item;

            // Cache any error retrieving the file/directory information
            // We use this field in conjunction with the Refresh method which should never throw.
            // If we succeed this is null, on failure we capture the Exception so that we can
            // throw it when attempting to access the cached info later.
            private ExceptionDispatchInfo _initializationException;

            public WinRTFileSystemObject(string fullPath, bool asDirectory)
            {
                _asDirectory = asDirectory;
                _fullPath = fullPath;
                _item = null;
                _initializationException = null;
            }

            public FileAttributes Attributes
            {
                get
                {
                    EnsureItemInitialized();

                    if (_item == null)
                        return (FileAttributes)(-1);

                    return ConvertFileAttributes(_item.Attributes);
                }
                set
                {
                    EnsureItemExists();
                    EnsureBackgroundThread();

                    try
                    {
                        SynchronousResultOf(SetAttributesAsync(_item, value));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // For consistency with Win32 we remap ACCESS_DENIED to ArgumentException
                        // Intentionally omit argument name since this is mimicking the Win32 sourced ArgumentException
                        throw new ArgumentException(SR.UnauthorizedAccess_IODenied_NoPathName /*, intentionally omitted*/);
                    }
                    // reset our attributes
                    _item = null;
                }
            }

            public DateTimeOffset CreationTime
            {
                get
                {
                    EnsureItemInitialized();

                    if (_item == null)
                        return DateTimeOffset.FromFileTime(0);

                    return _item.DateCreated;
                }
                set
                {
                    EnsureItemExists();
                    // intentionally noop : not supported
                }
            }

            public bool Exists
            {
                get
                {
                    // Do not throw
                    if (_item == null)
                        Refresh();

                    if (_item == null)
                        return false;

                    return _item.IsOfType(_asDirectory ? StorageItemTypes.Folder : StorageItemTypes.File);
                }
            }

            public DateTimeOffset LastAccessTime
            {
                get
                {
                    EnsureItemInitialized();

                    if (_item == null)
                        return DateTimeOffset.FromFileTime(0);

                    EnsureBackgroundThread();
                    return SynchronousResultOf(GetLastAccessTimeAsync(_item));
                }
                set
                {
                    EnsureItemExists();
                    // intentionally noop : not supported
                }
            }

            public DateTimeOffset LastWriteTime
            {
                get
                {
                    EnsureItemInitialized();

                    if (_item == null)
                        return DateTimeOffset.FromFileTime(0);

                    EnsureBackgroundThread();
                    return SynchronousResultOf(GetLastWriteTimeAsync(_item));
                }
                set
                {
                    EnsureItemExists();
                    // intentionally noop : not supported
                }
            }

            public long Length
            {
                get
                {
                    EnsureItemExists();
                    EnsureBackgroundThread();
                    return (long)SynchronousResultOf(GetLengthAsync());
                }
            }

            private async Task<ulong> GetLengthAsync()
            {
                BasicProperties properties = await _item.GetBasicPropertiesAsync().TranslateWinRTTask(_fullPath, _asDirectory);
                return properties.Size;
            }

            // Consumes cached file/directory information and throw if any error occured retrieving 
            // it, including file not found.
            private void EnsureItemExists()
            {
                // If we've already tried and failed to get the item, throw
                if (_initializationException != null)
                    _initializationException.Throw();

                // We don't have the item, try and get it allowing any exception to be thrown
                if (_item == null)
                {
                    EnsureBackgroundThread();
                    _item = SynchronousResultOf(GetStorageItemAsync(_fullPath));
                }
            }

            private void EnsureItemInitialized()
            {
                // Refresh only if we haven't already done so once.
                if (_item == null && _initializationException == null)
                {
                    // Refresh will ignore file-not-found errors.
                    Refresh();
                }

                // Refresh was unable to initialize the data
                if (_initializationException != null)
                    _initializationException.Throw();
            }

            public void Refresh()
            {
                EnsureBackgroundThread();
                SynchronousResultOf(RefreshAsync());
            }

            // Similar to WinRTFileSystem.TryGetStorageItemAsync but we only 
            // want to capture exceptions that are not related to file not 
            // found.  This matches the behavior of the Win32 implementation.
            private async Task RefreshAsync()
            {
                string directoryPath, itemName;
                _item = null;
                _initializationException = null;

                try
                {
                    PathHelpers.SplitDirectoryFile(_fullPath, out directoryPath, out itemName);

                    StorageFolder parent = null;

                    try
                    {
                        parent = await StorageFolder.GetFolderFromPathAsync(directoryPath).TranslateWinRTTask(directoryPath, isDirectory: true);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Ignore DirectoryNotFound, in this case we just return null;
                    }

                    if (String.IsNullOrEmpty(itemName) || null == parent)
                        _item = parent;
                    else
                        _item = await parent.TryGetItemAsync(itemName).TranslateWinRTTask(_fullPath);
                }
                catch (Exception e)
                {
                    _initializationException = ExceptionDispatchInfo.Capture(e);
                }
            }
        }
    }
}
