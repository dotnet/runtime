using System.IO;
using System.Text;
using Microsoft.DotNet.Cli.Build;

namespace Microsoft.DotNet.Host.Build
{
    public class StubPackageBuilder
    {
        private DotNetCli _dotnet;
        private string _intermediateDirectory;
        private string _outputDirectory;

        private bool _dummyFileCreated;

        public StubPackageBuilder(DotNetCli dotnet, string intermediateDirectory, string outputDirectory)
        {
            _dotnet = dotnet;
            _intermediateDirectory = intermediateDirectory;
            _outputDirectory = outputDirectory;
        }

        public void GeneratePackage(string packageId, string version)
        {
            if (!_dummyFileCreated)
            {
                CreateDummyFile(_dotnet, _intermediateDirectory);
            }

            CreateStubPackage(_dotnet, packageId, version, _intermediateDirectory, _outputDirectory);
        }

        private void CreateDummyFile(DotNetCli dotnet, string intermediateDirectory)
        {
            var dummyTxt = "dummy text";

            var tempPjDirectory = Path.Combine(intermediateDirectory, "dummyNuGetPackageIntermediate");
            FS.Rmdir(tempPjDirectory);

            Directory.CreateDirectory(tempPjDirectory);

            var dummyTextFile = Path.Combine(tempPjDirectory, "dummy.txt");

            File.WriteAllText(dummyTextFile, dummyTxt);

            _dummyFileCreated = true;
        }

        private static void CreateStubPackage(DotNetCli dotnet, 
            string packageId, 
            string version,
            string intermediateDirectory, 
            string outputDirectory)
        {
            var projectJson = new StringBuilder();
            projectJson.Append("{");
            projectJson.Append($"  \"version\": \"{version}\",");
            projectJson.Append($"  \"name\": \"{packageId}\",");
            projectJson.Append("  \"packOptions\": { \"files\": { \"include\": \"dummy.txt\" } },");
            projectJson.Append("  \"frameworks\": { \"netcoreapp1.0\": { } },");
            projectJson.Append("}");

            var tempPjDirectory = Path.Combine(intermediateDirectory, "dummyNuGetPackageIntermediate");
            var tempPjFile = Path.Combine(tempPjDirectory, "project.json");

            File.WriteAllText(tempPjFile, projectJson.ToString());

            dotnet.Pack(
                tempPjFile, "--no-build",
                "--output", outputDirectory)
                .WorkingDirectory(tempPjDirectory)
                .Execute()
                .EnsureSuccessful();
        }
    }
}
