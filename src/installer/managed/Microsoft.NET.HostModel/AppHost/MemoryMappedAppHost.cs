// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.NET.HostModel.MachO;

namespace Microsoft.NET.HostModel.AppHost
{
    internal class MemoryMappedAppHost : IDisposable
    {
        public FileStream FileStream { get; }
        public MemoryMappedFile MemoryMappedFile { get; }
        public MemoryMappedViewAccessor MemoryMappedViewAccessor { get; }
        public long Length { get; set; }

        private MemoryMappedAppHost(FileStream fileStream, long capacity = 0)
        {
            FileStream = fileStream;
            MemoryMappedFile = MemoryMappedFile.CreateFromFile(FileStream, null, capacity, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: true);
            MemoryMappedViewAccessor = MemoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.ReadWrite);
            Length = capacity == 0 ? FileStream.Length : capacity;
        }

        public static MemoryMappedAppHost Create(string appHostFilePath, bool macOsCodesign = false)
        {
            var fs = new FileStream(appHostFilePath, FileMode.Open, FileAccess.ReadWrite);
            return Create(fs, macOsCodesign, Path.GetFileName(appHostFilePath));
        }

        public static MemoryMappedAppHost Create(FileStream fs, bool macOsCodesign = false, string fileName = "")
        {
            long mappedFileLength = macOsCodesign ?
                MachObjectFile.GetSignatureSizeEstimate((uint)fs.Length, fileName)
                : fs.Length;
            return new MemoryMappedAppHost(fs, mappedFileLength);
        }

        public void Dispose()
        {
            MemoryMappedViewAccessor.Dispose();
            MemoryMappedFile.Dispose();
            FileStream.SetLength(Length);
            FileStream.Dispose();
        }
    }
}
