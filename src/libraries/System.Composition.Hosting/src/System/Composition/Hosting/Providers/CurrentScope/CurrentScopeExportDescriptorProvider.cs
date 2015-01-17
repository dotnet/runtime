// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Composition.Runtime;

namespace System.Composition.Hosting.Providers.CurrentScope
{
    internal class CurrentScopeExportDescriptorProvider : ExportDescriptorProvider
    {
        private static readonly CompositionContract s_currentScopeContract = new CompositionContract(typeof(CompositionContext));

        public override IEnumerable<ExportDescriptorPromise> GetExportDescriptors(CompositionContract contract, DependencyAccessor definitionAccessor)
        {
            if (!contract.Equals(s_currentScopeContract))
                return NoExportDescriptors;

            return new[] { new ExportDescriptorPromise(
                contract,
                typeof(CompositionContext).Name,
                true,
                NoDependencies,
                _ => ExportDescriptor.Create((c, o) => c, NoMetadata)) };
        }
    }
}
