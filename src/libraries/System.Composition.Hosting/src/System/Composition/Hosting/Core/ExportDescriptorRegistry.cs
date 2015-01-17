// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Composition.Runtime;
using Microsoft.Internal;

namespace System.Composition.Hosting.Core
{
    internal class ExportDescriptorRegistry
    {
        private readonly object _thisLock = new object();
        private readonly ExportDescriptorProvider[] _exportDescriptorProviders;
        private volatile IDictionary<CompositionContract, ExportDescriptor[]> _partDefinitions = new Dictionary<CompositionContract, ExportDescriptor[]>();

        public ExportDescriptorRegistry(ExportDescriptorProvider[] exportDescriptorProviders)
        {
            _exportDescriptorProviders = exportDescriptorProviders;
        }

        public bool TryGetSingleForExport(CompositionContract exportKey, out ExportDescriptor defaultForExport)
        {
            ExportDescriptor[] allForExport;
            if (!_partDefinitions.TryGetValue(exportKey, out allForExport))
            {
                lock (_thisLock)
                {
                    if (!_partDefinitions.ContainsKey(exportKey))
                    {
                        var updatedDefinitions = new Dictionary<CompositionContract, ExportDescriptor[]>(_partDefinitions);
                        var updateOperation = new ExportDescriptorRegistryUpdate(updatedDefinitions, _exportDescriptorProviders);
                        updateOperation.Execute(exportKey);

                        _partDefinitions = updatedDefinitions;
                    }
                }

                allForExport = (ExportDescriptor[])_partDefinitions[exportKey];
            }

            if (allForExport.Length == 0)
            {
                defaultForExport = null;
                return false;
            }

            // This check is duplicated in the update process- the update operation will catch
            // cardinality violations in advance of this in all but a few very rare scenarios.
            if (allForExport.Length != 1)
                throw ThrowHelper.CardinalityMismatch_TooManyExports(exportKey.ToString());

            defaultForExport = allForExport[0];
            return true;
        }
    }
}
