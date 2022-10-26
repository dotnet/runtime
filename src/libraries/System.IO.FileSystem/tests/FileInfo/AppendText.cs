// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_AppendText : File_ReadWriteAllText
    {
        protected override bool IsAppend => true;

        protected override void Write(string path, string content)
        {
            var writer = new FileInfo(path).AppendText();
            writer.Write(content);
            writer.Dispose();
        }

        protected override void Write(string path, string content, Encoding encoding)
        {
            var writer = new StreamWriter(path, IsAppend, encoding);
            writer.Write(content);
            writer.Dispose();
        }

        [Fact]
        public void FileInfoInvalidAfterAppendText()
        {
            FileInfo info = new FileInfo(GetTestFilePath());
            using (StreamWriter streamWriter = info.AppendText())
            {
                Assert.True(info.Exists);
            }
        }
    }
}
