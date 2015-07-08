using System;
using System.IO;
using Xunit;

namespace StreamWriterTests
{
    public class BaseStream
    {
        [Fact]
        public static void GetBaseStream()
        {
            // [] Get an underlying memorystream
            MemoryStream memstr2 = new MemoryStream();
            StreamWriter sw = new StreamWriter(memstr2);
            Assert.Equal(sw.BaseStream, memstr2);
        }
    }
}
