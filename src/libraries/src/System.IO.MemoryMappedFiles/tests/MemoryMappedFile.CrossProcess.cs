// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.MemoryMappedFiles.Tests
{
    public class CrossProcessTests : FileCleanupTestBase
    {
        [Fact]
        public void DataShared()
        {
            // Create a new file and load it into an MMF
            using (TempFile file = new TempFile(GetTestFilePath(), 4096))
            using (FileStream fs = new FileStream(file.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, null, fs.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
            using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor())
            {
                // Write some known data to the map
                long capacity = acc.Capacity;
                for (int i = 0; i < capacity; i++)
                {
                    acc.Write(i, unchecked((byte)i));
                }
                acc.Flush();

                // Spawn and then wait for the other process, which will verify the data and write its own known pattern
                RemoteExecutor.Invoke(new Func<string, int>(DataShared_OtherProcess), file.Path).Dispose();

                // Now verify we're seeing the data from the other process
                for (int i = 0; i < capacity; i++)
                {
                    Assert.Equal(unchecked((byte)(capacity - i - 1)), acc.ReadByte(i));
                }
            }
        }

        private static int DataShared_OtherProcess(string path)
        {
            // Open the specified file and load it into a map
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs, null, fs.Length, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true))
            using (MemoryMappedViewAccessor acc = mmf.CreateViewAccessor())
            {
                // Verify the known pattern written by the other process, then write one of our own
                long capacity = acc.Capacity;
                for (int i = 0; i < capacity; i++)
                {
                    Assert.Equal(unchecked((byte)i), acc.ReadByte(i));
                }
                for (int i = 0; i < capacity; i++)
                {
                    acc.Write(i, unchecked((byte)(capacity - i - 1)));
                }
                acc.Flush();
                return RemoteExecutor.SuccessExitCode;
            }
        }

    }
}
