// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    public sealed partial class FileInfo : FileSystemInfo
    {
        public StreamReader OpenText(FileStreamOptions options)
            => File.OpenText(NormalizedPath, options);

        public StreamWriter CreateText(FileStreamOptions options)
            => File.CreateText(NormalizedPath, options);

        public StreamWriter AppendText(FileStreamOptions options)
            => File.AppendText(NormalizedPath, options);

        public FileStream Create(FileStreamOptions options)
        {
            FileStream fileStream = File.Create(NormalizedPath, options);
            Invalidate();
            return fileStream;
        }

        public FileStream Open(FileStreamOptions options)
            => File.Open(NormalizedPath, options);

        public FileStream OpenRead(FileStreamOptions options)
            => File.OpenRead(NormalizedPath, options);

        public FileStream OpenWrite(FileStreamOptions options)
            => File.OpenRead(NormalizedPath, options);
    }
}
