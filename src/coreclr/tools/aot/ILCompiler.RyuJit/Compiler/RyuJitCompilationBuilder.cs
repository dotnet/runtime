// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Internal.IL;
using Internal.JitInterface;
using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class RyuJitCompilationBuilder : CompilationBuilder
    {
        // These need to provide reasonable defaults so that the user can optionally skip
        // calling the Use/Configure methods and still get something reasonable back.
        private KeyValuePair<string, string>[] _ryujitOptions = Array.Empty<KeyValuePair<string, string>>();
        private MethodLayoutAlgorithm _methodLayoutAlgorithm;
        private FileLayoutAlgorithm _fileLayoutAlgorithm;
        private ILProvider _ilProvider = new NativeAotILProvider();
        private ProfileDataManager _profileDataManager;
        private string _orderFile;
        private string _jitPath;
        private string _incrementalCommandLineConfiguration;

        public RyuJitCompilationBuilder(CompilerTypeSystemContext context, CompilationModuleGroup group)
            : base(context, group,
                  new NativeAotNameMangler(context.Target.IsWindows ? (NodeMangler)new WindowsNodeMangler(context.Target) : (NodeMangler)new UnixNodeMangler()))
        {
        }

        public RyuJitCompilationBuilder UseProfileData(IEnumerable<string> mibcFiles)
        {
            _profileDataManager = new ProfileDataManager(mibcFiles, _context);
            return this;
        }

        public RyuJitCompilationBuilder UseSymbolOrder(string filePath)
        {
            _orderFile = filePath;
            return this;
        }

        public RyuJitCompilationBuilder UseJitPath(string jitPath)
        {
            _jitPath = jitPath;
            return this;
        }

        public RyuJitCompilationBuilder FileLayoutAlgorithms(MethodLayoutAlgorithm methodLayoutAlgorithm, FileLayoutAlgorithm fileLayoutAlgorithm)
        {
            _methodLayoutAlgorithm = methodLayoutAlgorithm;
            _fileLayoutAlgorithm = fileLayoutAlgorithm;
            return this;
        }

        internal void SetIncrementalCommandLineConfiguration(string configurationDescription)
        {
            ArgumentException.ThrowIfNullOrEmpty(configurationDescription);
            _incrementalCommandLineConfiguration = configurationDescription;
        }

        internal static bool TryValidateIncrementalCommandLineConfiguration(
            bool exports,
            bool dependencyGraph,
            bool scannerDependencyGraph,
            bool ilDump,
            bool map,
            bool mstat,
            bool sourceLink,
            bool metadataLog,
            bool reachability,
            out string description,
            out string reason)
        {
            description =
                $"exports={exports};dependencygraph={dependencyGraph};" +
                $"scannerdependencygraph={scannerDependencyGraph};ildump={ilDump};map={map};" +
                $"mstat={mstat};sourcelink={sourceLink};metadatalog={metadataLog};" +
                $"reachability={reachability}";

            if (exports ||
                dependencyGraph ||
                scannerDependencyGraph ||
                ilDump ||
                map ||
                mstat ||
                sourceLink ||
                metadataLog ||
                reachability)
            {
                reason = "exports, dependency logs, IL dumps, map/mstat/SourceLink/metadata logs, and reachability instrumentation are unsupported";
                return false;
            }

            reason = null;
            return true;
        }

        public override CompilationBuilder UseBackendOptions(IEnumerable<string> options)
        {
            var builder = default(ArrayBuilder<KeyValuePair<string, string>>);

            foreach (string param in options)
            {
                int indexOfEquals = param.IndexOf('=');

                // We're skipping bad parameters without reporting.
                // This is not a mainstream feature that would need to be friendly.
                // Besides, to really validate this, we would also need to check that the config name is known.
                if (indexOfEquals < 1)
                    continue;

                string name = param.Substring(0, indexOfEquals);
                string value = param.Substring(indexOfEquals + 1);

                builder.Add(new KeyValuePair<string, string>(name, value));
            }

            _ryujitOptions = builder.ToArray();

            return this;
        }

        public override CompilationBuilder UseILProvider(ILProvider ilProvider)
        {
            _ilProvider = ilProvider;
            return this;
        }

        protected override ILProvider GetILProvider()
        {
            return _ilProvider;
        }

        public override ICompilation ToCompilation()
        {
            IncrementalCompilationOptions incrementalOptions;
            try
            {
                incrementalOptions = IncrementalCompilationOptions.ReadEnvironment();
            }
            catch (InvalidOperationException ex)
            {
                throw new IncrementalCompilationException(ex.Message);
            }
            string incrementalConfiguration = null;
            if (incrementalOptions is not null)
            {
                if (_incrementalCommandLineConfiguration is null)
                {
                    throw new IncrementalCompilationException(
                        "the driver did not validate and fingerprint command-line configuration");
                }

                if (!IncrementalBuilderAccess.TryGetBaseConfiguration(
                    this,
                    out string baseConfiguration,
                    out string baseReason))
                {
                    throw new IncrementalCompilationException(baseReason);
                }

                string unsupportedReason = GetIncrementalCompilationUnsupportedReason();
                if (unsupportedReason is not null)
                    throw new IncrementalCompilationException(unsupportedReason);

                incrementalConfiguration =
                    $"{baseConfiguration};{GetIncrementalConfigurationDescription()};" +
                    _incrementalCommandLineConfiguration;
            }

            ArrayBuilder<CorJitFlag> jitFlagBuilder = default(ArrayBuilder<CorJitFlag>);

            switch (_optimizationMode)
            {
                case OptimizationMode.None:
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_DEBUG_CODE);
                    break;

                case OptimizationMode.PreferSize:
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_SIZE_OPT);
                    break;

                case OptimizationMode.PreferSpeed:
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_SPEED_OPT);
                    break;

                default:
                    // Not setting a flag results in BLENDED_CODE.
                    break;
            }

            if (_optimizationMode != OptimizationMode.None && _profileDataManager != null)
            {
                jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_BBOPT);
            }

            // Do not bother with debug information if the debug info provider never gives anything.
            if (!(_debugInformationProvider is NullDebugInformationProvider))
                jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_DEBUG_INFO);

            RyuJitCompilationOptions options = 0;
            if ((_mitigationOptions & SecurityMitigationOptions.ControlFlowGuardAnnotations) != 0)
            {
                jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_ENABLE_CFG);
                options |= RyuJitCompilationOptions.ControlFlowGuardAnnotations;
            }

            if (_useDwarf5)
                options |= RyuJitCompilationOptions.UseDwarf5;

            if (_resilient)
                options |= RyuJitCompilationOptions.UseResilience;

            MethodBodyDeduplicator methodBodyDeduplicator = _methodBodyFolding switch
            {
                MethodBodyFoldingMode.Generic => new MethodBodyDeduplicator(genericsOnly: true),
                MethodBodyFoldingMode.All => new MethodBodyDeduplicator(genericsOnly: false),
                _ => null,
            };

            ObjectDataInterner interner = methodBodyDeduplicator is not null
                ? new ObjectDataInterner(methodBodyDeduplicator)
                : ObjectDataInterner.Null;

            var factory = new RyuJitNodeFactory(_context, _compilationGroup, _metadataManager, _interopStubManager, _nameMangler, _vtableSliceProvider, _dictionaryLayoutProvider, _inlinedThreadStatics, GetPreinitializationManager(), _devirtualizationManager, interner, methodBodyDeduplicator, _typeMapManager);

            JitConfigProvider.Initialize(_context.Target, jitFlagBuilder.ToArray(), _ryujitOptions, _jitPath);
            DependencyAnalyzerBase<NodeFactory> graph = CreateDependencyGraph(factory, new ObjectNode.ObjectNodeComparer(CompilerComparer.Instance));
            return new RyuJitCompilation(graph,
                factory,
                [.._compilationRoots, _typeMapManager],
                _ilProvider,
                _debugInformationProvider,
                _logger,
                _inliningPolicy ?? _compilationGroup,
                _instructionSetSupport,
                _profileDataManager,
                _methodImportationErrorProvider,
                _readOnlyFieldPolicy,
                options,
                _methodLayoutAlgorithm,
                _fileLayoutAlgorithm,
                _parallelism,
                _orderFile,
                incrementalOptions,
                incrementalConfiguration);
        }

        private string GetIncrementalCompilationUnsupportedReason()
        {
            if (!OperatingSystem.IsWindows() ||
                _context.Target.OperatingSystem != TargetOS.Windows ||
                _context.Target.Architecture != TargetArchitecture.X64 ||
                _context.Target.Abi != TargetAbi.NativeAot)
            {
                return "only a Windows host targeting Windows x64 NativeAOT COFF is supported";
            }
            if (_context.InputFilePaths.Count != 1 ||
                _compilationGroup is not SingleFileCompilationModuleGroup)
            {
                return "a single primary input in a single-file compilation is required";
            }
            if (_optimizationMode != OptimizationMode.None)
                return "optimization must be disabled";
            if (_parallelism != 1)
                return "parallelism must be exactly one";
            if (_inliningPolicy is not null)
                return "the scanner and custom inlining policies must be disabled";
            if (_methodBodyFolding != MethodBodyFoldingMode.None)
                return "method-body folding must be disabled";
            if (_debugInformationProvider is not NullDebugInformationProvider)
                return "native debug information must be disabled";
            if (_mitigationOptions != 0 || _dehydrate || _useDwarf5 || _resilient)
                return "security mitigations, dehydration, resilience, and DWARF options are unsupported";
            if (_profileDataManager is not null || _orderFile is not null)
                return "profile data and custom file ordering are unsupported";
            if (_methodLayoutAlgorithm != MethodLayoutAlgorithm.DefaultSort ||
                _fileLayoutAlgorithm != FileLayoutAlgorithm.DefaultSort)
            {
                return "default method and file layouts are required";
            }
            if (_ryujitOptions.Length != 0 || _jitPath is not null)
                return "custom JIT options and JIT paths are unsupported";

            return null;
        }

        private string GetIncrementalConfigurationDescription()
        {
            var builder = new StringBuilder();
            builder.Append("optimization=").Append((int)_optimizationMode);
            builder.Append(";parallelism=").Append(_parallelism);
            builder.Append(";group=").Append(_compilationGroup.GetType().AssemblyQualifiedName);
            builder.Append(";metadata=").Append(_metadataManager.GetType().AssemblyQualifiedName);
            builder.Append(";interop=").Append(_interopStubManager.GetType().AssemblyQualifiedName);
            builder.Append(";vtable=").Append(_vtableSliceProvider.GetType().AssemblyQualifiedName);
            builder.Append(";dictionary=").Append(_dictionaryLayoutProvider.GetType().AssemblyQualifiedName);
            builder.Append(";threadstatics=").Append(_inlinedThreadStatics.GetType().AssemblyQualifiedName);
            builder.Append(";devirtualization=").Append(_devirtualizationManager.GetType().AssemblyQualifiedName);
            builder.Append(";typemap=").Append(_typeMapManager.GetType().AssemblyQualifiedName);
            builder.Append(";readonly=").Append(_readOnlyFieldPolicy.GetType().AssemblyQualifiedName);
            builder.Append(";methodimport=").Append(_methodImportationErrorProvider.GetType().AssemblyQualifiedName);
            builder.Append(";methodlayout=").Append((int)_methodLayoutAlgorithm);
            builder.Append(";filelayout=").Append((int)_fileLayoutAlgorithm);
            builder.Append(";folding=").Append((int)_methodBodyFolding);
            builder.Append(";mitigations=").Append((int)_mitigationOptions);
            return builder.ToString();
        }

        private static class IncrementalBuilderAccess
        {
            // ILCompiler.Compiler and ILCompiler.RyuJit compile overlapping linked sources, so
            // InternalsVisibleTo would make duplicate internal types ambiguous. Keep this
            // experiment-only boundary internal and fail loudly if the reflected seam changes.
            private static readonly MethodInfo s_getBaseConfiguration =
                typeof(CompilationBuilder).GetMethod(
                    "TryGetIncrementalBaseConfiguration",
                    BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new MissingMethodException(
                    typeof(CompilationBuilder).FullName,
                    "TryGetIncrementalBaseConfiguration");

            internal static bool TryGetBaseConfiguration(
                CompilationBuilder builder,
                out string description,
                out string reason)
            {
                object[] arguments = { null, null };
                bool result;
                try
                {
                    result = (bool)s_getBaseConfiguration.Invoke(builder, arguments);
                }
                catch (TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    throw;
                }

                description = (string)arguments[0];
                reason = (string)arguments[1];
                return result;
            }
        }
    }
}
