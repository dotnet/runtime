// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly List<EcmaModule> _inputModules;
        private bool _ibcTuning;
        private bool _resilient;
        private bool _generateMapFile;
        private bool _composite;
        private int _parallelism;

        private string _jitPath;

        // These need to provide reasonable defaults so that the user can optionally skip
        // calling the Use/Configure methods and still get something reasonable back.
        private KeyValuePair<string, string>[] _ryujitOptions = Array.Empty<KeyValuePair<string, string>>();
        private ILProvider _ilProvider = new ReadyToRunILProvider();

        public ReadyToRunCodegenCompilationBuilder(CompilerTypeSystemContext context, CompilationModuleGroup group, bool composite, List<EcmaModule> inputModules)
            : base(context, group, new CoreRTNameMangler())
        {
            _composite = composite;
            _inputModules = inputModules;

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

        public ReadyToRunCodegenCompilationBuilder UseJitPath(FileInfo jitPath)
        {
            _jitPath = jitPath == null ? null : jitPath.FullName;
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

        public ReadyToRunCodegenCompilationBuilder UseMapFile(bool generateMapFile)
        {
            _generateMapFile = generateMapFile;
            return this;
        }

        public ReadyToRunCodegenCompilationBuilder UseParallelism(int parallelism)
        {
            _parallelism = parallelism;
            return this;
        }

        public override ICompilation ToCompilation()
        {
            // TODO: only copy COR headers for single-assembly build and for composite build with embedded MSIL
            CopiedCorHeaderNode corHeaderNode = (_composite ? null : new CopiedCorHeaderNode(_inputModules.First()));
            AttributePresenceFilterNode attributePresenceFilterNode = null;
            // TODO: proper support for multiple input files
            DebugDirectoryNode debugDirectoryNode = new DebugDirectoryNode(_inputModules.First());

            // Core library attributes are checked FAR more often than other dlls
            // attributes, so produce a highly efficient table for determining if they are
            // present. Other assemblies *MAY* benefit from this feature, but it doesn't show
            // as useful at this time.
            if (_inputModules.Contains(_context.SystemModule))
            {
                 attributePresenceFilterNode = new AttributePresenceFilterNode(_context.SystemModule);
            }

            // Produce a ResourceData where the IBC PROFILE_DATA entry has been filtered out
            // TODO: proper support for multiple input files
            ResourceData win32Resources = new ResourceData(_inputModules.First(), (object type, object name, ushort language) =>
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
            if (_inputModules.All(module => module.IsPlatformNeutral))
            {
                flags |= ReadyToRunFlags.READYTORUN_FLAG_PlatformNeutralSource;
            }
            flags |= _compilationGroup.GetReadyToRunFlags();

            NodeFactory factory = new NodeFactory(
                _context,
                _compilationGroup,
                _nameMangler,
                _composite,
                _inputModules,
                corHeaderNode,
                debugDirectoryNode,
                win32Resources,
                attributePresenceFilterNode,
                flags);

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
                    break;

                case OptimizationMode.PreferSpeed:
                    corJitFlags.Add(CorJitFlag.CORJIT_FLAG_SPEED_OPT);
                    break;

                default:
                    // Not setting a flag results in BLENDED_CODE.
                    break;
            }

            if (_ibcTuning)
                corJitFlags.Add(CorJitFlag.CORJIT_FLAG_BBINSTR);

            corJitFlags.Add(CorJitFlag.CORJIT_FLAG_FEATURE_SIMD);
            JitConfigProvider.Initialize(corJitFlags, _ryujitOptions, _jitPath);

            return new ReadyToRunCodegenCompilation(
                graph,
                factory,
                _compilationRoots,
                _ilProvider,
                _logger,
                new DependencyAnalysis.ReadyToRun.DevirtualizationManager(_compilationGroup),
                _inputModules,
                _resilient,
                _generateMapFile,
                _parallelism);
        }
    }
}
