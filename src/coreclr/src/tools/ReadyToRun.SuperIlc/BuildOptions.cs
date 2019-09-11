// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace ReadyToRun.SuperIlc
{
    public class BuildOptions
    {
        public DirectoryInfo InputDirectory { get; set; }
        public DirectoryInfo OutputDirectory { get; set; }
        public DirectoryInfo CoreRootDirectory { get; set; }
        public DirectoryInfo CpaotDirectory { get; set; }
        public bool Crossgen { get; set; }
        public bool NoJit { get; set; }
        public bool NoExe { get; set; }
        public bool NoEtw { get; set; }
        public bool NoCleanup { get; set; }
        public FileInfo PackageList { get; set; }
        public int DegreeOfParallelism { get; set; }
        public bool Sequential { get; set; }
        public bool Framework { get; set; }
        public bool UseFramework { get; set; }
        public bool Release { get; set; }
        public bool LargeBubble { get; set; }
        public int CompilationTimeoutMinutes { get; set; }
        public int ExecutionTimeoutMinutes { get; set; }
        public DirectoryInfo[] ReferencePath { get; set; }
        public FileInfo[] IssuesPath { get; set; }
        public FileInfo R2RDumpPath { get; set; }
        public FileInfo CrossgenResponseFile { get; set; }
        public DirectoryInfo[] RewriteOldPath { get;set; }
        public DirectoryInfo[] RewriteNewPath { get;set; }
        public string ConfigurationSuffix => (Release ? "-ret.out" : "-chk.out");

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
        public IEnumerable<CompilerRunner> CompilerRunners(bool isFramework, IEnumerable<string> overrideReferencePaths = null)
        {
            List<CompilerRunner> runners = new List<CompilerRunner>();

            if (CpaotDirectory != null)
            {
                List<string> referencePaths = new List<string>();
                referencePaths.Add(CoreRootOutputPath(CompilerIndex.CPAOT, isFramework));
                referencePaths.AddRange(overrideReferencePaths != null ? overrideReferencePaths : ReferencePaths());
                runners.Add(new CpaotRunner(this, referencePaths));
            }

            if (Crossgen)
            {
                if (CoreRootDirectory == null)
                {
                    throw new Exception("-coreroot folder not specified, cannot use Crossgen runner");
                }
                List<string> referencePaths = new List<string>();
                referencePaths.Add(CoreRootOutputPath(CompilerIndex.Crossgen, isFramework));
                referencePaths.AddRange(overrideReferencePaths != null ? overrideReferencePaths : ReferencePaths());
                runners.Add(new CrossgenRunner(this, referencePaths));
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
            string coreRunExe = "corerun".OSExeSuffix();
            string coreRunPath = Path.Combine(coreRunDir, coreRunExe);
            if (!File.Exists(coreRunPath))
            {
                Console.Error.WriteLine($@"{coreRunExe} not found in {coreRunDir}, explicit exe launches won't work");
            }
            return coreRunPath;
        }
    }
}
