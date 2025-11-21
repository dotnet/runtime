// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class PInvokeTableGeneratorTestsBase : WasmTemplateTestsBase
    {
        public PInvokeTableGeneratorTestsBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        protected string PublishForVariadicFunctionTests(ProjectInfo info, Configuration config, bool aot, string? verbosity = null, bool isNativeBuild = true)
        {
            string verbosityArg = verbosity == null ? string.Empty : $" -v:{verbosity}";
            // NativeFileReference forces native build
            (_, string output) = PublishProject(info,
                config,
                new PublishOptions(ExtraMSBuildArgs: verbosityArg, AOT: aot),
                isNativeBuild: isNativeBuild);
            return output;
        }
    }
}
