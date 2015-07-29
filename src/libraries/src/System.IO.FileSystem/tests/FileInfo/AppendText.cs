// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileInfo_AppendText : File_ReadWriteAllText
    {
        protected override void Write(string path, string content)
        {
            var writer = new FileInfo(path).AppendText();
            writer.Write(content);
            writer.Dispose();
        }

        [Fact]
        public override void Overwrite()
        {
            string path = GetTestFilePath();
            string lines = new string('c', 200);
            string appendline = new string('b', 100);
            Write(path, lines);
            Write(path, appendline);
            Assert.Equal(lines + appendline, Read(path));
        }
    }
}
