// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    /// <devdoc>
    /// Represents the method that will handle the <see cref='System.IO.FileSystemWatcher.Changed'/>, 
    /// <see cref='System.IO.FileSystemWatcher.Created'/>, or 
    /// <see cref='System.IO.FileSystemWatcher.Deleted'/> event of 
    /// a <see cref='System.IO.FileSystemWatcher'/> class.
    /// </devdoc>
    public delegate void FileSystemEventHandler(object sender, FileSystemEventArgs e);
}
