// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Hosting.Core;

namespace System.Composition.Hosting.Providers.CurrentScope
{
    internal sealed class CurrentScopeExportDescriptorProvider : ExportDescriptorProvider
    {
        private static readonly CompositionContract s_currentScopeContract = new CompositionContract(typeof(CompositionContext));

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor definitionAccessor)
        {
            if (!contract.Equals(s_currentScopeContract))
                return NoExportDescriptors;

            return new[] { new ExportDescriptorPromise(
                contract,
                nameof(CompositionContext),
                true,
                NoDependencies,
                _ => ExportDescriptor.Create((c, o) => c, NoMetadata)) };
        }
    }
}
