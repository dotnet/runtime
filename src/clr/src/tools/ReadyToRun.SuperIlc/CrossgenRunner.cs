// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ReadyToRun.SuperIlc
{
    /// <summary>
    /// Compiles assemblies using the Cross-Platform AOT compiler
    /// </summary>
    class CrossgenRunner : CompilerRunner
    {
        public override CompilerIndex Index => CompilerIndex.Crossgen;

        protected override string CompilerRelativePath => ".";

        protected override string CompilerFileName => "crossgen".OSExeSuffix();

        protected override string CompilerPath
        {
            get
            {
                return _options.CrossgenPath != null ? _options.CrossgenPath.FullName : base.CompilerPath;
            }
        }

        public CrossgenRunner(BuildOptions options, IEnumerable<string> referencePaths)
            : base(options, referencePaths) { }

        protected override ProcessParameters ExecutionProcess(IEnumerable<string> modules, IEnumerable<string> folders, bool noEtw)
        {
            ProcessParameters processParameters = base.ExecutionProcess(modules, folders, noEtw);
            processParameters.EnvironmentOverrides["COMPLUS_ReadyToRun"] = "1";
            processParameters.EnvironmentOverrides["COMPLUS_NoGuiOnAssert"] = "1";
            return processParameters;
        }

        protected override IEnumerable<string> BuildCommandLineArguments(string assemblyFileName, string outputFileName)
        {
            // The file to compile
            yield return "/in";
            yield return assemblyFileName;

            // Output
            yield return "/out";
            yield return outputFileName;

            if (_options.LargeBubble && Path.GetFileNameWithoutExtension(assemblyFileName) != "System.Private.CoreLib")
            {
                // There seems to be a bug in Crossgen on Linux we don't intend to fix -
                // it crashes when trying to compile S.P.C in large version bubble mode.
                yield return "/largeversionbubble";
            }

            yield return "/platform_assemblies_paths";

            IEnumerable<string> paths = new string[] { Path.GetDirectoryName(assemblyFileName) }.Concat(_referenceFolders);

            yield return paths.ConcatenatePaths();
        }
    }
}
