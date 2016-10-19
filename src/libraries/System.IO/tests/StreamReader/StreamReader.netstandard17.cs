// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Tests
{
    public partial class StreamReaderTests
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
    }
}
