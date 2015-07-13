// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class File_Copy_str_str_b : File_Copy_str_str
    {
        #region Utilities

        public override void Copy(string source, string dest)
        {
            File.Copy(source, dest, false);
        }

        public virtual void Copy(string source, string dest, bool overwrite)
        {
            File.Copy(source, dest, overwrite);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void OverwriteTrue()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = GetTestFilePath();
            char[] sourceData = { 'a', 'A', 'b' };
            char[] destData = { 'x', 'X', 'y' };

            // Write and copy file
            using (StreamWriter sourceStream = new StreamWriter(File.Create(testFileSource)))
            using (StreamWriter destStream = new StreamWriter(File.Create(testFileDest)))
            {
                sourceStream.Write(sourceData, 0, sourceData.Length);
                sourceStream.Flush();
                destStream.Write(destData, 0, destData.Length);
                destStream.Flush();
            }
            Copy(testFileSource, testFileDest, true);

            // Ensure copy transferred written data
            using (StreamReader stream = new StreamReader(File.OpenRead(testFileDest)))
            {
                char[] readData = new char[sourceData.Length];
                stream.Read(readData, 0, sourceData.Length);
                Assert.Equal(sourceData, readData);
            }
        }

        [Fact]
        public void OverwriteFalse()
        {
            string testFileSource = GetTestFilePath();
            string testFileDest = GetTestFilePath();
            char[] sourceData = { 'a', 'A', 'b' };
            char[] destData = { 'x', 'X', 'y' };

            // Write and copy file
            using (StreamWriter sourceStream = new StreamWriter(File.Create(testFileSource)))
            using (StreamWriter destStream = new StreamWriter(File.Create(testFileDest)))
            {
                sourceStream.Write(sourceData, 0, sourceData.Length);
                sourceStream.Flush();
                destStream.Write(destData, 0, destData.Length);
                destStream.Flush();
            }
            Assert.Throws<IOException>(() => Copy(testFileSource, testFileDest, false));

            // Ensure copy didn't overwrite existing data
            using (StreamReader stream = new StreamReader(File.OpenRead(testFileDest)))
            {
                char[] readData = new char[sourceData.Length];
                stream.Read(readData, 0, sourceData.Length);
                Assert.Equal(destData, readData);
            }
        }

        #endregion
    }
}
