using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Melanzana.CodeSign.Tests
{
    public class SignerTests
    {
        private const string WebDriverAgentFileName = "WebDriverAgentRunner-Runner.zip";

        [Theory]
        [InlineData("Frameworks/XCTAutomationSupport.framework")]
        [InlineData("Frameworks/XCTestCore.framework")]
        [InlineData("Frameworks/XCUIAutomation.framework")]
        [InlineData("Frameworks/XCUnit.framework")]
        public async Task CodeResources_Roundtrip(string nestedBundlePath)
        {
            // Extract a fresh copy of the WebDriverAgent into a directory dedicated to this test.
            var testDirectory = Path.GetFullPath(nameof(CodeResources_Roundtrip));
            await ExtractWebDriverAgent(testDirectory);

            // Build the resource seal and convert to a XML property list
            var bundleDirectory = Path.Combine(testDirectory, "WebDriverAgentRunner-Runner.app", nestedBundlePath);
            var bundle = new Bundle(bundleDirectory);
            var signer = new Signer(new CodeSignOptions());
            var actual = signer.BuildResourceSeal(bundle).ToXmlPropertyList();

            // The newly created resource seal should match the original resource seal
            var expected = File.ReadAllText(Path.Combine(bundleDirectory, "_CodeSignature/CodeResources"));
            Assert.Equal(expected, actual);
        }

        private async Task ExtractWebDriverAgent(string path)
        {
            await DownloadWebDriverAgent(WebDriverAgentFileName);

            if (!Directory.Exists(path))
            {
                using (Stream stream = File.OpenRead(WebDriverAgentFileName))
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    archive.ExtractToDirectory(path);
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

        private static void DeleteResources(string path)
        {
            foreach (var dir in Directory.GetDirectories(path))
            {
                if (Path.GetFileName(dir) == "_CodeSignature")
                {
                    Directory.Delete(dir, recursive: true);
                }
                else
                {
                    DeleteResources(dir);
                }
            }
        }
    }
}
