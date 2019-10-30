// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Win32Resources;
using Internal.IL;
using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    public sealed class ReadyToRunCodegenCompilationBuilder : CompilationBuilder
    {
        private readonly string _inputFilePath;
        private readonly EcmaModule _inputModule;
        private readonly bool _ibcTuning;
        private readonly bool _resilient;
        private string _jitPath;

        // These need to provide reasonable defaults so that the user can optionally skip
        // calling the Use/Configure methods and still get something reasonable back.
        private KeyValuePair<string, string>[] _ryujitOptions = Array.Empty<KeyValuePair<string, string>>();
        private ILProvider _ilProvider = new ReadyToRunILProvider();

        public ReadyToRunCodegenCompilationBuilder(CompilerTypeSystemContext context, CompilationModuleGroup group, string inputFilePath, bool ibcTuning, bool resilient)
            : base(context, group, new CoreRTNameMangler())
        {
            _inputFilePath = inputFilePath;
            _ibcTuning = ibcTuning;
            _resilient = resilient;

            _inputModule = context.GetModuleFromPath(_inputFilePath);

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

        public override CompilationBuilder UseJitPath(string jitPath)
        {
            _jitPath = jitPath;
            return this;
        }

        public override ICompilation ToCompilation()
        {
            ModuleTokenResolver moduleTokenResolver = new ModuleTokenResolver(_compilationGroup, _context);
            SignatureContext signatureContext = new SignatureContext(_inputModule, moduleTokenResolver);
            CopiedCorHeaderNode corHeaderNode = new CopiedCorHeaderNode(_inputModule);
            AttributePresenceFilterNode attributePresenceFilterNode = null;
            
            // Core library attributes are checked FAR more often than other dlls
            // attributes, so produce a highly efficient table for determining if they are
            // present. Other assemblies *MAY* benefit from this feature, but it doesn't show
            // as useful at this time.
            if (_inputModule == _inputModule.Context.SystemModule)
            {
                 attributePresenceFilterNode = new AttributePresenceFilterNode(_inputModule);
            }

            // Produce a ResourceData where the IBC PROFILE_DATA entry has been filtered out
            ResourceData win32Resources = new ResourceData(_inputModule, (object type, object name, ushort language) =>
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

            ReadyToRunCodegenNodeFactory factory = new ReadyToRunCodegenNodeFactory(
                _context,
                _compilationGroup,
                _nameMangler,
                moduleTokenResolver,
                signatureContext,
                corHeaderNode,
                win32Resources,
                attributePresenceFilterNode);

            DependencyAnalyzerBase<NodeFactory> graph = CreateDependencyGraph(factory);

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
            var jitConfig = new JitConfigProvider(corJitFlags, _ryujitOptions, _jitPath);

            return new ReadyToRunCodegenCompilation(
                graph,
                factory,
                _compilationRoots,
                _ilProvider,
                _logger,
                new DependencyAnalysis.ReadyToRun.DevirtualizationManager(_compilationGroup),
                jitConfig,
                _inputFilePath,
                new ModuleDesc[] { _inputModule },
                _resilient);
        }
    }
}
