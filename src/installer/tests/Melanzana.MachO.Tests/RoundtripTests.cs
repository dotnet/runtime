using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Melanzana.MachO.Tests
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
            var aOutStream = typeof(RoundtripTests).Assembly.GetManifestResourceStream("Melanzana.MachO.Tests.Data.a.out")!;
            TestRoundtrip(aOutStream);
        }

        [Fact]
        public void ObjectFileRoundtrip()
        {
            var aOutStream = typeof(RoundtripTests).Assembly.GetManifestResourceStream("Melanzana.MachO.Tests.Data.a.o")!;
            TestRoundtrip(aOutStream);
        }

        [Fact]
        public void ExecutableRoundtrip()
        {
            var aOutStream = typeof(RoundtripTests).Assembly.GetManifestResourceStream("Melanzana.MachO.Tests.Data.rpath.out")!;
            TestRoundtrip(aOutStream);
        }

        [Theory]
        [InlineData("WebDriverAgentRunner-Runner.app/WebDriverAgentRunner-Runner")]
        [InlineData("WebDriverAgentRunner-Runner.app/Frameworks/XCTAutomationSupport.framework/XCTAutomationSupport")]
        [InlineData("WebDriverAgentRunner-Runner.app/Frameworks/XCTest.framework/XCTest")]
        [InlineData("WebDriverAgentRunner-Runner.app/Frameworks/XCTestCore.framework/XCTestCore")]
        [InlineData("WebDriverAgentRunner-Runner.app/Frameworks/XCUIAutomation.framework/XCUIAutomation")]
        [InlineData("WebDriverAgentRunner-Runner.app/Frameworks/XCUnit.framework/XCUnit")]
        [InlineData("WebDriverAgentRunner-Runner.app/PlugIns/WebDriverAgentRunner.xctest/WebDriverAgentRunner")]
        [InlineData("WebDriverAgentRunner-Runner.app/PlugIns/WebDriverAgentRunner.xctest/Frameworks/WebDriverAgentLib.framework/WebDriverAgentLib")]
        public async Task WebDriverAgentRunnerRoundtrip(string entryName)
        {
            await DownloadWebDriverAgent(WebDriverAgentFileName);

            using (var bundleStream = File.OpenRead(WebDriverAgentFileName))
            using (var zipArchive = new ZipArchive(bundleStream, ZipArchiveMode.Read))
            using (var machObjectZipStream = zipArchive.GetEntry(entryName)!.Open())
            using (var machObjectStream = new MemoryStream())
            {
                machObjectZipStream.CopyTo(machObjectStream);

                if (MachReader.IsFatMach(machObjectStream))
                {
                    TestFatRoundtrip(machObjectStream);
                }
                else
                {
                    TestRoundtrip(machObjectStream);
                }
            }
        }

        private async Task DownloadWebDriverAgent(string path, string version = "v4.10.10")
        {
            if (!File.Exists(path))
            {
                using (var targetStream = File.Create(path))
                using (var client = new HttpClient())
                using (var sourceStream = await client.GetStreamAsync($"https://github.com/appium/WebDriverAgent/releases/download/{version}/WebDriverAgentRunner-Runner.zip"))
                {
                    await sourceStream.CopyToAsync(targetStream);
                }
            }
        }
    }
}
