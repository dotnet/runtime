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

        protected override string CompilerFileName => "crossgen2".OSExeSuffix();

        private List<string> _resolvedReferences;

        public CpaotRunner(BuildOptions options, IEnumerable<string> referencePaths)
            : base(options, referencePaths)
        { }

        protected override ProcessParameters ExecutionProcess(IEnumerable<string> modules, IEnumerable<string> folders, bool noEtw)
        {
            ProcessParameters processParameters = base.ExecutionProcess(modules, folders, noEtw);
            processParameters.EnvironmentOverrides["COMPLUS_ReadyToRun"] = "1";
            return processParameters;
        }

        protected override IEnumerable<string> BuildCommandLineArguments(string assemblyFileName, string outputFileName)
        {
            // The file to compile
            yield return assemblyFileName;

            // Output
            yield return $"-o:{outputFileName}";

            // Todo: Allow control of some of these
            yield return "--targetarch=x64";

            if (_options.Release)
            {
                yield return "-O";
            }

            if (_options.LargeBubble)
            {
                yield return "--inputbubble";
            }

            foreach (var reference in ComputeManagedAssemblies.GetManagedAssembliesInFolder(Path.GetDirectoryName(assemblyFileName)))
            {
                yield return $"-r:{reference}";
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
            List<string> references = new List<string>();
            foreach (var referenceFolder in _referenceFolders)
            {
                foreach (var reference in ComputeManagedAssemblies.GetManagedAssembliesInFolder(referenceFolder))
                {
                    references.Add($"-r:{reference}");
                }
            }
            return references;
        }
    }
}
