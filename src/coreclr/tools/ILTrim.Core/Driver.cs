// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ILCompiler;
using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using ILLink.Shared;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Mono.Linker
{
    public partial class Driver
    {
        public int Run(ILogWriter logWriter = null)
        {
            int setupStatus = SetupContext(logWriter);
            if (setupStatus > 0)
                return 0;
            if (setupStatus < 0)
                return 1;

            var tsContext = new ILTrimTypeSystemContext();
            tsContext.ReferenceFilePaths = context.Resolver.ToReferenceFilePaths();

            EcmaModule corelib = tsContext.GetModuleForSimpleName("System.Private.CoreLib");
            tsContext.SetSystemModule(corelib);

            var ilProvider = new ILTrimILProvider();

            var suppressedCategories = new List<string> { MessageSubCategory.AotAnalysis };
            if (context.NoTrimWarn)
                suppressedCategories.Add(MessageSubCategory.TrimAnalysis);

            Logger logger = new Logger(
                context.LogWriter,
                ilProvider,
                isVerbose: context.LogMessages,
                suppressedWarnings: context.NoWarn,
                singleWarn: context.GeneralSingleWarn,
                singleWarnEnabledModules: context.SingleWarn.Where(kv => kv.Value).Select(kv => kv.Key),
                singleWarnDisabledModules: context.SingleWarn.Where(kv => !kv.Value).Select(kv => kv.Key),
                suppressedCategories: suppressedCategories,
                treatWarningsAsErrors: context.GeneralWarnAsError,
                warningsAsErrors: context.WarnAsError,
                disableGeneratedCodeHeuristics: context.DisableGeneratedCodeHeuristics);

            var factory = new NodeFactory(context, logger, ilProvider, tsContext);

            DependencyTrackingLevel trackingLevel = context.DependenciesFileName is not null
                ? DependencyTrackingLevel.All
                : DependencyTrackingLevel.None;
            DependencyAnalyzerBase<NodeFactory> analyzer = trackingLevel.CreateDependencyGraph(factory);

            analyzer.ComputeDependencyRoutine += ComputeDependencyNodeDependencies;

            foreach (var input in context.Inputs)
                analyzer.AddRoot(input, "Command line root");

            analyzer.AddRoot(factory.VirtualMethodUse(
                (EcmaMethod)tsContext.GetWellKnownType(WellKnownType.Object).GetMethod("Finalize"u8, null)),
                "Finalizer");

            analyzer.ComputeMarkedNodes();

            var writers = ModuleWriter.CreateWriters(factory, analyzer.MarkedNodeList);
            if (!Directory.Exists(context.OutputDirectory))
                Directory.CreateDirectory(context.OutputDirectory);
            RunForEach(writers, writer =>
            {
                string outputPath = Path.Combine(context.OutputDirectory, writer.FileName);
                using var outputStream = File.Create(outputPath);
                writer.Save(outputStream);
            });

            if (context.DependenciesFileName is not null)
            {
                using var logStream = File.OpenWrite(context.DependenciesFileName);
                DgmlWriter.WriteDependencyGraphToStream<NodeFactory>(logStream, analyzer, factory);
            }

            return logger.HasLoggedErrors ? 1 : 0;

            void ComputeDependencyNodeDependencies(List<DependencyNodeCore<NodeFactory>> nodesWithPendingDependencyCalculation) =>
                RunForEach(
                    nodesWithPendingDependencyCalculation.Cast<INodeWithDeferredDependencies>(),
                    node => node.ComputeDependencies(factory));

            void RunForEach<T>(IEnumerable<T> inputs, Action<T> action)
            {
#if !SINGLE_THREADED
                if (context.MaxDegreeOfParallelism == 1)
#endif
                {
                    foreach (var input in inputs)
                        action(input);
                }
#if !SINGLE_THREADED
                else
                {
                    Parallel.ForEach(
                        inputs,
                        new() { MaxDegreeOfParallelism = context.EffectiveDegreeOfParallelism },
                        action);
                }
#endif
            }
        }

        protected virtual LinkContext GetDefaultContext(IReadOnlyList<DependencyNodeCore<NodeFactory>> inputs, ILogWriter logger)
        {
            return new LinkContext(inputs, logger ?? new TextLogWriter(Console.Out), "output")
            {
                TrimAction = AssemblyAction.Link,
                DefaultAction = AssemblyAction.Link,
                KeepComInterfaces = true,
            };
        }

        // TODO-ILTRIM: ILTrim only supports DGML format; XML is emitted as DGML
        protected virtual void AddXmlDependencyRecorder(LinkContext context, string fileName)
        {
            context.DependenciesFileName = fileName;
        }

        protected virtual void AddDgmlDependencyRecorder(LinkContext context, string fileName)
        {
            context.DependenciesFileName = fileName;
        }

        static List<DependencyNodeCore<NodeFactory>> GetStandardPipeline()
        {
            return new List<DependencyNodeCore<NodeFactory>>();
        }

        public void Dispose() { }
    }
}
