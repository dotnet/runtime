using System.IO;
using System.Reflection;
using Microsoft.DotNet.Cli.Build.Framework;
using NugetProgram = NuGet.CommandLine.XPlat.Program;

namespace Microsoft.DotNet.Cli.Build
{
    public static class NuGetUtil
    {
        public static void PushPackages(string packagesPath, string destinationUrl, string apiKey)
        {
            int result = RunNuGetCommand(
                "push",
                "-s", destinationUrl,
                "-k", apiKey,
                Path.Combine(packagesPath, "*.nupkg"));

            if (result != 0)
            {
                throw new BuildFailureException($"NuGet Push failed with exit code '{result}'.");
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

