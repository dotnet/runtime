// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.Wasm
{
    /// <summary>
    /// Options for <see cref="WasmInteropGenerator"/>, mirroring the command line.
    /// </summary>
    internal sealed class WasmInteropGeneratorOptions
    {
        public string OutputDirectory { get; init; }
        public IReadOnlyList<string> PInvokeModules { get; init; } = [];
        public IReadOnlyList<string> IgnoredPInvokeModules { get; init; } = [];
        public string TargetOS { get; init; }
        public bool WarnOnUnresolvedPInvokeModules { get; init; } = true;
    }

    /// <summary>
    /// Generates the C++ interop helpers the wasm interpreter needs: the static P/Invoke resolution
    /// table, the native-to-managed reverse thunks, and the interpreter-to-native call thunks.
    /// </summary>
    /// <remarks>
    /// This runs inside crossgen2 because the files it writes encode the wasm ABI - struct sizes,
    /// alignment, and how each type is passed - which cannot be derived from metadata alone. Doing
    /// it here means there is exactly one implementation of the lowering (<see cref="WasmLowering"/>,
    /// shared with compiled code) rather than a second one in a build task that would be free to
    /// drift from it.
    ///
    /// Usage:
    ///   crossgen2 --generate-portable-callhelpers &lt;dir&gt; --targetos &lt;browser|wasi&gt; --targetarch wasm \
    ///             --directpinvoke &lt;name&gt;... &lt;assembly&gt;...
    /// </remarks>
    internal static class WasmInteropGenerator
    {
        public const string PInvokeFileName = "callhelpers-pinvoke.cpp";
        public const string ReversePInvokeFileName = "callhelpers-reverse.cpp";
        public const string InterpToNativeFileName = "callhelpers-interp-to-managed.cpp";

        public static int Run(ReadyToRunCompilerContext context, WasmInteropGeneratorOptions options, Logger logger)
        {
            var log = new WasmInteropLogger(logger);

            try
            {
                Generate(context, options, log);
                return 0;
            }
            catch (LogAsErrorException ex)
            {
                logger.LogMessage($"crossgen2 : error : {ex.Message}");
                return 1;
            }
        }

        private static void Generate(ReadyToRunCompilerContext context, WasmInteropGeneratorOptions options, WasmInteropLogger log)
        {
            ConfigureCompilationGroup(context);

            var collector = new WasmPInvokeCollector(log, options.TargetOS);
            var internalCallCollector = new WasmInternalCallSignatureCollector(log);

            List<WasmPInvoke> pinvokes = [];
            List<WasmPInvokeCallback> callbacks = [];
            HashSet<string> signatures = [];

            foreach (string simpleName in context.InputFilePaths.Keys)
            {
                EcmaModule module = context.GetModuleForSimpleName(simpleName);

                // Only System.Private.CoreLib is scanned for InternalCall methods: all the ones that
                // are used are defined there, scanning everything is expensive, and doing so can hit
                // failures on assemblies that are not tested for it.
                bool scanInternalCalls = module == context.SystemModule;

                log.Verbose($"Scanning {simpleName} for pinvokes{(scanInternalCalls ? " and InternalCall methods" : "")}");

                foreach (MetadataType type in module.GetAllTypes())
                {
                    if (type is not EcmaType ecmaType)
                        continue;

                    collector.CollectPInvokes(pinvokes, callbacks, signatures, ecmaType);

                    if (scanInternalCalls)
                        internalCallCollector.ScanType(ecmaType);
                }
            }

            var generator = new WasmPInvokeTableGenerator(log, options.WarnOnUnresolvedPInvokeModules);

            WriteIfDifferent(Path.Combine(options.OutputDirectory, PInvokeFileName), log,
                w => generator.EmitPInvokeTable(w, options.PInvokeModules, options.IgnoredPInvokeModules, pinvokes));

            WriteIfDifferent(Path.Combine(options.OutputDirectory, ReversePInvokeFileName), log,
                w => generator.EmitNativeToInterp(w, callbacks));

            IEnumerable<string> cookies = signatures.Concat(internalCallCollector.Signatures);

            WriteIfDifferent(Path.Combine(options.OutputDirectory, InterpToNativeFileName), log,
                w => WasmInterpToNativeGenerator.Emit(w, cookies));
        }

        /// <summary>
        /// Writes generated content to <paramref name="path"/> only when it differs from what is
        /// already there, so that an unchanged file keeps its timestamp and does not retrigger the
        /// native build that consumes it.
        /// </summary>
        private static void WriteIfDifferent(string path, WasmInteropLogger log, Action<TextWriter> emit)
        {
            var buffer = new StringWriter { NewLine = Environment.NewLine };
            emit(buffer);
            string content = buffer.ToString();

            if (File.Exists(path) && File.ReadAllText(path) == content)
            {
                log.Verbose($"{path} is unchanged.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            log.Verbose($"Generated {path}.");
        }

        /// <summary>
        /// The ReadyToRun field layout algorithm asks the compilation group whether a derived type
        /// needs its base offset aligned, so a context without a group throws before computing any
        /// layout.
        /// </summary>
        /// <remarks>
        /// Every input goes into a single version bubble. The alignment the group would otherwise
        /// introduce exists to keep offsets baked into precompiled code valid across a version
        /// boundary, and there is no precompiled code here: the interpreter loads these assemblies
        /// and computes their layout itself. One bubble is what reports that layout.
        /// </remarks>
        private static void ConfigureCompilationGroup(ReadyToRunCompilerContext context)
        {
            List<EcmaModule> modules = context.InputFilePaths.Keys
                .Select(simpleName => context.GetModuleForSimpleName(simpleName))
                .ToList();

            context.SetCompilationGroup(new ReadyToRunSingleAssemblyCompilationModuleGroup(new ReadyToRunCompilationModuleGroupConfig
            {
                Context = context,
                // "Many inputs, one output unit" is what composite mode means, and it is what makes
                // the group treat every input as a single compilation unit. Without it the group
                // asserts on a compilation set larger than one assembly.
                IsCompositeBuildMode = true,
                IsInputBubble = true,
                CompilationModuleSet = modules,
                VersionBubbleModuleSet = modules,
                CrossModuleInlineable = [],
                InstructionSetSupport = context.InstructionSetSupport,
            }));
        }
    }
}
