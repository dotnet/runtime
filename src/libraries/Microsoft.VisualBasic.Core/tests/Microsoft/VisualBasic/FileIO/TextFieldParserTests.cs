// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.VisualBasic.FileIO.Tests
{
    public class TextFieldParserTests : FileCleanupTestBase
    {
        [Fact]
        public void Constructors()
        {
            var path = GetTestFilePath();
            File.WriteAllText(path, "abc123");

            // public TextFieldParser(System.IO.Stream stream)
            using (var stream = new FileStream(path, FileMode.Open))
            {
                using (var parser = new TextFieldParser(stream))
                {
                }
                Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
            }

            // public TextFieldParser(System.IO.Stream stream, System.Text.Encoding defaultEncoding, bool detectEncoding, bool leaveOpen);
            using (var stream = new FileStream(path, FileMode.Open))
            {
                using (var parser = new TextFieldParser(stream, defaultEncoding: System.Text.Encoding.Unicode, detectEncoding: true, leaveOpen: true))
                {
                }
                _ = stream.ReadByte();
            }

            // public TextFieldParser(System.IO.TextReader reader)
            using (var reader = new StreamReader(path))
            {
                using (var parser = new TextFieldParser(reader))
                {
                }
                Assert.Throws<ObjectDisposedException>(() => reader.ReadToEnd());
            }

            // public TextFieldParser(string path)
            using (var parser = new TextFieldParser(path))
            {
            }

            // public TextFieldParser(string path)
            Assert.Throws<FileNotFoundException>(() => new TextFieldParser(GetTestFilePath()));
        }

        [Fact]
        public void Close()
        {
            var path = GetTestFilePath();
            File.WriteAllText(path, "abc123");

            using (var stream = new FileStream(path, FileMode.Open))
            {
                using (var parser = new TextFieldParser(stream))
                {
                    parser.Close();
                }
            }

            using (var parser = new TextFieldParser(path))
            {
                parser.Close();
            }

            {
                var parser = new TextFieldParser(path);
                parser.Close();
                parser.Close();
            }

            {
                TextFieldParser parser;
                using (parser = new TextFieldParser(path))
                {
                }
                parser.Close();
            }
        }

        [Fact]
        public void Properties()
        {
            var path = GetTestFilePath();
            File.WriteAllText(path, "abc123");

            using (var parser = new TextFieldParser(path))
            {
                Assert.Equal(new string[0], parser.CommentTokens);
                parser.CommentTokens = new[] { "[", "]" };
                Assert.Equal(new[] { "[", "]" }, parser.CommentTokens);

                Assert.Null(parser.Delimiters);
                parser.Delimiters = new[] { "A", "123" };
                Assert.Equal(new[] { "A", "123" }, parser.Delimiters);
                parser.SetDelimiters(new[] { "123", "B" });
                Assert.Equal(new[] { "123", "B" }, parser.Delimiters);

                Assert.Null(parser.FieldWidths);
                parser.FieldWidths = new[] { 1, 2, int.MaxValue };
                Assert.Equal(new[] { 1, 2, int.MaxValue }, parser.FieldWidths);
                parser.SetFieldWidths(new[] { int.MaxValue, 3 });
                Assert.Equal(new[] { int.MaxValue, 3 }, parser.FieldWidths);
                Assert.Throws<ArgumentException>(() => parser.SetFieldWidths(new[] { -1, -1 }));

                Assert.True(parser.HasFieldsEnclosedInQuotes);
                parser.HasFieldsEnclosedInQuotes = false;
                Assert.False(parser.HasFieldsEnclosedInQuotes);

                Assert.Equal(FieldType.Delimited, parser.TextFieldType);
                parser.TextFieldType = FieldType.FixedWidth;
                Assert.Equal(FieldType.FixedWidth, parser.TextFieldType);

                Assert.True(parser.TrimWhiteSpace);
                parser.TrimWhiteSpace = false;
                Assert.False(parser.TrimWhiteSpace);
            }
        }

        // Not tested:
        //   public string[] CommentTokens { get { throw null; } set { } }

        [Fact]
        public void ErrorLine()
        {
            var path = GetTestFilePath();
            File.WriteAllText(path,
@"abc 123
def 45
ghi 789");

            using (var parser = new TextFieldParser(path))
            {
                parser.TextFieldType = FieldType.FixedWidth;
                parser.SetFieldWidths(new[] { 3, 4 });

                Assert.Equal(-1, parser.ErrorLineNumber);
                Assert.Equal("", parser.ErrorLine);

                Assert.Equal(new[] { "abc", "123" }, parser.ReadFields());
                Assert.Equal(-1, parser.ErrorLineNumber);
                Assert.Equal("", parser.ErrorLine);

                Assert.Throws<MalformedLineException>(() => parser.ReadFields());
                Assert.Equal(2, parser.ErrorLineNumber);
                Assert.Equal("def 45", parser.ErrorLine);

                Assert.Equal(new[] { "ghi", "789" }, parser.ReadFields());
                Assert.Equal(2, parser.ErrorLineNumber);
                Assert.Equal("def 45", parser.ErrorLine);
            }
        }

        [Fact]
        public void HasFieldsEnclosedInQuotes_TrimWhiteSpace()
        {
            var path = GetTestFilePath();
            File.WriteAllText(path, @""""", "" "" ,""abc"", "" 123 "" ,");

            using (var parser = new TextFieldParser(path))
            {
                parser.Delimiters = new[] { "," };
                Assert.Equal(new[] { "", "", "abc", "123", "" }, parser.ReadFields());
            }

            using (var parser = new TextFieldParser(path))
            {
                parser.TrimWhiteSpace = false;
                parser.Delimiters = new[] { "," };
                Assert.Equal(new[] { "", " ", "abc", " 123 ", "" }, parser.ReadFields());
            }

            using (var parser = new TextFieldParser(path))
            {
                parser.HasFieldsEnclosedInQuotes = false;
                parser.Delimiters = new[] { "," };
                Assert.Equal(new[] { @"""""", @""" """, @"""abc""", @""" 123 """, "" }, parser.ReadFields());
            }

            using (var parser = new TextFieldParser(path))
            {
                parser.TrimWhiteSpace = false;
                parser.HasFieldsEnclosedInQuotes = false;
                parser.Delimiters = new[] { "," };
                Assert.Equal(new[] { @"""""", @" "" "" ", @"""abc""", @" "" 123 "" ", "" }, parser.ReadFields());
            }
        }

        [Fact]
        public void PeekChars()
        {
            var path = GetTestFilePath();
            File.WriteAllText(path,
@"abc,123
def,456
ghi,789");

            using (var parser = new TextFieldParser(path))
            {
                Assert.Throws<ArgumentException>(() => parser.PeekChars(0));

                Assert.Equal("a", parser.PeekChars(1));
                Assert.Equal("abc,123", parser.PeekChars(10));

                Assert.Equal("abc,123", parser.ReadLine());

                parser.TextFieldType = FieldType.FixedWidth;
                parser.SetFieldWidths(new[] { 3, -1 });

                Assert.Equal("d", parser.PeekChars(1));
                Assert.Equal("def,456", parser.PeekChars(10));
                Assert.Equal(new[] { "def", ",456" }, parser.ReadFields());

                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(new[] { "," });

                Assert.Equal("g", parser.PeekChars(1));
                Assert.Equal("ghi,789", parser.PeekChars(10));
                Assert.Equal(new[] { "ghi", "789" }, parser.ReadFields());

                Assert.Null(parser.PeekChars(1));
                Assert.Null(parser.PeekChars(10));
            }
        }

        [Fact]
        public void ReadFields_FieldWidths()
        {
            var path = GetTestFilePath();
            File.WriteAllText(path,
@"abc,123
def,456
ghi,789");

            using (var parser = new TextFieldParser(path))
            {
                parser.TextFieldType = FieldType.FixedWidth;

                Assert.Throws<InvalidOperationException>(() => parser.ReadFields());

                parser.SetFieldWidths(new[] { -1 });
                Assert.Equal(new[] { "abc,123" }, parser.ReadFields());

                parser.SetFieldWidths(new[] { 3, -1 });
                Assert.Equal(new[] { "def", ",456" }, parser.ReadFields());

                parser.SetFieldWidths(new[] { 3, 2 });
                Assert.Equal(new[] { "ghi", ",7" }, parser.ReadFields());

                parser.SetFieldWidths(new[] { 3, 2 });
                Assert.Null(parser.ReadFields());
            }
        }

        [Fact]
        public void ReadFields_Delimiters_LineNumber()
        {
            var path = GetTestFilePath();
            File.WriteAllText(path,
@"abc,123
def,456
ghi,789");

            using (var parser = new TextFieldParser(path))
            {
                Assert.Equal(1, parser.LineNumber);

                Assert.Throws<ArgumentException>(() => parser.ReadFields());
                Assert.Equal(1, parser.LineNumber);

                parser.SetDelimiters(new[] { ", " });
                Assert.Equal(new[] { "abc,123" }, parser.ReadFields());
                Assert.Equal(2, parser.LineNumber);

                parser.SetDelimiters(new[] { ";", "," });
                Assert.Equal(new[] { "def", "456" }, parser.ReadFields());
                Assert.Equal(3, parser.LineNumber);

                parser.SetDelimiters(new[] { "g", "9" });
                Assert.Equal(new[] { "", "hi,78", "" }, parser.ReadFields());
                Assert.Equal(-1, parser.LineNumber);
            }

            File.WriteAllText(path,
@",,

,
");

            using (var parser = new TextFieldParser(path))
            {
                Assert.Equal(1, parser.LineNumber);

                parser.SetDelimiters(new[] { "," });
                Assert.Equal(new[] { "", "", "" }, parser.ReadFields());
                Assert.Equal(2, parser.LineNumber);

                Assert.Equal(new[] { "", "" }, parser.ReadFields());
                Assert.Equal(-1, parser.LineNumber);

                Assert.Null(parser.ReadFields());
                Assert.Equal(-1, parser.LineNumber);

                Assert.Null(parser.ReadFields());
                Assert.Equal(-1, parser.LineNumber);
            }
        }

        [Fact]
        public void ReadLine_ReadToEnd()
        {
            var path = GetTestFilePath();
            File.WriteAllText(path,
@"abc
123");

            using (var parser = new TextFieldParser(path))
            {
                Assert.False(parser.EndOfData);

                Assert.Equal(
@"abc
123",
                    parser.ReadToEnd());
                Assert.Equal(-1, parser.LineNumber);
                Assert.True(parser.EndOfData);
            }

            using (var parser = new TextFieldParser(path))
            {
                Assert.Equal("abc", parser.ReadLine());
                Assert.Equal(2, parser.LineNumber);
                Assert.False(parser.EndOfData);

                Assert.Equal("123", parser.ReadToEnd());
                Assert.Equal(-1, parser.LineNumber);
                Assert.True(parser.EndOfData);
            }

            using (var parser = new TextFieldParser(path))
            {
                Assert.Equal("abc", parser.ReadLine());
                Assert.Equal(2, parser.LineNumber);
                Assert.False(parser.EndOfData);

                Assert.Equal("123", parser.ReadLine());
                Assert.Equal(-1, parser.LineNumber);
                Assert.True(parser.EndOfData);

                Assert.Null(parser.ReadToEnd());
                Assert.Equal(-1, parser.LineNumber);
                Assert.True(parser.EndOfData);
            }
        }

        [Fact]
        public void UnmatchedQuote_MalformedLineException()
        {
            var path = GetTestFilePath();
            File.WriteAllText(path, @""""", """);

            using (var parser = new TextFieldParser(path))
            {
                parser.Delimiters = new[] { "," };
                Assert.Throws<MalformedLineException>(() => parser.ReadFields());
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReadFields_PartialReadsFromStream_Large(bool fieldsInQuotes) =>
            ReadFields_PartialReadsFromStream(fieldsInQuotes, 1023);

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework doesn't properly handle streams frequently returning much less than requested")]
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReadFields_PartialReadsFromStream_Small(bool fieldsInQuotes) =>
            ReadFields_PartialReadsFromStream(fieldsInQuotes, 10);

        private void ReadFields_PartialReadsFromStream(bool fieldsInQuotes, int maxCount)
        {
            // Generate some data
            var rand = new Random(42);
            string[][] expected = Enumerable
                .Range(0, 10_000)
                .Select(_ => Enumerable.Range(0, 4).Select(_ => new string('s', rand.Next(0, 10))).ToArray())
                .ToArray();

            // Write it out
            Stream s = new DribbleStream() { MaxCount = maxCount };
            using (var writer = new StreamWriter(s, Encoding.UTF8, 1024, leaveOpen: true))
            {
                foreach (string[] line in expected)
                {
                    string separator = "";
                    foreach (string part in line)
                    {
                        writer.Write(separator);
                        separator = ",";
                        if (fieldsInQuotes) writer.Write('"');
                        writer.Write(part);
                        if (fieldsInQuotes) writer.Write('"');
                    }
                    writer.WriteLine();
                }
            }

            // Read/parse it back in
            s.Position = 0;
            using (var parser = new TextFieldParser(s))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(new[] { "," });
                parser.HasFieldsEnclosedInQuotes = fieldsInQuotes;

                int i = 0;
                while (!parser.EndOfData)
                {
                    string[]? actual = parser.ReadFields();
                    Assert.Equal(expected[i], actual);
                    i++;
                }
                Assert.Equal(expected.Length, i);
            }
        }

        private sealed class DribbleStream : MemoryStream
        {
            public int MaxCount { get; set; } = 1;

            public override int Read(byte[] buffer, int offset, int count) =>
                base.Read(buffer, offset, Math.Min(count, MaxCount));
        }
    }
}
