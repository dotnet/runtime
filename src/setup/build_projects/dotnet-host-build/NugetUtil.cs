using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.DotNet.Cli.Build.Framework;
using NugetProgram = NuGet.CommandLine.XPlat.Program;

namespace Microsoft.DotNet.Cli.Build
{
    public static class NuGetUtil
    {
        public enum NuGetIncludePackageType
        {
            Standard = 1,
            Symbols
        }
        public static void PushPackages(
            string packageDirPath,
            string destinationUrl,
            string apiKey,
            NuGetIncludePackageType includePackageTypes)
        {
            List<string> paths = new List<string>();
            if ((includePackageTypes & NuGetIncludePackageType.Standard) != 0)
            {
                paths.AddRange(Directory.GetFiles(packageDirPath, "*.nupkg").Where(p => !p.EndsWith(".symbols.nupkg")));
            }
            if ((includePackageTypes & NuGetIncludePackageType.Symbols) != 0)
            {
                paths.AddRange(Directory.GetFiles(packageDirPath, "*.symbols.nupkg"));
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

