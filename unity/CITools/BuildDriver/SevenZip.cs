using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using NiceIO;

namespace BuildDriver;

public class SevenZip
{
    public static NPath Create7z(NPath sevenZipPath, NPath directory, string outputFile, string additional7zArguments = "")
    {
        Console.WriteLine($"Creating .7z {outputFile}");
        var args = $"a {outputFile} *";
        if (!string.IsNullOrWhiteSpace(additional7zArguments))
            args = $"{additional7zArguments} {args}";

        ProcessStartInfo psi = new()
        {
          FileName = sevenZipPath,
          Arguments = args,
          WorkingDirectory = directory
        };

        Utils.RunProcess(psi);

        return new NPath(outputFile);
    }

    public static void Zip(NPath zipExe, NPath artifacts, GlobalConfig gConfig)
    {
        NPath zipArtifact = new (Environment.GetEnvironmentVariable("ARTIFACT_FILENAME") ??
                                 $"dotnet-unity-{gConfig.Architecture}.7z");
        Create7z(zipExe, artifacts, Paths.Artifacts.Combine("unity", zipArtifact));
    }

    public static void Get7ZipUrl(out string url, out string filename)
    {
        string baseUrl = "https://public-stevedore.unity3d.com/r/public";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            url = $"{baseUrl}/7za-linux-x64/e6c75fb7ffda_e6a295cdcae3f74d315361883cf53f75141be2e739c020035f414a449d4876af.zip";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            url = RuntimeInformation.OSArchitecture == Architecture.Arm64
                ? $"{baseUrl}/7za-mac-arm64/e6c75fb7ffda_891473e6242d16ca181cee7c728b73f80c931f58de45ab79f376acd63d524151.zip"
                : $"{baseUrl}/7za-mac-x64/e6c75fb7ffda_5bd76652986a0e3756d1cfd7e84ce056a9e1dbfc5f70f0514a001f724c0fbad2.zip";
        }
        else
            url = $"{baseUrl}/7za-win-x64/38c5b39be2e8_a333cfccb708c88459b3812eb2597ca486ec9b416172543ca3ef8e5cd5f80984.zip";

        filename = $"{url.Split('/').Single(s => s.StartsWith("7za"))}.zip";
    }

    public static Task<NPath> DownloadAndUnzip7Zip()
    {
        return Task.Run(() =>
        {
            Get7ZipUrl(out string url, out string filename);
            NPath zipDest = Paths.Artifacts.Combine(filename);
            zipDest.Parent.EnsureDirectoryExists();
            if (!zipDest.FileExists())
            {
                Console.WriteLine($"Starting download of 7zip: {filename}");
                using (HttpClient client = new())
                    using (Task<Stream> s = client.GetStreamAsync(url))
                        using (FileStream fs = new(zipDest, FileMode.OpenOrCreate))
                            s.Result.CopyTo(fs);
            }

            NPath destDir = zipDest.Parent.Combine(zipDest.FileNameWithoutExtension);
            if (!destDir.DirectoryExists())
            {
                Console.WriteLine($"Extracting 7zip to: {destDir}");
                ZipFile.ExtractToDirectory(zipDest, destDir);
            }

            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? destDir.Combine("7za.exe") : destDir.Combine("7za");
        });
    }
}
