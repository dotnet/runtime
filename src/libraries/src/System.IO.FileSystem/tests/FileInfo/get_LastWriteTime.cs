// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_get_LastWriteTime : FileSystemTest
    {
        [Fact]
        public void WriteTimeRefreshes()
        {
            string fileName = GetTestFilePath();
            File.Create(fileName).Dispose();
            FileInfo fileInfo = new FileInfo(fileName);
            DateTime beforeWrite = DateTime.Now.AddSeconds(-1);
            using (Stream stream = new FileInfo(fileName).OpenWrite())
            {
                stream.Write(new Byte[] { 10 }, 0, 1);
            }
            DateTime afterWrite = DateTime.Now;
            fileInfo.Refresh();
            DateTime lastWriteTime = fileInfo.LastWriteTime;
            Assert.InRange<long>(fileInfo.LastWriteTime.Ticks, beforeWrite.Ticks, afterWrite.Ticks);

            //Read from the File and test the writeTime to ensure it is unchanged
            using (Stream stream = new FileInfo(fileName).OpenRead())
            {
                stream.Read(new Byte[1], 0, 1);
            }
            Assert.Equal(fileInfo.LastWriteTime, lastWriteTime);

            //Write to the file again to test lastWriteTime can be updated again
            beforeWrite = DateTime.Now.AddSeconds(-1);
            using (Stream stream = fileInfo.Open(FileMode.Open))
            {
                stream.Write(new Byte[] { 10 }, 0, 1);
            }
            afterWrite = DateTime.Now;
            Assert.Equal(fileInfo.LastWriteTime, lastWriteTime); //needs to be refreshed first
            fileInfo.Refresh();
            Assert.InRange<long>(fileInfo.LastWriteTime.Ticks, beforeWrite.Ticks, afterWrite.Ticks);
        }
    }
}
