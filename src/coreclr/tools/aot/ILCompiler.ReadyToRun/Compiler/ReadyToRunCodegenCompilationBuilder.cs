// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Win32Resources;
using Internal.IL;
using Internal.JitInterface;
using Internal.ReadyToRunConstants;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public sealed class ReadyToRunCodegenCompilationBuilder : CompilationBuilder
    {
        private static bool _isJitInitialized = false;

        private readonly IEnumerable<string> _inputFiles;
        private readonly string _compositeRootPath;
        private bool _ibcTuning;
        private bool _resilient;
        private bool _generateMapFile;
        private bool _generateMapCsvFile;
        private bool _generatePdbFile;
        private string _pdbPath;
        private bool _generatePerfMapFile;
        private string _perfMapPath;
        private Guid? _perfMapMvid;
        private bool _generateProfileFile;
        private int _parallelism;
        Func<MethodDesc, string> _printReproInstructions;
        private InstructionSetSupport _instructionSetSupport;
        private ProfileDataManager _profileData;
        private ReadyToRunMethodLayoutAlgorithm _r2rMethodLayoutAlgorithm;
        private ReadyToRunFileLayoutAlgorithm _r2rFileLayoutAlgorithm;
        private int _customPESectionAlignment;
        private bool _verifyTypeAndFieldLayout;
        private CompositeImageSettings _compositeImageSettings;

        private string _jitPath;
        private string _outputFile;

        // These need to provide reasonable defaults so that the user can optionally skip
        // calling the Use/Configure methods and still get something reasonable back.
        private KeyValuePair<string, string>[] _ryujitOptions = Array.Empty<KeyValuePair<string, string>>();
        private ILProvider _ilProvider = new ReadyToRunILProvider();

        public ReadyToRunCodegenCompilationBuilder(
            CompilerTypeSystemContext context,
            ReadyToRunCompilationModuleGroupBase group,
            IEnumerable<string> inputFiles,
            string compositeRootPath)
            : base(context, group, new CoreRTNameMangler())
        {
            _inputFiles = inputFiles;
            _compositeRootPath = compositeRootPath;

            // R2R field layout needs compilation group information
            ((ReadyToRunCompilerContext)context).SetCompilationGroup(group);
        }

        public override CompilationBuilder UseBackendOptions(IEnumerable<string> options)
        {
            var builder = new ArrayBuilder<KeyValuePair<string, string>>();

            foreach (string param in options ?? Array.Empty<string>())
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

            if (_context.Target.Abi == TargetAbi.CoreRTArmel)
            {
                builder.Add(new KeyValuePair<string, string>("JitSoftFP", "1"));
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

        public ReadyToRunCodegenCompilationBuilder UseJitPath(string jitPath)
        {
            _jitPath = jitPath;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseIbcTuning(bool ibcTuning)
        {
            _ibcTuning = ibcTuning;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseResilience(bool resilient)
        {
            _resilient = resilient;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseProfileData(ProfileDataManager profileData)
        {
            _profileData = profileData;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder FileLayoutAlgorithms(ReadyToRunMethodLayoutAlgorithm r2rMethodLayoutAlgorithm, ReadyToRunFileLayoutAlgorithm r2rFileLayoutAlgorithm)
        {
            _r2rMethodLayoutAlgorithm = r2rMethodLayoutAlgorithm;
            _r2rFileLayoutAlgorithm = r2rFileLayoutAlgorithm;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseMapFile(bool generateMapFile)
        {
            _generateMapFile = generateMapFile;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseMapCsvFile(bool generateMapCsvFile)
        {
            _generateMapCsvFile = generateMapCsvFile;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UsePdbFile(bool generatePdbFile, string pdbPath)
        {
            _generatePdbFile = generatePdbFile;
            _pdbPath = pdbPath;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UsePerfMapFile(bool generatePerfMapFile, string perfMapPath, Guid? inputModuleMvid)
        {
            _generatePerfMapFile = generatePerfMapFile;
            _perfMapPath = perfMapPath;
            _perfMapMvid = inputModuleMvid;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseProfileFile(bool generateProfileFile)
        {
            _generateProfileFile = generateProfileFile;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseParallelism(int parallelism)
        {
            _parallelism = parallelism;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UsePrintReproInstructions(Func<MethodDesc, string> printReproInstructions)
        {
            _printReproInstructions = printReproInstructions;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseInstructionSetSupport(InstructionSetSupport instructionSetSupport)
        {
            _instructionSetSupport = instructionSetSupport;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder GenerateOutputFile(string outputFile)
        {
            _outputFile = outputFile;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseCustomPESectionAlignment(int customPESectionAlignment)
        {
            _customPESectionAlignment = customPESectionAlignment;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseVerifyTypeAndFieldLayout(bool verifyTypeAndFieldLayout)
        {
            _verifyTypeAndFieldLayout = verifyTypeAndFieldLayout;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseCompositeImageSettings(CompositeImageSettings compositeImageSettings)
        {
            _compositeImageSettings = compositeImageSettings;
            return this;
        }

        public override ICompilation ToCompilation()
        {
            // TODO: only copy COR headers for single-assembly build and for composite build with embedded MSIL
            IEnumerable<EcmaModule> inputModules = _compilationGroup.CompilationModuleSet;
            EcmaModule singleModule = _compilationGroup.IsCompositeBuildMode ? null : inputModules.First();
            CopiedCorHeaderNode corHeaderNode = new CopiedCorHeaderNode(singleModule);
            // TODO: proper support for multiple input files
            DebugDirectoryNode debugDirectoryNode = new DebugDirectoryNode(singleModule, _outputFile);

            // Produce a ResourceData where the IBC PROFILE_DATA entry has been filtered out
            // TODO: proper support for multiple input files
            ResourceData win32Resources = new ResourceData(inputModules.First(), (object type, object name, ushort language) =>
            {
                if (!(type is string) || !(name is string))
                    return true;
                if (language != 0)
                    return true;

                string typeString = (string)type;
                string nameString = (string)name;

                if ((typeString == "IBC") && (nameString == "PROFILE_DATA"))
                    return false;

                return true;
            });

            ReadyToRunFlags flags = ReadyToRunFlags.READYTORUN_FLAG_NonSharedPInvokeStubs;
            if (inputModules.All(module => module.IsPlatformNeutral))
            {
                flags |= ReadyToRunFlags.READYTORUN_FLAG_PlatformNeutralSource;
            }
            flags |= _compilationGroup.GetReadyToRunFlags();

            NodeFactory factory = new NodeFactory(
                _context,
                (ReadyToRunCompilationModuleGroupBase)_compilationGroup,
                _profileData,
                _nameMangler,
                corHeaderNode,
                debugDirectoryNode,
                win32Resources,
                flags);

            factory.CompositeImageSettings = _compositeImageSettings;

            IComparer<DependencyNodeCore<NodeFactory>> comparer = new SortableDependencyNode.ObjectNodeComparer(new CompilerComparer());
            DependencyAnalyzerBase<NodeFactory> graph = CreateDependencyGraph(factory, comparer);

            List<CorJitFlag> corJitFlags = new List<CorJitFlag> { CorJitFlag.CORJIT_FLAG_DEBUG_INFO };

            switch (_optimizationMode)
            {
                case OptimizationMode.None:
                    corJitFlags.Add(CorJitFlag.CORJIT_FLAG_DEBUG_CODE);
                    break;

                case OptimizationMode.PreferSize:
                    corJitFlags.Add(CorJitFlag.CORJIT_FLAG_SIZE_OPT);
                    corJitFlags.Add(CorJitFlag.CORJIT_FLAG_BBOPT);
                    break;

                case OptimizationMode.PreferSpeed:
                    corJitFlags.Add(CorJitFlag.CORJIT_FLAG_SPEED_OPT);
                    corJitFlags.Add(CorJitFlag.CORJIT_FLAG_BBOPT);
                    break;

                default:
                    // Not setting a flag results in BLENDED_CODE.
                    corJitFlags.Add(CorJitFlag.CORJIT_FLAG_BBOPT);
                    break;
            }

            if (_ibcTuning)
                corJitFlags.Add(CorJitFlag.CORJIT_FLAG_BBINSTR);

            if (!_isJitInitialized)
            {
                JitConfigProvider.Initialize(_context.Target, corJitFlags, _ryujitOptions, _jitPath);
                _isJitInitialized = true;
            }

            return new ReadyToRunCodegenCompilation(
                graph,
                factory,
                _compilationRoots,
                _ilProvider,
                _logger,
                new DependencyAnalysis.ReadyToRun.DevirtualizationManager(_compilationGroup),
                _inputFiles,
                _compositeRootPath,
                _instructionSetSupport,
                resilient: _resilient,
                generateMapFile: _generateMapFile,
                generateMapCsvFile: _generateMapCsvFile,
                generatePdbFile: _generatePdbFile,
                printReproInstructions: _printReproInstructions,
                pdbPath: _pdbPath,
                generatePerfMapFile: _generatePerfMapFile,
                perfMapPath: _perfMapPath,
                perfMapMvid: _perfMapMvid,
                generateProfileFile: _generateProfileFile,
                _parallelism,
                _profileData,
                _r2rMethodLayoutAlgorithm,
                _r2rFileLayoutAlgorithm,
                _customPESectionAlignment,
                _verifyTypeAndFieldLayout);
        }
    }
}
