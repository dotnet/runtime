// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;
using Internal.TypeSystem.Interop;

namespace ILCompiler.PortableCallHelpers
{
    /// <summary>
    /// Options for <see cref="PortableCallHelpersGenerator"/>, mirroring the command line.
    /// </summary>
    public sealed class PortableCallHelpersGeneratorOptions
    {
        public string OutputDirectory { get; init; }
        public IReadOnlyList<string> PInvokeModules { get; init; } = [];
        public string TargetOS { get; init; }
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
    public static class PortableCallHelpersGenerator
    {
        public const string PInvokeFileName = "callhelpers-pinvoke.cpp";
        public const string ReversePInvokeFileName = "callhelpers-reverse.cpp";
        public const string InterpToNativeFileName = "callhelpers-interp-to-managed.cpp";

        public static int Run(ReadyToRunCompilerContext context, PortableCallHelpersGeneratorOptions options, Logger logger)
        {
            var log = new InteropLogger(logger);

            try
            {
                // An empty directory would quietly write the files next to whatever the current
                // directory happens to be, so name it as the error it is.
                if (string.IsNullOrEmpty(options.OutputDirectory))
                    throw new LogAsErrorException("--generate-portable-callhelpers needs a directory to write to.");

                // The scan reads platform attributes against this name, and an empty one matches
                // nothing, which would silently drop every method guarded by SupportedOSPlatform.
                if (string.IsNullOrEmpty(options.TargetOS))
                    throw new LogAsErrorException("--generate-portable-callhelpers needs a target OS to match platform attributes against.");

                Generate(context, options, log);
                return 0;
            }
            catch (LogAsErrorException ex)
            {
                logger.LogMessage($"crossgen2 : error : {ex.Message}");
                return 1;
            }
        }

        private static void Generate(ReadyToRunCompilerContext context, PortableCallHelpersGeneratorOptions options, InteropLogger log)
        {
            ConfigureCompilationGroup(context);

            var collector = new PInvokeCollector(log, options.TargetOS);
            var internalCallCollector = new InternalCallSignatureCollector(log);

            List<PInvokeInfo> pinvokes = [];
            List<PInvokeCallback> callbacks = [];
            Dictionary<string, MethodDesc> signatures = [];

            foreach (string simpleName in context.InputFilePaths.Keys)
            {
                EcmaModule module = context.GetModuleForSimpleName(simpleName);

                // Only System.Private.CoreLib is scanned for InternalCall methods: all the ones that
                // are used are defined there, scanning everything is expensive, and doing so can hit
                // failures on assemblies that are not tested for it.
                bool scanInternalCalls = module == context.SystemModule;

                log.Verbose($"Scanning {simpleName} for pinvokes{(scanInternalCalls ? " and InternalCall methods" : "")}");

                int pinvokesFromOtherModules = pinvokes.Count;

                foreach (MetadataType type in module.GetAllTypes())
                {
                    if (type is not EcmaType ecmaType)
                        continue;

                    collector.CollectPInvokes(pinvokes, callbacks, signatures, ecmaType);

                    if (scanInternalCalls)
                        internalCallCollector.ScanType(ecmaType);
                }

                // WASM-TODO: The helpers describe every P/Invoke with the signature the type system
                // reports, which is what native sees only when the module disables runtime
                // marshalling. Make this check per-P/Invoke and marshalling-aware (see
                // Marshaller.IsMarshallingRequired), then raise it to a warning. It stays a message
                // while it names whole framework assemblies, which ship prebuilt and would fail
                // builds nobody can fix. Tracked by https://github.com/dotnet/runtime/issues/133190.
                if (pinvokes.Count != pinvokesFromOtherModules && MarshalHelpers.IsRuntimeMarshallingEnabled(module))
                {
                    log.InfoHigh("WASM0065",
                        $"'{simpleName}' declares P/Invokes without [assembly: DisableRuntimeMarshalling]; the generated helpers assume its signatures cross to native unmarshalled.");
                }
            }

            var generator = new PInvokeTableGenerator(log);

            WriteIfDifferent(Path.Combine(options.OutputDirectory, PInvokeFileName), log,
                w => generator.EmitPInvokeTable(w, options.PInvokeModules, pinvokes));

            WriteIfDifferent(Path.Combine(options.OutputDirectory, ReversePInvokeFileName), log,
                w => generator.EmitNativeToInterp(w, callbacks));

            foreach (KeyValuePair<string, MethodDesc> internalCall in internalCallCollector.Signatures)
                signatures.TryAdd(internalCall.Key, internalCall.Value);

            WriteIfDifferent(Path.Combine(options.OutputDirectory, InterpToNativeFileName), log,
                w => InterpToNativeGenerator.Emit(w, signatures));
        }

        /// <summary>
        /// Writes generated content to <paramref name="path"/> only when it differs from what is
        /// already there, so that an unchanged file keeps its timestamp and does not retrigger the
        /// native build that consumes it.
        /// </summary>
        private static void WriteIfDifferent(string path, InteropLogger log, Action<TextWriter> emit)
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
            List<EcmaModule> modules = new(context.InputFilePaths.Count);
            foreach (string simpleName in context.InputFilePaths.Keys)
                modules.Add(context.GetModuleForSimpleName(simpleName));

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
