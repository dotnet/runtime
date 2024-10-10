// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.NET.HostModel.MachO.Tests
{
    public class RoundtripTests
    {
        private const string WebDriverAgentFileName = "WebDriverAgentRunner-Runner.zip";

        private static void TestRoundtrip(Stream aOutStream)
        {
            var objectFile = MachReader.Read(aOutStream).Single();

            using (MemoryStream cloneStream = new MemoryStream((int)aOutStream.Length))
            using (var outputStream = new ValidatingStream(cloneStream))
            {
                aOutStream.Seek(0, SeekOrigin.Begin);
                aOutStream.CopyTo(cloneStream);
                outputStream.Seek(0, SeekOrigin.Begin);

                MachWriter.Write(outputStream, objectFile);
            }
        }

        private static void TestFatRoundtrip(Stream aOutStream)
        {
            var objectFiles = MachReader.Read(aOutStream).ToList();

            using (MemoryStream cloneStream = new MemoryStream((int)aOutStream.Length))
            using (var outputStream = new ValidatingStream(cloneStream))
            {
                aOutStream.Seek(0, SeekOrigin.Begin);
                aOutStream.CopyTo(cloneStream);
                outputStream.Seek(0, SeekOrigin.Begin);

                MachWriter.Write(outputStream, objectFiles);
            }
        }

        [Fact]
        public void BasicRoundtrip()
        {
            var aOutStream = typeof(RoundtripTests).Assembly.GetManifestResourceStream("Microsoft.NET.HostModel.MachO.Tests.Data.a.out")!;
            TestRoundtrip(aOutStream);
        }

        [Fact]
        public void ObjectFileRoundtrip()
        {
            var aOutStream = typeof(RoundtripTests).Assembly.GetManifestResourceStream("Microsoft.NET.HostModel.MachO.Tests.Data.a.o")!;
            TestRoundtrip(aOutStream);
        }

        [Fact]
        public void ExecutableRoundtrip()
        {
            var aOutStream = typeof(RoundtripTests).Assembly.GetManifestResourceStream("Microsoft.NET.HostModel.MachO.Tests.Data.rpath.out")!;
            TestRoundtrip(aOutStream);
        }
    }
}
