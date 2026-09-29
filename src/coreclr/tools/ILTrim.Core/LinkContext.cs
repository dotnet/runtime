// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

using ILCompiler;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Logging;

using ILLink.Shared;

namespace Mono.Linker
{
    public partial class LinkContext
    {
        public IReadOnlyList<DependencyNodeCore<NodeFactory>> Inputs { get; }

        public ILogWriter LogWriter => _logger;

        public int? MaxDegreeOfParallelism { get; set; }

        public int EffectiveDegreeOfParallelism => MaxDegreeOfParallelism ?? Environment.ProcessorCount;

        public string DependenciesFileName { get; set; }

        public void LogError(MessageOrigin? origin, DiagnosticId id, params string[] args)
        {
            MessageContainer? error = MessageContainer.CreateErrorMessage(origin, id, args);
            if (error.HasValue)
            {
                _logger.WriteError(error.Value);
            }
        }

        public LinkContext(IReadOnlyList<DependencyNodeCore<NodeFactory>> inputs, ILogWriter logger, string outputDirectory)
        {
            Inputs = inputs;
            _logger = logger;
            _actions = new Dictionary<string, AssemblyAction>();
            _parameters = new Dictionary<string, string>(StringComparer.Ordinal);
            _cachedWarningMessageContainers = new List<MessageContainer>();
            OutputDirectory = outputDirectory;
            FeatureSettings = new Dictionary<string, bool>(StringComparer.Ordinal);

            PInvokes = new List<PInvokeInfo>();
            NoWarn = new HashSet<int>();
            GeneralWarnAsError = false;
            WarnAsError = new Dictionary<int, bool>();
            WarnVersion = WarnVersion.Latest;
            GeneralSingleWarn = false;
            SingleWarn = new Dictionary<string, bool>();
            AssembliesWithGeneratedSingleWarning = new HashSet<string>();

            const CodeOptimizations defaultOptimizations =
                CodeOptimizations.BeforeFieldInit |
                CodeOptimizations.OverrideRemoval |
                CodeOptimizations.UnusedInterfaces |
                CodeOptimizations.UnusedTypeChecks |
                CodeOptimizations.IPConstantPropagation |
                CodeOptimizations.UnreachableBodies |
                CodeOptimizations.RemoveDescriptors |
                CodeOptimizations.RemoveLinkAttributes |
                CodeOptimizations.RemoveSubstitutions |
                CodeOptimizations.RemoveDynamicDependencyAttribute |
                CodeOptimizations.OptimizeTypeHierarchyAnnotations |
                CodeOptimizations.SubstituteFeatureGuards;

            DisableEventSourceSpecialHandling = true;

            Optimizations = new CodeOptimizationsSettings(defaultOptimizations);
        }

        public ResolverShim Resolver { get; } = new ResolverShim();

        public AssemblyAction CalculateAssemblyAction(string assemblyName)
        {
            if (_actions.TryGetValue(assemblyName, out AssemblyAction action))
                return action;

            // TODO-ILTRIM: match CalculateAssemblyAction from illink (IsTrimmable, C++/CLI, etc.)
            // THREAD SAFETY IF YOU MODIFY _actions!!!
            return DefaultAction;
        }

        public class ResolverShim
        {
            private Dictionary<string, string> _referencePathsFromDirectories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private Dictionary<string, string> _referencePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public void AddSearchDirectory(string directory)
            {
                foreach (var file in Directory.GetFiles(directory, "*.exe"))
                {
                    _referencePathsFromDirectories[Path.GetFileNameWithoutExtension(file)] = file;
                }

                foreach (var file in Directory.GetFiles(directory, "*.dll"))
                {
                    _referencePathsFromDirectories[Path.GetFileNameWithoutExtension(file)] = file;
                }
            }

            public void AddReferenceAssembly(string path)
            {
                _referencePaths[Path.GetFileNameWithoutExtension(path)] = path;
            }

            public IReadOnlyDictionary<string, string> ToReferenceFilePaths()
            {
                Dictionary<string, string> result = new Dictionary<string, string>(_referencePathsFromDirectories);

                foreach ((string assemblyName, string fileName) in _referencePaths)
                    result[assemblyName] = fileName;

                return result;
            }
        }
    }
}
