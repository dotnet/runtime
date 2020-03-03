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

        protected override string CompilerRelativePath => "crossgen2";

        protected override string CompilerFileName => "crossgen2".AppendOSExeSuffix();

        private List<string> _resolvedReferences;

        public CpaotRunner(BuildOptions options, IEnumerable<string> referencePaths)
            : base(options, referencePaths)
        {
            // Set SuperIlc parallelism to a low enough value that ensures that each Crossgen2 invocation gets to use its parallelism
            if (options.DegreeOfParallelism == 0)
                options.DegreeOfParallelism = 2;
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

            char referenceOption = (_options.Composite ? 'u' : 'r');
            HashSet<string> uniqueFolders = new HashSet<string>();
            foreach (string assemblyFileName in assemblyFileNames)
            {
                uniqueFolders.Add(Path.GetDirectoryName(assemblyFileName));
            }
            foreach (string folder in uniqueFolders)
            {
                foreach (var reference in ComputeManagedAssemblies.GetManagedAssembliesInFolder(folder))
                {
                    yield return $"-{referenceOption}:{reference}";
                }
            }

            if (_resolvedReferences == null)
            {
                _resolvedReferences = ResolveReferences();
            }

            foreach (string asmRef in _resolvedReferences)
            {
                yield return asmRef;
            }
        }

        private List<string> ResolveReferences()
        {
            char referenceOption = (_options.Composite ? 'u' : 'r');
            List<string> references = new List<string>();
            foreach (var referenceFolder in _referenceFolders)
            {
                foreach (var reference in ComputeManagedAssemblies.GetManagedAssembliesInFolder(referenceFolder))
                {
                    references.Add($"-{referenceOption}:{reference}");
                }
            }
            return references;
        }
    }
}
