// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel
{
    public class CompilationOptions
    {
        public IEnumerable<string> Defines { get; }

        public string LanguageVersion { get; }

        public string Platform { get; }

        public bool? AllowUnsafe { get; }

        public bool? WarningsAsErrors { get; }

        public bool? Optimize { get; }

        public string KeyFile { get; }

        public bool? DelaySign { get; }

        public bool? PublicSign { get; }

        public bool? EmitEntryPoint { get; }

        public CompilationOptions(IEnumerable<string> defines,
            string languageVersion,
            string platform,
            bool? allowUnsafe,
            bool? warningsAsErrors,
            bool? optimize,
            string keyFile,
            bool? delaySign,
            bool? publicSign,
            bool? emitEntryPoint)
        {
            Defines = defines;
            LanguageVersion = languageVersion;
            Platform = platform;
            AllowUnsafe = allowUnsafe;
            WarningsAsErrors = warningsAsErrors;
            Optimize = optimize;
            KeyFile = keyFile;
            DelaySign = delaySign;
            PublicSign = publicSign;
            EmitEntryPoint = emitEntryPoint;
        }
    }
}