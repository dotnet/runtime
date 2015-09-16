// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using Xunit;

namespace InterProcessCommunication.Tests
{
    public class MemoryMappedFilesTests : RemoteExecutorTestBase
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
                    acc.Write(i, (byte)i);
                }
                acc.Flush();

                // Spawn and then wait for the other process, which will verify the data and write its own known pattern
                RemoteInvoke(DataShared_OtherProcess, file.Path).Dispose();

                // Now verify we're seeing the data from the other process
                for (int i = 0; i < capacity; i++)
                {
                    Assert.Equal((byte)(capacity - i - 1), acc.ReadByte(i));
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
                    Assert.Equal((byte)i, acc.ReadByte(i));
                }
                for (int i = 0; i < capacity; i++)
                {
                    acc.Write(i, (byte)(capacity - i - 1));
                }
                acc.Flush();
                return SuccessExitCode;
            }
        }

    }
}
