// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public partial class StreamReaderTests : FileCleanupTestBase
    {
        [Fact]
        public void ObjectClosedReadLine()
        {
            var baseInfo = GetCharArrayStream();
            StreamReader sr = baseInfo.Item2;

            sr.Close();
            Assert.Throws<ObjectDisposedException>(() => sr.ReadLine());
        }

        [Fact]
        public void ObjectClosedReadLineBaseStream()
        {
            Stream ms = GetLargeStream();
            StreamReader sr = new StreamReader(ms);

            ms.Close();
            Assert.Throws<ObjectDisposedException>(() => sr.ReadLine());
        }

        [Fact]
        public void Synchronized_NewObject()
        {
            using (Stream str = GetLargeStream())
            {
                StreamReader reader = new StreamReader(str);
                TextReader synced = TextReader.Synchronized(reader);
                Assert.NotEqual(reader, synced);
                int res1 = reader.Read();
                int res2 = synced.Read();
                Assert.NotEqual(res1, res2);
            }
        }

        public static IEnumerable<object[]> DetectEncoding_EncodingRoundtrips_MemberData()
        {
            yield return new object[] { new UTF8Encoding(encoderShouldEmitUTF8Identifier:true) };
            yield return new object[] { new UTF32Encoding(bigEndian:false, byteOrderMark:true) };
            yield return new object[] { new UTF32Encoding(bigEndian:true, byteOrderMark:true) };
            yield return new object[] { new UnicodeEncoding(bigEndian:false, byteOrderMark:true) };
            yield return new object[] { new UnicodeEncoding(bigEndian:true, byteOrderMark:true) };
        }

        [Theory]
        [MemberData(nameof(DetectEncoding_EncodingRoundtrips_MemberData))]
        public void DetectEncoding_EncodingRoundtrips(Encoding encoding)
        {
            const string Text = "This is some text for testing.";
            string path = GetTestFilePath();

            using (var stream = File.OpenWrite(path))
            using (var writer = new StreamWriter(stream, encoding))
            {
                writer.Write(Text);
            }

            using (var stream = File.OpenRead(path))
            using (var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks:true))
            {
                Assert.Equal(Text, reader.ReadToEnd());
                Assert.Equal(encoding.EncodingName, reader.CurrentEncoding.EncodingName);
            }
        }
    }
}
