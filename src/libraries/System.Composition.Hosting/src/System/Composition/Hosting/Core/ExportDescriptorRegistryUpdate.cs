// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace System.Composition.Hosting.Core
{
    internal sealed class ExportDescriptorRegistryUpdate : DependencyAccessor
    {
        private readonly IDictionary<CompositionContract, ExportDescriptor[]> _partDefinitions;
        private readonly ExportDescriptorProvider[] _exportDescriptorProviders;
        private readonly IDictionary<CompositionContract, UpdateResult> _updateResults = new Dictionary<CompositionContract, UpdateResult>();

        private static readonly CompositionDependency[] s_noDependenciesValue = Array.Empty<CompositionDependency>();
        private static readonly Func<CompositionDependency[]> s_noDependencies = () => s_noDependenciesValue;

        private bool _updateFinished;

        public ExportDescriptorRegistryUpdate(
            IDictionary<CompositionContract, ExportDescriptor[]> partDefinitions,
            ExportDescriptorProvider[] exportDescriptorProviders)
        {
            _partDefinitions = partDefinitions;
            _exportDescriptorProviders = exportDescriptorProviders;
        }

        public void Execute(CompositionContract contract)
        {
            // Opportunism - we'll miss recursive calls to Execute(), but this will catch some problems
            // and the _updateFinished flag is required for other purposes anyway.
            if (_updateFinished) throw new InvalidOperationException(SR.Update_already_executed);

            CompositionDependency initial;
            if (TryResolveOptionalDependency("initial request", contract, true, out initial))
            {
                var @checked = new HashSet<ExportDescriptorPromise>();
                var checking = new Stack<CompositionDependency>();
                CheckTarget(initial, @checked, checking);
            }

            _updateFinished = true;

            foreach (var result in _updateResults)
            {
                var resultContract = result.Key;
                var descriptors = result.Value.GetResults().Select(cb => cb.GetDescriptor()).ToArray();
                _partDefinitions.Add(resultContract, descriptors);
            }
        }

        private void CheckTarget(CompositionDependency dependency, HashSet<ExportDescriptorPromise> @checked, Stack<CompositionDependency> checking)
        {
            if (dependency.IsError)
            {
                var message = new StringBuilder();
                dependency.DescribeError(message);
                message.AppendLine();
                message.Append(DescribeCompositionStack(dependency, checking));

                var ex = new CompositionFailedException(message.ToString());
                Debug.WriteLine(SR.Diagnostic_ThrowingException, ex.ToString());
                throw ex;
            }

            if (@checked.Contains(dependency.Target))
                return;

            @checked.Add(dependency.Target);

            checking.Push(dependency);
            foreach (var dep in dependency.Target.Dependencies)
                CheckDependency(dep, @checked, checking);
            checking.Pop();
        }

        private void CheckDependency(CompositionDependency dependency, HashSet<ExportDescriptorPromise> @checked, Stack<CompositionDependency> checking)
        {
            if (@checked.Contains(dependency.Target))
            {
                var sharedSeen = false;
                var nonPrereqSeen = !dependency.IsPrerequisite;

                foreach (var step in checking)
                {
                    if (step.Target.IsShared)
                        sharedSeen = true;

                    if (sharedSeen && nonPrereqSeen)
                        break;

                    if (step.Target.Equals(dependency.Target))
                    {
                        var message = new StringBuilder();
                        message.AppendFormat(SR.ExportDescriptor_UnsupportedCycle, dependency.Target.Origin);
                        message.AppendLine();
                        message.Append(DescribeCompositionStack(dependency, checking));

                        var ex = new CompositionFailedException(message.ToString());
                        Debug.WriteLine(SR.Diagnostic_ThrowingException, ex.ToString());
                        throw ex;
                    }

                    if (!step.IsPrerequisite)
                        nonPrereqSeen = true;
                }
            }

            CheckTarget(dependency, @checked, checking);
        }

        private static StringBuilder DescribeCompositionStack(CompositionDependency import, Stack<CompositionDependency> dependencies)
        {
            var result = new StringBuilder();
            if (dependencies.Count == 0 || dependencies.Peek() == null)
            {
                return result;
            }

            foreach (CompositionDependency step in dependencies)
            {
                result.AppendFormat(SR.ExportDescriptor_DependencyErrorLine, import.Site, step.Target.Origin);
                result.AppendLine();
                import = step;
            }

            result.AppendFormat(SR.ExportDescriptor_DependencyErrorContract, import.Contract);
            return result;
        }

        protected override IEnumerable<ExportDescriptorPromise> GetPromises(CompositionContract contract)
        {
            if (_updateFinished)
            {
                throw new Exception(SR.Dependencies_Should_Be_Requested_Earlier);
            }
            ExportDescriptor[] definitions;
            if (_partDefinitions.TryGetValue(contract, out definitions))
                return definitions.Select(d => new ExportDescriptorPromise(contract, "Preexisting", false, s_noDependencies, _ => d)).ToArray();

            UpdateResult updateResult;
            if (!_updateResults.TryGetValue(contract, out updateResult))
            {
                updateResult = new UpdateResult(_exportDescriptorProviders);
                _updateResults.Add(contract, updateResult);
            }

            ExportDescriptorProvider nextProvider;
            while (updateResult.TryDequeueNextProvider(out nextProvider))
            {
                var newDefinitions = nextProvider.GetExportDescriptors(contract, this);
                foreach (var definition in newDefinitions)
                    updateResult.AddPromise(definition);
            }

            return updateResult.GetResults();
        }
    }
}
