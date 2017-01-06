using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.Cli.Build.Framework;
using NugetProgram = NuGet.CommandLine.XPlat.Program;

namespace Microsoft.DotNet.Cli.Build
{
    public static class NuGetUtil
    {
        public static void PushPackages(
            string packageDirPath,
            string destinationUrl,
            string apiKey,
            bool includeSymbolPackages)
        {
            string[] paths;
            if (includeSymbolPackages)
            {
                paths = new[]
                {
                    Path.Combine(packageDirPath, "*.nupkg")
                };
            }
            else
            {
                paths = Directory.GetFiles(packageDirPath, "*.nupkg")
                    .Where(path => !path.EndsWith(".symbols.nupkg"))
                    .ToArray();
            }

            foreach (var path in paths)
            {
                int result = RunNuGetCommand(
                    "push",
                    "-s", destinationUrl,
                    "-k", apiKey,
                    "--timeout", "3600",
                    path);

                if (result != 0)
                {
                    throw new BuildFailureException($"NuGet Push failed with exit code '{result}'.");
                }
            }
        }

        private static int RunNuGetCommand(params string[] nugetArgs)
        {
            var nugetAssembly = typeof(NugetProgram).GetTypeInfo().Assembly;
            var mainMethod = nugetAssembly.EntryPoint;
            return (int)mainMethod.Invoke(null, new object[] { nugetArgs });
        }
    }
}

