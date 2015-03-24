// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using IOPath = System.IO.Path;

internal abstract class TemporaryFileSystemItem<T> : IDisposable
    where T : FileSystemInfo
{
    private readonly T _info;

    protected TemporaryFileSystemItem(T info)
    {
#if !TEST_WINRT  // TODO: reenable once DirectoryInfo adapter is in place
        Debug.Assert(info.Exists);
#endif

        _info = info;
    }

    public string Path
    {
        get { return _info.FullName; }
    }

    public string Drive
    {
        get { return IOServices.RemoveTrailingSlash(IOPath.GetPathRoot(_info.FullName)); }
    }

    public T Info
    {
        get { return _info; }
    }

    public bool IsReadOnly
    {
        get { return (_info.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly; }
        set
        {
            if (value)
            {
                _info.Attributes |= FileAttributes.ReadOnly;
            }
            else
            {
                _info.Attributes &= ~FileAttributes.ReadOnly;
            }
        }
    }

    public bool IsHidden
    {
        get { return (_info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden; }
        set
        {
            if (value)
            {
                _info.Attributes |= FileAttributes.Hidden;
            }
            else
            {
                _info.Attributes &= ~FileAttributes.Hidden;
            }
        }
    }

    public void Dispose()
    {
        if (Info.Exists && IsReadOnly)
        {
            IsReadOnly = false;
        }

        Delete();
    }

    protected abstract void Delete();
}
