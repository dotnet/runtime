using System.Diagnostics;
using System.Runtime.InteropServices;
using NiceIO;

namespace BuildDriver;

public class SevenZip
{
    public static NPath Create7z(NPath directory, NPath outputFile, string additional7zArguments = "")
        => Create7z(Get7zPath(), directory, outputFile, additional7zArguments);

    static NPath Create7z(NPath sevenZipPath, NPath directory, string outputFile, string additional7zArguments)
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

        BuildDriver.RunProcess(psi);

        return new NPath(outputFile);
    }

    public static NPath Get7zPath()
    {
        var artifacts = Paths.Artifacts;
        NPath GetArtifactsPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return artifacts.Combine("7za-win-x64/7za.exe");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
                    return artifacts.Combine("7za-mac-arm64/7za");
                return artifacts.Combine("7za-mac-x64/7za");
            }

            return artifacts.Combine("7za-linux-x64/7za");
        }

        var path = GetArtifactsPath();
        if (path.FileExists())
            return path;

        return new NPath("7z");
    }
}
