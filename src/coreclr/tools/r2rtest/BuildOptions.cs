// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace R2RTest
{
    public partial class BuildOptions
    {
        public string ConfigurationSuffix => (Release ? "-ret.out" : "-chk.out");

        public string DotNetCli
        {
            get
            {
                if (_dotnetCli == null)
                {
                    _dotnetCli = Process.GetCurrentProcess().MainModule.FileName;
                    Console.WriteLine($"Using dotnet: {_dotnetCli}");

                    if (Path.GetFileNameWithoutExtension(_dotnetCli).Equals("r2rtest", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException("Error: --dotnet-cli argument is required to run crossgen2. Cannot use the host running r2rtest itself when launched by its native app host.");
                    }
                }

                return _dotnetCli;
            } 
            set
            {
                _dotnetCli = value;
                Console.WriteLine($"Using dotnet: {_dotnetCli}");
            }
        }
        private string _dotnetCli;


        public IEnumerable<string> ReferencePaths()
        {
            if (ReferencePath != null)
            {
                foreach (DirectoryInfo referencePath in ReferencePath)
                {
                    yield return referencePath.FullName;
                }
            }
        }

        /// <summary>
        /// Construct CoreRoot native path for a given compiler runner.
        /// </summary>
        /// <param name="index">Compiler runner index</param>
        /// <returns></returns>
        public string CoreRootOutputPath(CompilerIndex index, bool isFramework)
        {
            if (CoreRootDirectory == null)
            {
                return null;
            }

            string outputPath = CoreRootDirectory.FullName;
            if (!isFramework && (Framework || UseFramework))
            {
                outputPath = Path.Combine(outputPath, index.ToString() + ConfigurationSuffix);
            }
            return outputPath;
        }

        /// <summary>
        /// Creates compiler runner instances for each supported compiler based on the populated BuildOptions.
        /// </summary>
        /// <param name="isFramework">True if compiling the CoreFX framework assemblies</param>
        /// <param name="referencePaths">Optional set of reference paths to use instead of BuildOptions.ReferencePaths()</param>
        public IEnumerable<CompilerRunner> CompilerRunners(bool isFramework, IEnumerable<string> overrideReferencePaths = null, string overrideOutputPath = null)
        {
            List<CompilerRunner> runners = new List<CompilerRunner>();

            if (!NoCrossgen2)
            {
                List<string> cpaotReferencePaths = new List<string>();
                cpaotReferencePaths.Add(CoreRootOutputPath(CompilerIndex.CPAOT, isFramework));
                cpaotReferencePaths.AddRange(overrideReferencePaths != null ? overrideReferencePaths : ReferencePaths());
                runners.Add(new Crossgen2Runner(this, new Crossgen2RunnerOptions() { Composite = this.Composite }, cpaotReferencePaths, overrideOutputPath));
            }

            if (!NoJit)
            {
                runners.Add(new JitRunner(this));
            }

            return runners;
        }

        public string CoreRunPath(CompilerIndex index, bool isFramework)
        {
            string coreRunDir = CoreRootOutputPath(index, isFramework);
            string coreRunExe = "corerun".AppendOSExeSuffix();
            string coreRunPath = Path.Combine(coreRunDir, coreRunExe);
            if (!File.Exists(coreRunPath))
            {
                Console.Error.WriteLine($@"{coreRunExe} not found in {coreRunDir}, explicit exe launches won't work");
            }
            return coreRunPath;
        }
    }
}
