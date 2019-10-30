// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class File_AppendText : File_ReadWriteAllText
    {
        protected override void Write(string path, string content)
        {
            var writer = File.AppendText(path);
            writer.Write(content);
            writer.Dispose();
        }

        [Fact]
        public override void Overwrite()
        {
            string path = GetTestFilePath();
            string lines = new string('c', 200);
            string appendLines = new string('b', 100);
            Write(path, lines);
            Write(path, appendLines);
            Assert.Equal(lines + appendLines, Read(path));
        }
    }

    public class File_AppendAllText : File_ReadWriteAllText
    {
        protected override void Write(string path, string content)
        {
            File.AppendAllText(path, content);
        }

        [Fact]
        public override void Overwrite()
        {
            string path = GetTestFilePath();
            string lines = new string('c', 200);
            string appendLines = new string('b', 100);
            Write(path, lines);
            Write(path, appendLines);
            Assert.Equal(lines + appendLines, Read(path));
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
        protected override void Write(string path, string[] content)
        {
            File.AppendAllLines(path, content);
        }

        [Fact]
        public override void Overwrite()
        {
            string path = GetTestFilePath();
            string[] lines = new string[] { new string('c', 200) };
            string[] appendLines = new string[] { new string('b', 100) };
            Write(path, lines);
            Write(path, appendLines);
            Assert.Equal(new string[] { lines[0], appendLines[0] }, Read(path));
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
