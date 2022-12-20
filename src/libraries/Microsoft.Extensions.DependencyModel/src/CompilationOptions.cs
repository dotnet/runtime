// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public class CompilationOptions
    {
        public IReadOnlyList<string?> Defines { get; }

        public string? LanguageVersion { get; }

        public string? Platform { get; }

        public bool? AllowUnsafe { get; }

        public bool? WarningsAsErrors { get; }

        public bool? Optimize { get; }

        public string? KeyFile { get; }

        public bool? DelaySign { get; }

        public bool? PublicSign { get; }

        public string? DebugType { get; }

        public bool? EmitEntryPoint { get; }

        public bool? GenerateXmlDocumentation { get; }

        public static CompilationOptions Default { get; } = new CompilationOptions(
            defines: Enumerable.Empty<string?>(),
            languageVersion: null,
            platform: null,
            allowUnsafe: null,
            warningsAsErrors: null,
            optimize: null,
            keyFile: null,
            delaySign: null,
            publicSign: null,
            debugType: null,
            emitEntryPoint: null,
            generateXmlDocumentation: null);

        public CompilationOptions(IEnumerable<string?> defines,
            string? languageVersion,
            string? platform,
            bool? allowUnsafe,
            bool? warningsAsErrors,
            bool? optimize,
            string? keyFile,
            bool? delaySign,
            bool? publicSign,
            string? debugType,
            bool? emitEntryPoint,
            bool? generateXmlDocumentation)
        {
            ThrowHelper.ThrowIfNull(defines);

            Defines = defines.ToArray();
            LanguageVersion = languageVersion;
            Platform = platform;
            AllowUnsafe = allowUnsafe;
            WarningsAsErrors = warningsAsErrors;
            Optimize = optimize;
            KeyFile = keyFile;
            DelaySign = delaySign;
            PublicSign = publicSign;
            DebugType = debugType;
            EmitEntryPoint = emitEntryPoint;
            GenerateXmlDocumentation = generateXmlDocumentation;
        }
    }
}
