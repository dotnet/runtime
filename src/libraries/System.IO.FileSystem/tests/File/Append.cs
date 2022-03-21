// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class File_AppendText : File_ReadWriteAllText
    {
        protected override bool IsAppend => true;

        protected override void Write(string path, string content)
        {
            var writer = File.AppendText(path);
            writer.Write(content);
            writer.Dispose();
        }

        protected override void Write(string path, string content, Encoding encoding)
        {
            var writer = new StreamWriter(path, IsAppend, encoding);
            writer.Write(content);
            writer.Dispose();
        }
    }

    public class File_AppendAllText : File_ReadWriteAllText
    {
        protected override bool IsAppend => true;

        protected override void Write(string path, string content)
        {
            File.AppendAllText(path, content);
        }

        protected override void Write(string path, string content, Encoding encoding)
        {
            File.AppendAllText(path, content, encoding);
        }
    }

    public class File_AppendAllText_Encoded : File_AppendAllText
    {
        protected override void Write(string path, string content)
        {
            File.AppendAllText(path, content, new UTF8Encoding(false));
        }

        [Fact]
        public void NullEncoding()
        {
            Assert.Throws<ArgumentNullException>(() => File.AppendAllText(GetTestFilePath(), "Text", null));
        }
    }

    public class File_AppendAllLines : File_ReadWriteAllLines_Enumerable
    {
        protected override bool IsAppend => true;

        protected override void Write(string path, string[] content)
        {
            File.AppendAllLines(path, content);
        }
    }

    public class File_AppendAllLines_Encoded : File_AppendAllLines
    {
        protected override void Write(string path, string[] content)
        {
            File.AppendAllLines(path, content, new UTF8Encoding(false));
        }

        [Fact]
        public void NullEncoding()
        {
            Assert.Throws<ArgumentNullException>(() => File.AppendAllLines(GetTestFilePath(), new string[] { "Text" }, null));
        }
    }
}
