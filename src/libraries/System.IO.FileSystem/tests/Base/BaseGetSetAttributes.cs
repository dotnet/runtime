// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.IO.Tests
{
    public abstract class BaseGetSetAttributes : FileSystemTest
    {
        protected abstract bool CanBeReadOnly { get; }
        protected abstract FileAttributes GetAttributes(string path);
        protected abstract void SetAttributes(string path, FileAttributes attributes);

        /// <summary>
        /// Create an appropriate filesystem object at the given path.
        /// </summary>
        protected virtual string CreateItem(string path = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            path =  path ?? GetTestFilePath(null, memberName, lineNumber);
            File.Create(path).Dispose();
            return path;
        }

        protected virtual void DeleteItem(string path) => File.Delete(path);

        protected virtual bool IsDirectory => false;
    }
}
