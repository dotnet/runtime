// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReadyToRun.SuperIlc
{
    /// <summary>
    /// Compiles assemblies using the Cross-Platform AOT compiler
    /// </summary>
    class CpaotRunner : CompilerRunner
    {
        public override CompilerIndex Index => CompilerIndex.CPAOT;

        // Crossgen2 runs on top of corerun.
        protected override string CompilerRelativePath => "";

        protected override string CompilerFileName => "corerun".AppendOSExeSuffix();

        private string Crossgen2Path => Path.Combine(_options.CoreRootDirectory.FullName, "crossgen2", "crossgen2.dll");

        private IEnumerable<string> _resolvedReferences;

        public CpaotRunner(BuildOptions options, IEnumerable<string> referencePaths)
            : base(options, referencePaths)
        {
            // Set SuperIlc parallelism to a low enough value that ensures that each Crossgen2 invocation gets to use its parallelism
            if (options.DegreeOfParallelism == 0)
                options.DegreeOfParallelism = 2;
        }

        public override ProcessParameters CompilationProcess(string outputFileName, IEnumerable<string> inputAssemblyFileNames)
        {
            ProcessParameters processParameters = base.CompilationProcess(outputFileName, inputAssemblyFileNames);
            processParameters.Arguments = $"{Crossgen2Path} {processParameters.Arguments}";
            return processParameters;
        }

        protected override ProcessParameters ExecutionProcess(IEnumerable<string> modules, IEnumerable<string> folders, bool noEtw)
        {
            ProcessParameters processParameters = base.ExecutionProcess(modules, folders, noEtw);
            processParameters.EnvironmentOverrides["COMPLUS_ReadyToRun"] = "1";
            return processParameters;
        }

        protected override IEnumerable<string> BuildCommandLineArguments(IEnumerable<string> assemblyFileNames, string outputFileName)
        {
            // The file to compile
            foreach (string inputAssembly in assemblyFileNames)
            {
                yield return inputAssembly;
            }

            // Output
            yield return $"-o:{outputFileName}";

            // Todo: Allow control of some of these
            yield return "--targetarch=x64";

            if (_options.Map)
            {
                yield return "--map";
            }

            if (_options.Release)
            {
                yield return "-O";
            }

            if (_options.LargeBubble || _options.Composite)
            {
                yield return "--inputbubble";
            }

            if (_options.Composite)
            {
                yield return "--composite";
            }

            if (_options.Crossgen2Parallelism != 0)
            {
                yield return $"--parallelism={_options.Crossgen2Parallelism}";
            }

            HashSet<string> uniqueFolders = new HashSet<string>();
            foreach (string assemblyFileName in assemblyFileNames)
            {
                uniqueFolders.Add(Path.GetDirectoryName(assemblyFileName));
            }

            foreach (string reference in ResolveReferences(uniqueFolders))
            {
                yield return reference;
            }

            if (_resolvedReferences == null)
            {
                _resolvedReferences = ResolveReferences(_referenceFolders);
            }

            foreach (string asmRef in _resolvedReferences)
            {
                yield return asmRef;
            }
        }

        private IEnumerable<string> ResolveReferences(IEnumerable<string> folders)
        {
            char referenceOption = (_options.Composite ? 'u' : 'r');

            foreach (string referenceFolder in folders)
            {
                foreach (string reference in ComputeManagedAssemblies.GetManagedAssembliesInFolder(referenceFolder))
                {
                    string simpleName = Path.GetFileNameWithoutExtension(reference);
                    if (!FrameworkExclusion.Exclude(simpleName, Index, out string reason))
                    {
                        yield return $"-{referenceOption}:{reference}";
                    }
                }
            }
        }
    }
}
