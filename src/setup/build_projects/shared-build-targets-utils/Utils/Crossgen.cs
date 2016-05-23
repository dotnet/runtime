using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.InternalAbstractions;
using static Microsoft.DotNet.Cli.Build.Framework.BuildHelpers;

namespace Microsoft.DotNet.Cli.Build
{
    public class Crossgen
    {
        private string _coreClrVersion;
        private string _crossGenPath;

        // This is not always correct. The version of crossgen we need to pick up is whatever one was restored as part
        // of the Microsoft.NETCore.Runtime.CoreCLR package that is part of the shared library. For now, the version hardcoded
        // in CompileTargets and the one in the shared library project.json match and are updated in lock step, but long term
        // we need to be able to look at the project.lock.json file and figure out what version of Microsoft.NETCore.Runtime.CoreCLR
        // was used, and then select that version.
        public Crossgen(string coreClrVersion)
        {
            _coreClrVersion = coreClrVersion;
            _crossGenPath = GetCrossgenPathForVersion();
        }

        private string GetCrossgenPathForVersion()
        {
            string arch = RuntimeEnvironment.RuntimeArchitecture;
            string packageId;
            if (CurrentPlatform.IsWindows)
            {
                packageId = $"runtime.win7-{arch}.Microsoft.NETCore.Runtime.CoreCLR";
            }
            else if (CurrentPlatform.IsUbuntu)
            {
                packageId = "runtime.ubuntu.14.04-x64.Microsoft.NETCore.Runtime.CoreCLR";
            }
            else if (CurrentPlatform.IsCentOS || CurrentPlatform.IsRHEL)
            {
                // CentOS runtime is in the runtime.rhel.7-x64... package.
                packageId = "runtime.rhel.7-x64.Microsoft.NETCore.Runtime.CoreCLR";
            }
            else if (CurrentPlatform.IsOSX)
            {
                packageId = "runtime.osx.10.10-x64.Microsoft.NETCore.Runtime.CoreCLR";
            }
            else if (CurrentPlatform.IsDebian)
            {
                packageId = "runtime.debian.8-x64.Microsoft.NETCore.Runtime.CoreCLR";
            }
            else
            {
                return null;
            }

            return Path.Combine(
                Dirs.NuGetPackages,
                packageId,
                _coreClrVersion,
                "tools",
                $"crossgen{Constants.ExeSuffix}");
        }

        public void CrossgenDirectory(string sharedFxPath, string pathToAssemblies)
        {
            // Check if we need to skip crossgen
            if (string.Equals(Environment.GetEnvironmentVariable("DISABLE_CROSSGEN"), "1"))
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Skipping crossgen for because DISABLE_CROSSGEN is set to 1");
                Console.ForegroundColor = originalColor;
                return;
            }

            // HACK
            // The input directory can be a portable FAT app (example the CLI itself).
            // In that case there can be RID specific managed dependencies which are not right next to the app binary (example System.Diagnostics.TraceSource).
            // We need those dependencies during crossgen. For now we just pass all subdirectories of the input directory as input to crossgen.
            // The right fix -
            // If the assembly has deps.json then parse the json file to get all the dependencies, pass these dependencies as input to crossgen.
            // else pass the current directory of assembly as input to crossgen.
            var addtionalPaths = Directory.GetDirectories(pathToAssemblies, "*", SearchOption.AllDirectories).ToList();
            var paths = new List<string>() { sharedFxPath, pathToAssemblies };
            paths.AddRange(addtionalPaths);
            var platformAssembliesPaths = string.Join(Path.PathSeparator.ToString(), paths.Distinct());

            var env = new Dictionary<string, string>()
            {
                // disable partial ngen
                { "COMPlus_PartialNGen", "0" }
            };

            foreach (var file in Directory.GetFiles(pathToAssemblies))
            {
                string fileName = Path.GetFileName(file);

                if (fileName == "mscorlib.dll" || fileName == "mscorlib.ni.dll" || !PEUtils.HasMetadata(file))
                {
                    continue;
                }

                string tempPathName = Path.ChangeExtension(file, "readytorun");

                IList<string> crossgenArgs = new List<string> {
                    "-readytorun", "-in", file, "-out", tempPathName,
                    "-platform_assemblies_paths", platformAssembliesPaths
                };

                ExecSilent(_crossGenPath, crossgenArgs, env);

                File.Copy(tempPathName, file, overwrite: true);
                File.Delete(tempPathName);
            }
        }
    }
}
